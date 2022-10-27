using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.HostServices;
using Octopus.Server.Extensibility.Authentication.Resources;
using Octopus.Time;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.IntegratedAuthentication
{
    /// <summary>
    /// This class is used to track integrated challenges to the browser, i.e. when we've returned a 401 to trigger a challenge.
    /// If the same connection comes back a second time without the context's user identity being set then the challenge has failed. 
    /// </summary>
    class IntegratedChallengeCoordinator : IIntegratedChallengeCoordinator
    {
        readonly IClock clock;
        readonly ConcurrentDictionary<string, DateTimeOffset> connections = new ConcurrentDictionary<string, DateTimeOffset>();

        public IntegratedChallengeCoordinator(IClock clock)
        {
            this.clock = clock;
        }
        
        public IntegratedChallengeTrackerStatus SetupResponseIfChallengeHasNotSucceededYet(HttpContext context, LoginState? state, IAuthenticationConfigurationStore authenticationConfigurationStore, ISystemLog log)
        {
            // Based on https://github.com/dotnet/runtime/blob/cf63e732fc6fb57c0ea97c1b4ca965acce46343a/src/libraries/System.Net.Security/src/System/Net/Security/NegotiateStreamPal.Windows.cs#L52
            // we're being a bit cautious about using IsAuthenticated
            if (string.IsNullOrWhiteSpace(context.User.Identity.Name))
            {
                if (IsNewChallengeRequest(context.Connection.Id))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    RevokeAuthErrorCookie(context);
                    SetChallengeInitiated(context.Connection.Id);
                    return IntegratedChallengeTrackerStatus.ChallengeIssued;
                }
                
                // if we've seen this connection before and the user still isn't set then something has gone
                // wrong with the challenge. Most likely to us using HTTPS.sys https://docs.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-3.1&tabs=visual-studio#httpsys
                // User mode authentication isn't supported with Kerberos and HTTP.sys. The machine account must be used to decrypt the Kerberos token/ticket that's obtained from Active Directory
                // SPN KB: https://support.microsoft.com/en-us/help/929650/how-to-use-spns-when-you-configure-web-applications-that-are-hosted-on

                // this matches an error structure that the portal currently uses. It is not something the server
                // currently knows directly about. We may make it a first-class error object at some point to help consistency.
                var errorObj = new
                {
                    ErrorMessage = "Authentication Error",
                    Errors = new [] {"An error occurred with Windows authentication, possibly due to a known issue, please try using forms authentication."},
                    DetailLinks = new[] {"https://g.octopushq.com/TroubleshootingAD#Integrated"}
                };
                
                // we used to use the query string to pass errors back to the portal but that was quite problematic when it came to not interfering
                // with deep links etc. The portal sign in page now checks for this cookie to see if an external 
                // auth provider is trying to return an error.
                context.Response.Cookies.Append(
                    "Octopus-Auth-Error",
                    JsonConvert.SerializeObject(errorObj),
                    new CookieOptions
                    {
                        MaxAge = TimeSpan.FromSeconds(15),
                        Secure = state?.UsingSecureConnection ?? false
                    });
                
                var stateRedirectAfterLoginTo = state?.RedirectAfterLoginTo;
                // we pass back to the original link here if it is a trusted url (e.g. the deep link that originally triggered the sign in)
                // we normally wouldn't do this without the user being authenticated, but given we know they aren't
                // authenticated we'll redirect back and rely on the sign in page kicking in again and seeing the cookie.
                // If the caller has provided a redirect after successful login, we need to check it is a local address - preventing Open Redirection attacks
                if (!string.IsNullOrWhiteSpace(stateRedirectAfterLoginTo))
                {
                    var whitelist = authenticationConfigurationStore.GetTrustedRedirectUrls();
                    if (Requests.IsLocalUrl(stateRedirectAfterLoginTo, whitelist))
                    {
                        // This is a safe redirect, let's go!
                        context.Response.Redirect(stateRedirectAfterLoginTo);

                        SetChallengeCompleted(context.Connection.Id);
                        return IntegratedChallengeTrackerStatus.ChallengeFailed;
                    }

                    // Just log that we detected a non-local redirect URL, and fall through to the root of the local web site
                    log.WarnFormat(
                        "Prevented potential Open Redirection attack on an integrated authentication challenge, to the non-local url {0}",
                        stateRedirectAfterLoginTo);
                }

                // By default, redirect to the root of the local web site
                context.Response.Redirect(context.Request.PathBase.Value ?? "/");
                
                // count this as complete, if the browser comes back on the same connection we'll start over 
                SetChallengeCompleted(context.Connection.Id);
                return IntegratedChallengeTrackerStatus.ChallengeFailed;
            }

            RevokeAuthErrorCookie(context);
            SetChallengeCompleted(context.Connection.Id);
            return IntegratedChallengeTrackerStatus.ChallengeSucceeded;
        }

        void RevokeAuthErrorCookie(HttpContext context)
        {
            context.Response.Cookies.Append(
                "Octopus-Auth-Error",
                string.Empty);
        }

        internal bool IsNewChallengeRequest(string connectionId)
        {
            return !connections.ContainsKey(connectionId);
        }

        internal void SetChallengeInitiated(string connectionId)
        {
            if (!IsNewChallengeRequest(connectionId))
                return;
            var initiated = clock.GetUtcTime();
            connections.AddOrUpdate(connectionId, c => initiated, (c, i) => initiated);
            TryCleanupOldConnections();
        }

        internal void SetChallengeCompleted(string connectionId)
        {
            if (IsNewChallengeRequest(connectionId))
                return;
            connections.TryRemove(connectionId, out DateTimeOffset initiated);
            TryCleanupOldConnections();
        }

        /// <summary>
        /// There's a possibility a browser could initiate a challenge and then never come back. This method looks for
        /// any cases where the a challenge was initiated over an hour ago
        /// </summary>
        void TryCleanupOldConnections()
        {
            try
            {
                var anHourAgo = clock.GetUtcTime().AddMinutes(-60);
                var oldConnectionIds = connections.Where(c => c.Value < anHourAgo).Select(c => c.Key).ToArray();

                foreach (var oldConnectionId in oldConnectionIds)
                {
                    connections.TryRemove(oldConnectionId, out DateTimeOffset initiated);
                }
            }
            catch (Exception)
            {
                // we don't want any misc error in here with the dictionary to cause an auth to fail
            }
        }
    }
}
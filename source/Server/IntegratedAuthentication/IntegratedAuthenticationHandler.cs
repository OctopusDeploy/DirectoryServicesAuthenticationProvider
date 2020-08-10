using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Octopus.Data;
using Octopus.Data.Model.User;
using Octopus.Data.Storage.User;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;
using Octopus.Server.Extensibility.Authentication.HostServices;
using Octopus.Server.Extensibility.Authentication.Resources;
using Octopus.Server.Extensibility.Results;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;
using Octopus.Server.Extensibility.HostServices.Web;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.IntegratedAuthentication
{
    public static class ApiConstants
    {
        public const string OctopusNode = "Octopus-Node";
        public const string ApiKeyHttpHeaderName = "X-Octopus-ApiKey";
        public const string AntiforgeryTokenHttpHeaderName = "X-Octopus-Csrf-Token";
        public const string OctopusUserAgentHeaderName = "X-Octopus-User-Agent";
        public const string OctopusDataVersionHeaderName = "X-Octopus-Data-Version";
        public const string OctopusAuthorizationHashHeaderName = "X-Octopus-Authorization-Hash";
    }

    public enum DomainCookieOptions
    {
        CustomDomain = 0,
        OriginDomain = 1,
    }
    
    class IntegratedAuthenticationHandler : IIntegratedAuthenticationHandler
    {
        readonly ILog log;
        readonly IWebPortalConfigurationStore configuration;
        readonly IAuthCookieCreator tokenIssuer;
        readonly IAuthenticationConfigurationStore authenticationConfigurationStore;
        readonly DirectoryServicesUserCreationFromPrincipal supportsAutoUserCreationFromPrincipals;
        readonly IUserStore userStore;
        readonly IIntegratedChallengeCoordinator integratedChallengeCoordinator;

        public IntegratedAuthenticationHandler(ILog log,
            IWebPortalConfigurationStore configuration,
            IAuthCookieCreator tokenIssuer,
            IAuthenticationConfigurationStore authenticationConfigurationStore,
            DirectoryServicesUserCreationFromPrincipal supportsAutoUserCreationFromPrincipals,
            IUserStore userStore,
            IIntegratedChallengeCoordinator integratedChallengeCoordinator)
        {
            this.log = log;
            this.configuration = configuration;
            this.tokenIssuer = tokenIssuer;
            this.authenticationConfigurationStore = authenticationConfigurationStore;
            this.supportsAutoUserCreationFromPrincipals = supportsAutoUserCreationFromPrincipals;
            this.userStore = userStore;
            this.integratedChallengeCoordinator = integratedChallengeCoordinator;
        }

        void AddCorsHeaders(HttpContext context)
        {
            context.Response.Headers.Add("Access-Control-Allow-Origin", configuration.GetCorsWhitelist());
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, PUT, POST, DELETE, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Add("Access-Control-Expose-Headers", $"{ApiConstants.OctopusDataVersionHeaderName}, {ApiConstants.OctopusAuthorizationHashHeaderName}, {ApiConstants.OctopusNode}");
            context.Response.Headers.Add("Access-Control-Allow-Headers",$"cache-control, content-type, x-http-method-override, {ApiConstants.OctopusDataVersionHeaderName}, {ApiConstants.OctopusAuthorizationHashHeaderName}, {ApiConstants.ApiKeyHttpHeaderName}, {ApiConstants.AntiforgeryTokenHttpHeaderName}, {ApiConstants.OctopusUserAgentHeaderName}" );
            context.Request.Headers.TryGetValue("Access-Control-Request-Method", out var accessControlRequestMethod);
            context.Response.Headers.Add("Allow", accessControlRequestMethod.Any() ? accessControlRequestMethod.FirstOrDefault() ?? "GET" : "GET");
        }

        public Task HandleRequest(HttpContext context)
        {
            var state = GetLoginState(context);
            AddCorsHeaders(context);
            if (integratedChallengeCoordinator.SetupResponseIfChallengeHasNotSucceededYet(context, state) != IntegratedChallengeTrackerStatus.ChallengeSucceeded)
            {
                // the coordinator will configure the Response object in the correct way for incomplete challenges
                return Task.CompletedTask;
            }

            // Challenge has succeeded!!

            var result = TryAuthenticateRequest(context);
            if (result == null)
                return Task.CompletedTask;

            var principalIdentificationToken = result.Value?.IdentificationToken;
            if (principalIdentificationToken == null)
                throw new InvalidOperationException("The user principal's identification token was ");

            // Build the auth cookies to send back with the response
            var authCookies = tokenIssuer.CreateAuthCookies(principalIdentificationToken.Value, SessionExpiry.TwentyDays, context.Request.IsHttps, state?.UsingSecureConnection);

            // If the caller has provided a redirect after successful login, we need to check it is a local address - preventing Open Redirection attacks
            if (!string.IsNullOrWhiteSpace(state?.RedirectAfterLoginTo))
            {
                var whitelist = authenticationConfigurationStore.GetTrustedRedirectUrls();
                var redirectAfterLoginTo = state.RedirectAfterLoginTo.Replace(DirectoryServicesConstants.IntegratedAuthVirtualDirectory, string.Empty);
                if (Requests.IsLocalUrl(redirectAfterLoginTo, whitelist))
                {
                    // This is a safe redirect, let's go!
                    context.Response.Redirect(redirectAfterLoginTo);
                    var isLocalhost = String.Compare(context.Request.Host.Value, "localhost", StringComparison.OrdinalIgnoreCase) == 0;
                    foreach (var cookie in authCookies)
                    {
                        //If the current host happens to be localhost, then we don't want to set the cookie domain as this will result in being unable to log in using AD credentials when using localhost
                        context.Response.Cookies.Append(cookie.Name, cookie.Value, ConvertOctoCookieToCookieOptions(cookie, isLocalhost ? DomainCookieOptions.OriginDomain : DomainCookieOptions.CustomDomain));
                    }
                    return Task.CompletedTask;
                }

                // Just log that we detected a non-local redirect URL, and fall through to the root of the local web site
                log.WarnFormat(
                    "Prevented potential Open Redirection attack on an integrated authentication challenge, to the non-local url {0}",
                    redirectAfterLoginTo);
            }

            // By default, redirect to the root of the local web site
            context.Response.Redirect(context.Request.PathBase.Value ?? "/");
            foreach (var cookie in authCookies)
            {
                context.Response.Cookies.Append(cookie.Name, cookie.Value, ConvertOctoCookieToCookieOptions(cookie, DomainCookieOptions.CustomDomain));
            }

            return Task.CompletedTask;
        }
        
        CookieOptions ConvertOctoCookieToCookieOptions(OctoCookie cookie, DomainCookieOptions options)
        {
            var result = new CookieOptions
            {
                Domain = options == DomainCookieOptions.CustomDomain ? cookie.Domain: null,
                Expires = cookie.Expires, 
                Path = cookie.Path, 
                HttpOnly = cookie.HttpOnly,
                Secure = cookie.Secure,
                MaxAge = cookie.MaxAge,
            };
            
            return result;
        }

        LoginState? GetLoginState(HttpContext context)
        {
            // Decode the state object sent from the client (if there was one) so we can use those hints to build the most appropriate response
            // If the state can't be interpreted, we will fall back to a safe-by-default behaviour, however:
            //   1. Deep-links will not work because we don't know where the anonymous request originally wanted to go
            //   2. Cookies may not have the Secure flag set properly when SSL Offloading is in play
            LoginState? state = null;
            if (context.Request.Query.TryGetValue("state", out var stateString))
            {
                try
                {
                    state = JsonConvert.DeserializeObject<LoginState>(stateString.FirstOrDefault());
                }
                catch (Exception e)
                {
                    log.Warn(e, "Invalid login state object passed to the server when setting up the integrated authentication challenge. Falling back to the default behaviour.");
                }
            }

            return state;
        }

        /// <returns>Null if the user is currently unknown.</returns>
        Result<IUser>? TryAuthenticateRequest(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.Request == null) throw new ArgumentNullException($"{nameof(context)}.{nameof(context.Request)}");

            // If there is no "RequestPrincipal" in the Context.Items it's not our job to authenticate this request
            var principal = context.User;
            if (string.IsNullOrWhiteSpace(principal.Identity.Name))
                return null;

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token))
            {
                // Attempt to create the user based on the provided System.Security.Principal.IPrincipal
                var userResult = supportsAutoUserCreationFromPrincipals.GetOrCreateUser(principal, cts.Token);

                // If we couldn't create the user account we also can't authenticate this request
                if (userResult is FailureResult) return null;

                // Otherwise we should be good to go!
                var user = userStore.GetByIdentificationToken(((ResultFromExtension<IUser>)userResult).Value.IdentificationToken);

                return Result<IUser>.Success(user);
            }
        }
    }
}
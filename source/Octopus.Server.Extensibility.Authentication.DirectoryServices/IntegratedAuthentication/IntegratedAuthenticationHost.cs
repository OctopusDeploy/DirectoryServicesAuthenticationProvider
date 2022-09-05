using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web;
using Octopus.Server.Extensibility.HostServices.Web;
using AuthenticationSchemes = Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.IntegratedAuthentication
{
    /// <summary>
    /// This class is used to start up a second WebHost, which is used to host the integrated challenge endpoint.
    ///
    /// Background:
    /// HttpSys only allows a single authentication scheme. To support our cookies based authentication, and the
    /// ability for users to log in/out and have multiple authentication providers enabled at once, we need to
    /// support multiple authentication schemes.
    ///
    /// We considered a number of options but after going through https://github.com/dotnet/aspnetcore/issues/5888
    /// we decided to follow the advice there and use 2 hosts.
    ///
    /// Notes:
    /// This host is configure to look/behave like a virtual directory off the main API site's root, I.e. `/integrate-challenge`,
    /// and is therefore consistent with the location in earlier versions of server. The host only has that one route
    /// and it initiates the challenge, using a 401 response, when the user isn't already authenticated.
    ///
    /// Changing the authentication scheme in this world requires a restart of all nodes, as the setting has to be set
    /// when the WebHost starts.
    /// </summary>
    class IntegratedAuthenticationHost : IShareWebHostLifetime
    {
        readonly ISystemLog log;
        readonly IWebPortalConfigurationStore configuration;
        readonly IDirectoryServicesConfigurationStore configurationStore;
        readonly IIntegratedAuthenticationHandler handler;
        IWebHost? host;

        public IntegratedAuthenticationHost(ISystemLog log,
            IWebPortalConfigurationStore configuration,
            IDirectoryServicesConfigurationStore configurationStore,
            IIntegratedAuthenticationHandler handler)
        {
            this.log = log;
            this.configuration = configuration;
            this.configurationStore = configurationStore;
            this.handler = handler;
        }

        public Task StartAsync()
        {
            if (configuration.GetWebServer() != WebServer.HttpSys)
            {
                // This HTTP.sys based integrated authentication endpoint will only work when the server is also
                // running HTTP.sys because it requires port sharing
                return Task.CompletedTask;
            }

            var prefixes = GetListenPrefixes();

            var builder = new WebHostBuilder();

            builder.UseHttpSys(options =>
            {
                options.Authentication.Schemes = MapAuthenticationScheme();

                // IMPORTANT: we need AllowAnonymous to be true here. If it is false then the ASPNET Core internals
                // will automatically issue the 401 challenge and we don't get a chance to report meaningful errors
                // if the challenge fails (the user will get the challenge popup dialog in the browser).
                options.Authentication.AllowAnonymous = true;

                foreach (var baseUri in prefixes)
                {
                    var prefix = baseUri.ToString();

                    if (!baseUri.Host.Contains("."))
                    {
                        prefix = prefix.Replace("localhost", "+");
                    }

                    options.UrlPrefixes.Add(prefix);
                }
            });

             builder.Configure(app =>
             {
                 app.Use((context, func) =>
                 {
                     if (!configurationStore.GetIsEnabled())
                     {
                         context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                         return Task.CompletedTask;
                     }

                     return handler.HandleRequest(context);
                 });
             });

            host = builder.Build();
            return host.StartAsync();
        }

        AuthenticationSchemes MapAuthenticationScheme()
        {
            switch (configurationStore.GetAuthenticationScheme())
            {
                case System.Net.AuthenticationSchemes.Ntlm:
                    return AuthenticationSchemes.NTLM;
                case System.Net.AuthenticationSchemes.Negotiate:
                    return AuthenticationSchemes.Negotiate;
                case System.Net.AuthenticationSchemes.IntegratedWindowsAuthentication:
                    return AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM;
            }
            return AuthenticationSchemes.None;
        }

        Uri[] GetListenPrefixes()
        {
            var setting = configuration.GetCurrentNodeWebPortalConfiguration().ListenPrefixes;

            var prefixes = new List<Uri>();

            if (setting.Any())
            {
                var values = setting.Select(v => v.Trim()).Where(v => v.Length > 0).ToArray();

                foreach (var value in values)
                {
                    log.Trace("Adding listen prefix: " + value);

                    try
                    {
                        prefixes.Add(new Uri(value.TrimEnd('/') + DirectoryServicesConstants.IntegratedAuthVirtualDirectory, UriKind.Absolute));
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                        throw new UriFormatException("Unable to parse listen prefix '" + value + "': " + ex.Message, ex);
                    }
                }
            }

            return prefixes.ToArray();
        }

        public async Task StopAsync()
        {
            if (host == null)
                return;

            await host.StopAsync();
            host.Dispose();
            host = null;
        }
    }
}
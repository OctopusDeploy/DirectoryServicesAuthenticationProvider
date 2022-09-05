using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.DependencyInjection;
using Octopus.Server.Extensibility.HostServices.Web;
using Octopus.Server.Extensibility.Web.Api;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices
{
    public class DirectoryServicesWebServiceContributor : IContributeToWebServices
    {
        readonly IWebPortalConfigurationStore configuration;

        public DirectoryServicesWebServiceContributor(IWebPortalConfigurationStore configuration)
        {
            this.configuration = configuration;
        }

        public void ContributeTo(IServiceCollection instance)
        {
            if (configuration.GetWebServer() != WebServer.Kestrel)
                return;

            instance.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
            instance.AddControllers()
                .AddApplicationPart(typeof(DirectoryServicesWebServiceContributor).Assembly)
                .AddControllersAsServices();
        }
    }
}
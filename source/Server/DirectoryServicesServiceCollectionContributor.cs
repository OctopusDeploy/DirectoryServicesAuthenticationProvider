using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.DependencyInjection;
using Octopus.Server.Extensibility.Extensions;
using Octopus.Server.Extensibility.HostServices.Web;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices
{
    public class DirectoryServicesServiceCollectionContributor : IContributeToType<IServiceCollection>
    {
        readonly IWebPortalConfigurationStore configuration;

        public DirectoryServicesServiceCollectionContributor(IWebPortalConfigurationStore configuration)
        {
            this.configuration = configuration;
        }

        public void ContributeTo(IServiceCollection instance)
        {
            if (configuration.GetWebServer() == WebServer.Kestrel)
            {
                instance.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
            }
        }
    }
}
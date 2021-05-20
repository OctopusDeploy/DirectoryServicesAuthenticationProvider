using Microsoft.AspNetCore.Builder;
using Octopus.Server.Extensibility.Extensions;
using Octopus.Server.Extensibility.HostServices.Web;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices
{
    public class DirectoryServicesApplicationBuilderContributor : IContributeToType<IApplicationBuilder>
    {
        readonly IWebPortalConfigurationStore configuration;

        public DirectoryServicesApplicationBuilderContributor(IWebPortalConfigurationStore configuration)
        {
            this.configuration = configuration;
        }

        public void ContributeTo(IApplicationBuilder instance)
        {
            if (configuration.GetWebServer() == WebServer.Kestrel)
            {
                instance.UseAuthentication();
            }
        }
    }
}
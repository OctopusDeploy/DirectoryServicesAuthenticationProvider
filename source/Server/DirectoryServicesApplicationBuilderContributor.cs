using Microsoft.AspNetCore.Builder;
using Octopus.Server.Extensibility.Extensions;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices
{
    public class DirectoryServicesApplicationBuilderContributor : IContributeToType<IApplicationBuilder>
    {
        public void ContributeTo(IApplicationBuilder instance)
        {
            instance.UseAuthentication(); //
        }
    }
}
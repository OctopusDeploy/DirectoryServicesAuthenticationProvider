using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.DependencyInjection;
using Octopus.Server.Extensibility.Authentication.Extensions;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices
{
    public class DirectoryServicesServiceCollectionContributor : IContributeToType<IServiceCollection>
    {
        public void ContributeTo(IServiceCollection instance)
        {
            instance.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
        }
    }
}
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Web;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices
{
    class DirectoryServicesApi : RegistersEndpoints
    {
        public const string ApiExternalGroupsSearch = "/api/externalgroups/directoryServices{?partialName}";
        public const string ApiExternalUsersSearch = "/api/externalusers/directoryServices{?partialName}";

        public DirectoryServicesApi()
        {
            Add<ListSecurityGroupsAction>("GET", ApiExternalGroupsSearch, RouteCategory.Navigable, new SecuredWhenEnabledEndpointInvocation<IDirectoryServicesConfigurationStore>());
            Add<UserLookupAction>("GET", ApiExternalUsersSearch, RouteCategory.Navigable, new SecuredWhenEnabledEndpointInvocation<IDirectoryServicesConfigurationStore>());
        }
    }
}
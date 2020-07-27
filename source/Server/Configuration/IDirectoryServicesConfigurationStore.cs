using System.Net;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration
{
    interface IDirectoryServicesConfigurationStore : IExtensionConfigurationStore<DirectoryServicesConfiguration>
    {
        string? GetActiveDirectoryContainer();
        void SetActiveDirectoryContainer(string? activeDirectoryContainer);

        AuthenticationSchemes GetAuthenticationScheme();
        void SetAuthenticationScheme(AuthenticationSchemes scheme);

        bool GetAllowFormsAuthenticationForDomainUsers();
        void SetAllowFormsAuthenticationForDomainUsers(bool allowFormAuth);

        bool GetAreSecurityGroupsEnabled();
        void SetAreSecurityGroupsEnabled(bool areSecurityGroupsEnabled);

        bool GetAllowAutoUserCreation();
        void SetAllowAutoUserCreation(bool allowAutoUserCreation);
    }
}
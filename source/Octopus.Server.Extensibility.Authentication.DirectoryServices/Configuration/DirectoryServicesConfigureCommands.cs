using System;
using System.Collections.Generic;
using System.Net;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration
{
    class DirectoryServicesConfigureCommands : IContributeToConfigureCommand
    {
        readonly ISystemLog log;
        readonly Lazy<IDirectoryServicesConfigurationStore> activeDirectoryConfiguration;

        public DirectoryServicesConfigureCommands(
            ISystemLog log,
            Lazy<IDirectoryServicesConfigurationStore> activeDirectoryConfiguration)
        {
            this.log = log;
            this.activeDirectoryConfiguration = activeDirectoryConfiguration;
        }

        public IEnumerable<ConfigureCommandOption> GetOptions()
        {
            yield return new ConfigureCommandOption("activeDirectoryIsEnabled=", "Set whether active directory is enabled.", v =>
            {
                var isEnabled = bool.Parse(v);
                activeDirectoryConfiguration.Value.SetIsEnabled(isEnabled);
                log.Info($"Active directory IsEnabled set to: {isEnabled}");
            });
            yield return new ConfigureCommandOption("activeDirectoryContainer=", DirectoryServicesConfigurationResource.ActiveDirectoryContainerDescription, v =>
            {
                activeDirectoryConfiguration.Value.SetActiveDirectoryContainer(v);
                log.Info($"Active directory container set to: {v}");
            });
            yield return new ConfigureCommandOption("webAuthenticationScheme=", DirectoryServicesConfigurationResource.AuthenticationSchemeDescription, v =>
            {
                var scheme = (AuthenticationSchemes) Enum.Parse(typeof(AuthenticationSchemes), v);
                activeDirectoryConfiguration.Value.SetAuthenticationScheme(scheme);
                log.Info("Web authentication scheme: " + scheme);
            });
            yield return new ConfigureCommandOption("allowFormsAuthenticationForDomainUsers=", DirectoryServicesConfigurationResource.AllowFormsAuthenticationForDomainUsersDescription, v =>
            {
                var allowFormsAuthenticationForDomainUsers = bool.Parse(v);
                activeDirectoryConfiguration.Value.SetAllowFormsAuthenticationForDomainUsers(allowFormsAuthenticationForDomainUsers);
                log.Info("Allow forms authentication for domain users: " + allowFormsAuthenticationForDomainUsers);
            });
            yield return new ConfigureCommandOption("activeDirectorySecurityGroupsEnabled=", DirectoryServicesConfigurationResource.AreSecurityGroupsEnabledDescription, v =>
            {
                var externalSecurityGroupsEnabled = bool.Parse(v);
                activeDirectoryConfiguration.Value.SetAreSecurityGroupsEnabled(externalSecurityGroupsEnabled);
                log.Info("Active Directory security groups enabled: " + externalSecurityGroupsEnabled);
            });
            yield return new ConfigureCommandOption("activeDirectoryAllowAutoUserCreation=", DirectoryServicesConfigurationResource.AllowAutoUserCreationDescription, v =>
            {
                var isAllowed = bool.Parse(v);
                activeDirectoryConfiguration.Value.SetAllowAutoUserCreation(isAllowed);
                log.Info("Active Directory auto user creation allowed: " + isAllowed);
            });

        }
    }
}
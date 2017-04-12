using System;
using System.Collections.Generic;
using System.Net;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration
{
    public class DirectoryServicesConfigureCommands : IContributeToConfigureCommand, IHandleLegacyWebAuthenticationModeConfigurationCommand
    {
        readonly ILog log;
        readonly IDirectoryServicesConfigurationStore activeDirectoryConfiguration;

        public DirectoryServicesConfigureCommands(
            ILog log,
            IDirectoryServicesConfigurationStore activeDirectoryConfiguration)
        {
            this.log = log;
            this.activeDirectoryConfiguration = activeDirectoryConfiguration;
        }

        public IEnumerable<ConfigureCommandOption> GetOptions()
        {
            yield return new ConfigureCommandOption("activeDirectoryIsEnabled=", "Set whether active directory is enabled.", v =>
            {
                var isEnabled = bool.Parse(v);
                activeDirectoryConfiguration.SetIsEnabled(isEnabled);
                log.Info($"Active directory IsEnabled set to: {isEnabled}");
            });
            yield return new ConfigureCommandOption("activeDirectoryContainer=", "Set the active directory container used for authentication.", v =>
            {
                activeDirectoryConfiguration.SetActiveDirectoryContainer(v);
                log.Info($"Active directory container set to: {v}");
            });
            yield return new ConfigureCommandOption("webAuthenticationScheme=", "When Domain authentication is used, specifies the scheme (Basic, Digest, IntegratedWindowsAuthentication, Negotiate, Ntlm)", v =>
            {
                var scheme = (AuthenticationSchemes) Enum.Parse(typeof(AuthenticationSchemes), v);
                activeDirectoryConfiguration.SetAuthenticationScheme(scheme);
                log.Info("Web authentication scheme: " + scheme);
            });
            yield return new ConfigureCommandOption("allowFormsAuthenticationForDomainUsers=", "When Domain authentication is used, specifies whether the HTML-based username/password form can be used to sign in.", v =>
            {
                var allowFormsAuthenticationForDomainUsers = bool.Parse(v);
                activeDirectoryConfiguration.SetAllowFormsAuthenticationForDomainUsers(allowFormsAuthenticationForDomainUsers);
                log.Info("Allow forms authentication for domain users: " + allowFormsAuthenticationForDomainUsers);
            });
            yield return new ConfigureCommandOption("activeDirectorySecurityGroupsEnabled=", "When Domain authentication is used, specifies whether to support security groups from AD.", v =>
            {
                var externalSecurityGroupsEnabled = bool.Parse(v);
                activeDirectoryConfiguration.SetAreSecurityGroupsEnabled(externalSecurityGroupsEnabled);
                log.Info("Active Directory security groups enabled: " + externalSecurityGroupsEnabled);
            });

        }

        public void Handle(string webAuthenticationMode)
        {
            var isEnabled = "Domain".Equals(webAuthenticationMode, StringComparison.InvariantCultureIgnoreCase);
            activeDirectoryConfiguration.SetIsEnabled(isEnabled);
            log.Info($"Active directory IsEnabled set, based on webAuthenticationMode={webAuthenticationMode}, to: {isEnabled}");
        }
    }
}
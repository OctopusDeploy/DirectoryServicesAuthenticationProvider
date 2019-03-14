using System.Collections.Generic;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Server.Extensibility.HostServices.Mapping;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration
{
    public class DirectoryServicesConfigurationSettings :
        ExtensionConfigurationSettings<DirectoryServicesConfiguration, DirectoryServicesConfigurationResource,
            IDirectoryServicesConfigurationStore>, IDirectoryServicesConfigurationSettings
    {
        public DirectoryServicesConfigurationSettings(
            IDirectoryServicesConfigurationStore configurationDocumentStore) : base(configurationDocumentStore)
        {
        }

        public override string Id => DirectoryServicesConfigurationStore.SingletonId;

        public override string ConfigurationSetName => "Active Directory";

        public override string Description => "Active Directory authentication settings";

        public override IEnumerable<IConfigurationValue> GetConfigurationValues()
        {
            var isEnabled = ConfigurationDocumentStore.GetIsEnabled();

            yield return new ConfigurationValue<bool>("Octopus.WebPortal.ActiveDirectoryIsEnabled", isEnabled, isEnabled, "Is Enabled");
            yield return new ConfigurationValue<string>("Octopus.WebPortal.ActiveDirectoryContainer", ConfigurationDocumentStore.GetActiveDirectoryContainer(), isEnabled && !string.IsNullOrWhiteSpace(ConfigurationDocumentStore.GetActiveDirectoryContainer()), "Active Directory Container");
            yield return new ConfigurationValue<string>("Octopus.WebPortal.AuthenticationScheme", ConfigurationDocumentStore.GetAuthenticationScheme().ToString(), isEnabled, "Authentication Scheme");
            yield return new ConfigurationValue<bool>("Octopus.WebPortal.AllowFormsAuthenticationForDomainUsers", ConfigurationDocumentStore.GetAllowFormsAuthenticationForDomainUsers(), isEnabled, "Allow forms authentication");
            yield return new ConfigurationValue<bool>("Octopus.WebPortal.ExternalSecurityGroupsDisabled", ConfigurationDocumentStore.GetAreSecurityGroupsEnabled(), isEnabled, "Security groups enabled");
            yield return new ConfigurationValue<bool>("Octopus.WebPortal.ActiveDirectoryAllowAutoUserCreation", ConfigurationDocumentStore.GetAllowAutoUserCreation(), isEnabled, "Allow auto user creation");
        }

        public override void BuildMappings(IResourceMappingsBuilder builder)
        {
            builder.Map<DirectoryServicesConfigurationResource, DirectoryServicesConfiguration>();
        }
    }
}
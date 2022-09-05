using System;
using System.Linq;
using System.Net;
using Octopus.Configuration;
using Octopus.Data.Storage.Configuration;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Extensions.Infrastructure;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration
{
    class DatabaseInitializer : ExecuteWhenDatabaseInitializes
    {
        readonly ISystemLog log;
        readonly IConfigurationStore configurationStore;
        readonly IWritableKeyValueStore settings;

        bool cleanupRequired = false;

        public DatabaseInitializer(ISystemLog log, IConfigurationStore configurationStore, IWritableKeyValueStore settings)
        {
            this.log = log;
            this.configurationStore = configurationStore;
            this.settings = settings;
        }

        readonly string[] legacyModes = { "Domain", "1" };

        public override void Execute()
        {
            var doc = configurationStore.Get<DirectoryServicesConfiguration>(DirectoryServicesConfigurationStore.SingletonId);
            if (doc != null)
            {
                // TODO: to cover a dev team edge case during 4.0 Alpha. Can be removed before final release
                if (doc.ConfigurationSchemaVersion != "1.0")
                {
                    doc.ConfigurationSchemaVersion = "1.0";
                    configurationStore.Update(doc);
                }
                return;
            }

            log.Info("Moving Octopus.WebPortal.ActiveDirectoryContainer/AuthenticationScheme/AllowFormsAuthenticationForDomainUsers from config file to DB");

            var activeDirectoryContainer = settings.Get("Octopus.WebPortal.ActiveDirectoryContainer", string.Empty);
            var authenticationScheme = settings.Get("Octopus.WebPortal.AuthenticationScheme", AuthenticationSchemes.Ntlm);
            var allowFormsAuth = settings.Get("Octopus.WebPortal.AllowFormsAuthenticationForDomainUsers", true);
            var areSecurityGroupsDisabled = settings.Get("Octopus.WebPortal.ExternalSecurityGroupsDisabled", false);
            var allowAutoUserCreation = settings.Get("Octopus.WebPortal.ActiveDirectoryAllowAutoUserCreation", true);

            var authenticationMode = settings.Get("Octopus.WebPortal.AuthenticationMode", string.Empty);
            doc = new DirectoryServicesConfiguration
            {
                IsEnabled = legacyModes.Any(x => x.Equals(authenticationMode.Replace("\"", ""), StringComparison.InvariantCultureIgnoreCase)),
                ActiveDirectoryContainer = activeDirectoryContainer,
                AuthenticationScheme = authenticationScheme,
                AllowFormsAuthenticationForDomainUsers = allowFormsAuth,
                AreSecurityGroupsEnabled = !areSecurityGroupsDisabled,
                AllowAutoUserCreation = allowAutoUserCreation
            };

            configurationStore.Create(doc);

            cleanupRequired = true;
        }

        public override void PostExecute()
        {
            if (cleanupRequired == false)
                return;

            settings.Remove("Octopus.WebPortal.AuthenticationMode");

            settings.Remove("Octopus.WebPortal.ActiveDirectoryContainer");
            settings.Remove("Octopus.WebPortal.AuthenticationScheme");
            settings.Remove("Octopus.WebPortal.AllowFormsAuthenticationForDomainUsers");
            settings.Remove("Octopus.WebPortal.ExternalSecurityGroupsDisabled");
            settings.Remove("Octopus.WebPortal.ActiveDirectoryAllowAutoUserCreation");

            settings.Save();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Octopus.Configuration;
using Octopus.Data.Storage.Configuration;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;
using Octopus.Server.Extensibility.Authentication.Extensions;
using Octopus.Server.Extensibility.Authentication.HostServices;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration
{
    public class DirectoryServicesConfigurationStore : IDirectoryServicesConfigurationStore, IAuthenticationSchemeProvider, IHasConfigurationSettings
    {
        public static string SingletonId = "authentication-directoryservices";

        readonly ILog log;
        readonly IKeyValueStore settings;
        readonly IConfigurationStore configurationStore;
        readonly IAuthenticationConfigurationStore authenticationConfigurationStore;

        public DirectoryServicesConfigurationStore(
            ILog log,
            IKeyValueStore settings,
            IConfigurationStore configurationStore,
            IAuthenticationConfigurationStore authenticationConfigurationStore)
        {
            this.log = log;
            this.settings = settings;
            this.configurationStore = configurationStore;
            this.authenticationConfigurationStore = authenticationConfigurationStore;
        }

        public string ChallengePath => DirectoryServicesConstants.ChallengePath;
        public AuthenticationSchemes AuthenticationScheme => GetIsEnabled() ? GetAuthenticationScheme() : AuthenticationSchemes.Anonymous;

        public bool GetIsEnabled()
        {
            var doc = configurationStore.Get<DirectoryServicesConfiguration>(SingletonId);
            if (doc != null)
                return doc.IsEnabled;

            doc = MoveSettingsToDatabase();

            return doc.IsEnabled;
        }

        public void SetIsEnabled(bool isEnabled)
        {
            var doc = configurationStore.Get<DirectoryServicesConfiguration>(SingletonId) ?? MoveSettingsToDatabase();
            doc.IsEnabled = isEnabled;
            configurationStore.Update(doc);
        }

        public string GetActiveDirectoryContainer()
        {
            var doc = configurationStore.Get<DirectoryServicesConfiguration>(SingletonId);
            if (doc != null)
                return doc.ActiveDirectoryContainer;

            doc = MoveSettingsToDatabase();

            return doc.ActiveDirectoryContainer;
        }

        public void SetActiveDirectoryContainer(string activeDirectoryContainer)
        {
            var doc = configurationStore.Get<DirectoryServicesConfiguration>(SingletonId) ?? MoveSettingsToDatabase();
            doc.ActiveDirectoryContainer = activeDirectoryContainer;
            configurationStore.Update(doc);
        }

        public AuthenticationSchemes GetAuthenticationScheme()
        {
            var doc = configurationStore.Get<DirectoryServicesConfiguration>(SingletonId);
            if (doc != null)
                return doc.AuthenticationScheme;

            doc = MoveSettingsToDatabase();

            return doc.AuthenticationScheme;
        }

        public void SetAuthenticationScheme(AuthenticationSchemes scheme)
        {
            var doc = configurationStore.Get<DirectoryServicesConfiguration>(SingletonId) ?? MoveSettingsToDatabase();
            doc.AuthenticationScheme = scheme;
            configurationStore.Update(doc);
        }

        public bool GetAllowFormsAuthenticationForDomainUsers()
        {
            var doc = configurationStore.Get<DirectoryServicesConfiguration>(SingletonId);
            if (doc != null)
                return doc.AllowFormsAuthenticationForDomainUsers;

            doc = MoveSettingsToDatabase();

            return doc.AllowFormsAuthenticationForDomainUsers;
        }

        public void SetAllowFormsAuthenticationForDomainUsers(bool allowFormAuth)
        {
            var doc = configurationStore.Get<DirectoryServicesConfiguration>(SingletonId) ?? MoveSettingsToDatabase();
            doc.AllowFormsAuthenticationForDomainUsers = allowFormAuth;
            configurationStore.Update(doc);
        }

        public bool GetAreSecurityGroupsEnabled()
        {
            var doc = configurationStore.Get<DirectoryServicesConfiguration>(SingletonId);
            if (doc != null)
                return doc.AreSecurityGroupsEnabled;

            doc = MoveSettingsToDatabase();

            return doc.AreSecurityGroupsEnabled;
        }

        public void SetAreSecurityGroupsEnabled(bool areSecurityGroupsEnabled)
        {
            var doc = configurationStore.Get<DirectoryServicesConfiguration>(SingletonId) ?? MoveSettingsToDatabase();
            doc.AreSecurityGroupsEnabled = areSecurityGroupsEnabled;
            configurationStore.Update(doc);
        }

        readonly string[] legacyModes = {"Domain", "1"};

        DirectoryServicesConfiguration MoveSettingsToDatabase()
        {
            log.Info("Moving Octopus.WebPortal.ActiveDirectoryContainer/AuthenticationScheme/AllowFormsAuthenticationForDomainUsers from config file to DB");

            var activeDirectoryContainer = settings.Get("Octopus.WebPortal.ActiveDirectoryContainer", string.Empty);
            var authenticationScheme = settings.Get("Octopus.WebPortal.AuthenticationScheme", AuthenticationSchemes.Ntlm);
            var allowFormsAuth = settings.Get("Octopus.WebPortal.AllowFormsAuthenticationForDomainUsers", true);
            var areSecurityGroupsDisabled = settings.Get("Octopus.WebPortal.ExternalSecurityGroupsDisabled", false);

            var authenticationMode = authenticationConfigurationStore.GetAuthenticationMode();
            var doc = new DirectoryServicesConfiguration("DirectoryServices", "Octopus Deploy")
            {
                IsEnabled = legacyModes.Any(x => x.Equals(authenticationMode.Replace("\"", ""), StringComparison.InvariantCultureIgnoreCase)),
                ActiveDirectoryContainer = activeDirectoryContainer,
                AuthenticationScheme = authenticationScheme,
                AllowFormsAuthenticationForDomainUsers = allowFormsAuth,
                AreSecurityGroupsEnabled = !areSecurityGroupsDisabled
            };

            configurationStore.Create(doc);

            settings.Remove("Octopus.WebPortal.ActiveDirectoryContainer");
            settings.Remove("Octopus.WebPortal.AuthenticationScheme");
            settings.Remove("Octopus.WebPortal.AllowFormsAuthenticationForDomainUsers");
            settings.Remove("Octopus.WebPortal.ExternalSecurityGroupsDisabled");
            settings.Save();

            return doc;
        }

        public string ConfigurationSetName => "Active Directory";
        public IEnumerable<ConfigurationValue> GetConfigurationValues()
        {
            yield return new ConfigurationValue("Octopus.WebPortal.ActiveDirectoryIsEnabled", GetIsEnabled().ToString(), GetIsEnabled(), "Is Enabled");
            yield return new ConfigurationValue("Octopus.WebPortal.ActiveDirectoryContainer", GetActiveDirectoryContainer(), GetIsEnabled() && !string.IsNullOrWhiteSpace(GetActiveDirectoryContainer()), "Active Directory Container");
            yield return new ConfigurationValue("Octopus.WebPortal.AuthenticationScheme", GetAuthenticationScheme().ToString(), GetIsEnabled(), "Authentication Scheme");
            yield return new ConfigurationValue("Octopus.WebPortal.AllowFormsAuthenticationForDomainUsers", GetAllowFormsAuthenticationForDomainUsers().ToString(), GetIsEnabled(), "Allow forms authentication");
            yield return new ConfigurationValue("Octopus.WebPortal.ActiveDirectorySecurityGroupsEnabled", GetAreSecurityGroupsEnabled().ToString(), GetIsEnabled(), "Security groups enabled");
        }
    }
}
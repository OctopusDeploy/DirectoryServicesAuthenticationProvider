using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Octopus.Data.Model.User;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Identities;
using Octopus.Server.Extensibility.Authentication.Extensions;
using Octopus.Server.Extensibility.Results;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    class GroupRetriever : IExternalGroupRetriever
    {
        readonly ISystemLog log;
        readonly IDirectoryServicesConfigurationStore configurationStore;
        readonly IDirectoryServicesExternalSecurityGroupLocator groupLocator;

        public GroupRetriever(
            ISystemLog log,
            IDirectoryServicesConfigurationStore configurationStore,
            IDirectoryServicesExternalSecurityGroupLocator groupLocator)
        {
            this.log = log;
            this.configurationStore = configurationStore;
            this.groupLocator = groupLocator;
        }

        public string IdentityProviderName => DirectoryServicesAuthentication.ProviderName;

        public IResultFromExtension<ExternalGroupResult> Read(IUser user, CancellationToken cancellationToken)
        {
            if (!configurationStore.GetIsEnabled() ||
                !configurationStore.GetAreSecurityGroupsEnabled())
                return ResultFromExtension<ExternalGroupResult>.ExtensionDisabled();
            if (user.Username == User.GuestLogin)
                return ResultFromExtension<ExternalGroupResult>.Failed("Not valid for Guest user");
            if (user.Identities.All(p => p.IdentityProviderName != DirectoryServicesAuthentication.ProviderName))
                return ResultFromExtension<ExternalGroupResult>.Failed("No identities matching this provider");

            // if the user has multiple, unique identities assigned then the group list should be the distinct union of the groups from
            // all of the identities
            var wasAbleToRetrieveSomeGroups = false;
            var newGroups = new HashSet<string>();
            var adIdentities = user.Identities.Where(p => p.IdentityProviderName == DirectoryServicesAuthentication.ProviderName);
            foreach (var adIdentity in adIdentities)
            {
                var samAccountName = adIdentity.Claims[IdentityCreator.SamAccountNameClaimType].Value;

                if (string.IsNullOrWhiteSpace(samAccountName))
                    continue;

                var result = groupLocator.GetGroupIdsForUser(samAccountName, cancellationToken);
                if (result.WasAbleToRetrieveGroups)
                {
                    foreach (var groupId in result.GroupsIds.Where(g => !newGroups.Contains(g)))
                    {
                        newGroups.Add(groupId);
                    }

                    wasAbleToRetrieveSomeGroups = true;

                }
                else
                {
                    log.WarnFormat("Couldn't retrieve groups for samAccountName {0}", samAccountName);
                }
            }

            if (!wasAbleToRetrieveSomeGroups)
            {
                log.ErrorFormat("Couldn't retrieve groups for user {0}", user.Username);
                return ResultFromExtension<ExternalGroupResult>.Failed($"Couldn't retrieve groups for user {user.Username}");
            }

            return ResultFromExtension<ExternalGroupResult>.Success(new ExternalGroupResult(newGroups.Select(g => g).ToArray()));
        }
    }
}
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading;
using Octopus.Data.Model.User;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Identities;
using Octopus.Server.Extensibility.Authentication.Extensions;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    class UserMatcher : ICanMatchExternalUser
    {
        readonly IDirectoryServicesContextProvider contextProvider;
        readonly IDirectoryServicesObjectNameNormalizer objectNameNormalizer;
        readonly IDirectoryServicesConfigurationStore configurationStore;
        readonly IIdentityCreator identityCreator;

        public UserMatcher(
            IDirectoryServicesContextProvider contextProvider,
            IDirectoryServicesObjectNameNormalizer objectNameNormalizer,
            IDirectoryServicesConfigurationStore configurationStore,
            IIdentityCreator identityCreator)
        {
            this.contextProvider = contextProvider;
            this.objectNameNormalizer = objectNameNormalizer;
            this.configurationStore = configurationStore;
            this.identityCreator = identityCreator;
        }

        public string IdentityProviderName => DirectoryServicesAuthentication.ProviderName;

        public Identity? Match(string name, CancellationToken cancellationToken)
        {
            if (!configurationStore.GetIsEnabled())
                return null;

            var domainUser = objectNameNormalizer.NormalizeName(name);

            using (var context = contextProvider.GetContext(domainUser.Domain))
            {
                if (cancellationToken.IsCancellationRequested) return null;

                var userPrincipal = new UserPrincipal(context);

                if (domainUser.NormalizedName.Contains("@"))
                {
                    userPrincipal.UserPrincipalName = domainUser.NormalizedName;
                }
                else
                {
                    userPrincipal.SamAccountName = domainUser.NormalizedName;
                }

                var searcher = new PrincipalSearcher
                {
                    QueryFilter = userPrincipal
                };

                var users = searcher.FindAll();
                if (!users.Any() || users.Count() > 1)
                    return null;

                return users
                    .Select(u => identityCreator.Create("", u.UserPrincipalName, ConvertSamAccountName(u, domainUser.Domain), u.DisplayName))
                    .First();
            }
        }

        static string ConvertSamAccountName(Principal u, string? domain)
        {
            return !string.IsNullOrWhiteSpace(domain) ? $"{domain}\\{u.SamAccountName}" : u.SamAccountName;
        }
    }
}
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Identities;
using Octopus.Server.Extensibility.Authentication.Extensions;
using Octopus.Server.Extensibility.Results;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    class UserSearch : ICanSearchActiveDirectoryUsers
    {
        readonly IDirectoryServicesContextProvider contextProvider;
        readonly IDirectoryServicesObjectNameNormalizer objectNameNormalizer;
        readonly IDirectoryServicesConfigurationStore configurationStore;
        readonly IIdentityCreator identityCreator;

        public UserSearch(
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

        public IResultFromExtension<ExternalUserLookupResult> Search(string searchTerm, CancellationToken cancellationToken)
        {
            if (!configurationStore.GetIsEnabled())
                return ResultFromExtension<ExternalUserLookupResult>.ExtensionDisabled();

            var domainUser = objectNameNormalizer.NormalizeName(searchTerm);

            using (var context = contextProvider.GetContext(domainUser.Domain))
            {
                if (cancellationToken.IsCancellationRequested) return ResultFromExtension<ExternalUserLookupResult>.Failed("Cancellation requested.");

                var identities = new List<Principal>(SearchBy(new UserPrincipal(context) { Name = "*" + domainUser.NormalizedName + "*" }));
                identities.AddRange(SearchBy(new UserPrincipal(context) { UserPrincipalName = "*" + domainUser.NormalizedName + "*" }));
                identities.AddRange(SearchBy(new UserPrincipal(context) { SamAccountName = "*" + domainUser.NormalizedName + "*" }));

                var identityResources = identities.Distinct(new PrincipalComparer())
                    .Select(u => identityCreator.Create("", u.UserPrincipalName, ConvertSamAccountName(u, domainUser.Domain),
                        u.DisplayName).ToResource())
                    .ToArray();

                return ResultFromExtension<ExternalUserLookupResult>.Success(new ExternalUserLookupResult(identityResources));
            }
        }

        IEnumerable<Principal> SearchBy(UserPrincipal queryFilter)
        {
            var searcher = new PrincipalSearcher
            {
                QueryFilter = queryFilter
            };

            return searcher.FindAll();
        }

        static string ConvertSamAccountName(Principal u, string? domain)
        {
            return !string.IsNullOrWhiteSpace(domain) ? $"{domain}\\{u.SamAccountName}" : u.SamAccountName;
        }
    }

    interface ICanSearchActiveDirectoryUsers : ICanSearchExternalUsers
    { }
}
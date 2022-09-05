using System.Security.Principal;
using System.Threading;
using Octopus.Data.Model.User;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.Extensions;
using Octopus.Server.Extensibility.Results;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    class DirectoryServicesUserCreationFromPrincipal : ISupportsAutoUserCreationFromPrincipal
    {
        readonly IDirectoryServicesConfigurationStore configurationStore;
        readonly IDirectoryServicesCredentialValidator credentialValidator;

        public DirectoryServicesUserCreationFromPrincipal(
            IDirectoryServicesConfigurationStore configurationStore,
            IDirectoryServicesCredentialValidator credentialValidator)
        {
            this.configurationStore = configurationStore;
            this.credentialValidator = credentialValidator;
        }

        public string IdentityProviderName => DirectoryServicesAuthentication.ProviderName;

        public IResultFromExtension<IUser> GetOrCreateUser(IPrincipal principal, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(principal.Identity?.Name))
                        return ResultFromExtension<IUser>.Failed();

            return !configurationStore.GetIsEnabled() ?
                ResultFromExtension<IUser>.ExtensionDisabled() :
                credentialValidator.GetOrCreateUser(principal.Identity.Name, cancellationToken);
        }
    }
}
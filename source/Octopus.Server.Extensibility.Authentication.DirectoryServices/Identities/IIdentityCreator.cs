using Octopus.Data.Model.User;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Identities
{
    interface IIdentityCreator
    {
        Identity Create(string email, string upn, string samAccountName, string displayName);
    }
}
using System.DirectoryServices.AccountManagement;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    interface IUserPrincipalFinder
    {
        IUserPrincipalWrapper? FindByIdentity(PrincipalContext context, string samAccountName);
    }

    class UserPrincipalFinder : IUserPrincipalFinder
    {
        public IUserPrincipalWrapper? FindByIdentity(PrincipalContext context, string samAccountName)
        {
            var findByIdentity = UserPrincipal.FindByIdentity(context, samAccountName);
            return findByIdentity is null ? null : new UserPrincipalWrapper(findByIdentity);
        }
    }
}
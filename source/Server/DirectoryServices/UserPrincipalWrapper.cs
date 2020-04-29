using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    interface IUserPrincipalWrapper : IPrincipalWrapper, IDisposable
    {
        IEnumerable<IPrincipalWrapper> GetAuthorizationGroups();
        IEnumerable<IPrincipalWrapper> GetGroups();
    }

    class UserPrincipalWrapper : PrincipalWrapper, IUserPrincipalWrapper
    {
        readonly UserPrincipal userPrincipal;

        public UserPrincipalWrapper(UserPrincipal userPrincipal)
            : base(userPrincipal)
        {
            this.userPrincipal = userPrincipal;
        }

        public void Dispose()
        {
            userPrincipal?.Dispose();
        }

        public IEnumerable<IPrincipalWrapper> GetAuthorizationGroups()
            => userPrincipal.GetAuthorizationGroups().TrySelect(p => new PrincipalWrapper(p));

        public IEnumerable<IPrincipalWrapper> GetGroups()
            => userPrincipal.GetGroups().TrySelect(p => new PrincipalWrapper(p));
    }
}
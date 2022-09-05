using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Threading;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    interface IUserPrincipalWrapper : IPrincipalWrapper, IDisposable
    {
        IEnumerable<IPrincipalWrapper> GetAuthorizationGroups(CancellationToken cancellationToken);
        IEnumerable<IPrincipalWrapper> GetGroups(CancellationToken cancellationToken);
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

        public IEnumerable<IPrincipalWrapper> GetAuthorizationGroups(CancellationToken cancellationToken)
            => GetList(userPrincipal.GetAuthorizationGroups(), cancellationToken);

        public IEnumerable<IPrincipalWrapper> GetGroups(CancellationToken cancellationToken)
            => GetList(userPrincipal.GetGroups(), cancellationToken);

        static IEnumerable<IPrincipalWrapper> GetList(PrincipalSearchResult<Principal> principalResult,
            CancellationToken cancellationToken)
        {
            var results = new List<IPrincipalWrapper>();

            var iterGroup = principalResult.GetEnumerator();
            using (iterGroup)
            {
                while (iterGroup.MoveNext())
                {
                    try
                    {
                        var p = iterGroup.Current;
                        results.Add(new PrincipalWrapper(p));
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                    catch (NoMatchingPrincipalException)
                    {
                    }
                }
            }

            return results;
        }
    }
}
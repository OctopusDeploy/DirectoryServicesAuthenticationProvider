using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    class PrincipalComparer : IEqualityComparer<Principal>
    {
        public bool Equals(Principal x, Principal y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Principal obj)
        {
            return obj.Sid.GetHashCode();
        }
    }
}
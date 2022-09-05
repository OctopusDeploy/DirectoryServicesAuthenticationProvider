using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    [SupportedOSPlatform("Windows")]
    internal class PrincipalComparer : IEqualityComparer<Principal>
    {
        public bool Equals(Principal? x, Principal? y)
        {
            return x != null && x.Equals(y);
        }

        public int GetHashCode(Principal obj)
        {
            return obj.Sid.GetHashCode();
        }
    }
}
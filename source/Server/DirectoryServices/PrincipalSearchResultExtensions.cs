using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    public static class PrincipalSearchResultExtensions
    {
        public static IEnumerable<TOut> TrySelect<T, TOut>(this PrincipalSearchResult<T> collection, Func<T, TOut> projection) where T : Principal
        {
            var results = new List<TOut>();
            
            try
            {
                foreach (var result in collection) results.Add(projection(result));
            }
            catch (NoMatchingPrincipalException)
            {
            }

            return results;
        }
    }
}
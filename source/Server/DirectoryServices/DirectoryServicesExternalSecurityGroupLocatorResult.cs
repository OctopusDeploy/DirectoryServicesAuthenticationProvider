using System.Collections.Generic;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    class DirectoryServicesExternalSecurityGroupLocatorResult
    {
        public DirectoryServicesExternalSecurityGroupLocatorResult()
        {
        }

        public DirectoryServicesExternalSecurityGroupLocatorResult(IList<string> groupsIds)
        {
            WasAbleToRetrieveGroups = true;
            GroupsIds = groupsIds;
        }

        public bool WasAbleToRetrieveGroups { get; }
        public IList<string>? GroupsIds { get; }
    }
}
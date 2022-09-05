using System.Threading;
using Octopus.Server.Extensibility.Authentication.Extensions;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    interface IDirectoryServicesExternalSecurityGroupLocator : ICanSearchExternalGroups
    {
        DirectoryServicesExternalSecurityGroupLocatorResult GetGroupIdsForUser(string samAccountName, CancellationToken cancellationToken);
    }
}
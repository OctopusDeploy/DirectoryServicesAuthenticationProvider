using System.Threading;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    interface IDirectoryServicesService
    {
        UserValidationResult ValidateCredentials(string username, string password, CancellationToken cancellationToken);
        UserValidationResult FindByIdentity(string username);
    }
}
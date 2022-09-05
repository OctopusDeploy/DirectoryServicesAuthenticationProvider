using System.Threading;
using Octopus.Data.Model.User;
using Octopus.Server.Extensibility.Authentication.Extensions;
using Octopus.Server.Extensibility.Results;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    interface IDirectoryServicesCredentialValidator : IDoesBasicAuthentication
    {
        IResultFromExtension<IUser> GetOrCreateUser(string username, CancellationToken cancellationToken);
    }
}
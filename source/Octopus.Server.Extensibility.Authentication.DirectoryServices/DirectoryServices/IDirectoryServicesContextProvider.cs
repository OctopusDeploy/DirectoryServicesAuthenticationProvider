using System.DirectoryServices.AccountManagement;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    interface IDirectoryServicesContextProvider
    {
        PrincipalContext GetContext(string? domain);
    }
}
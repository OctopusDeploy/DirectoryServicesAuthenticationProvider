namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    interface IDirectoryServicesObjectNameNormalizer
    {
        DomainUser NormalizeName(string name);

        string ValidatedUserPrincipalName(string? userPrincipalName, string? fallbackUsername, string? fallbackDomain);
    }
}
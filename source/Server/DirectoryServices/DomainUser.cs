namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    class DomainUser
    {
        public DomainUser(string? domain, string normalizedName)
        {
            Domain = domain;
            NormalizedName = normalizedName;
        }

        public string? Domain { get; }
        public string NormalizedName { get; }
    }
}
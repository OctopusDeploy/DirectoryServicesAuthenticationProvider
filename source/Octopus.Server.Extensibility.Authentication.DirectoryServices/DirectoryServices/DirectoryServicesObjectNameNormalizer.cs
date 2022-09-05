using System;
using Octopus.Diagnostics;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    class DirectoryServicesObjectNameNormalizer : IDirectoryServicesObjectNameNormalizer
    {
        readonly ISystemLog log;
        const string NTAccountUsernamePrefix = "nt:";

        public DirectoryServicesObjectNameNormalizer(ISystemLog log)
        {
            this.log = log;
        }

        public DomainUser NormalizeName(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (name.StartsWith(NTAccountUsernamePrefix))
                name = name.Remove(0, NTAccountUsernamePrefix.Length);

            var domainUser = TryParseDownLevelLogonName(name);
            if (domainUser == null)
            {
                domainUser = new DomainUser(null, name);
            }

            return domainUser;
        }

        public string ValidatedUserPrincipalName(string? userPrincipalName, string? fallbackUsername, string? fallbackDomain)
        {
            var name = userPrincipalName;
            if (name == null)
            {
                log.Warn($"The user name (UPN) could not be determined for principal - falling back to NT-style '{fallbackDomain}\\{fallbackUsername}'");
                if (string.IsNullOrWhiteSpace(fallbackDomain))
                    throw new InvalidOperationException("No fallback domain was provided");
                if (string.IsNullOrWhiteSpace(fallbackUsername))
                    throw new InvalidOperationException("No fallback name was provided");
                name = NTAccountUsernamePrefix + fallbackDomain.Trim() + "\\" + fallbackUsername.Trim();
            }
            return name;
        }

        // If the return value is true, dlln was a valid down-level logon name, and name/domain
        // contain precisely the component name and domain name values. Note, we don't split
        // UPNs this way because the suffix part of a UPN is not necessarily a domain, and in
        // the default LogonUser case should be passed whole to the function with a null domain.
        static DomainUser? TryParseDownLevelLogonName(string dlln)
        {
            if (dlln == null) throw new ArgumentNullException(nameof(dlln));

            var slash = dlln.IndexOf('\\');
            if (slash == -1 || slash == dlln.Length - 1 || slash == 0)
                return null;

            var domain = dlln.Substring(0, slash).Trim();
            var username = dlln.Substring(slash + 1).Trim();
            return !string.IsNullOrWhiteSpace(domain) && !string.IsNullOrWhiteSpace(username) ? new DomainUser(domain,  username) : null;
        }
    }
}
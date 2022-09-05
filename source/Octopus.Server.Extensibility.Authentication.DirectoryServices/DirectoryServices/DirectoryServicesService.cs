using System;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Threading;
using Octopus.Diagnostics;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    class DirectoryServicesService : IDirectoryServicesService
    {
        readonly ISystemLog log;
        readonly IDirectoryServicesObjectNameNormalizer objectNameNormalizer;
        readonly IDirectoryServicesContextProvider contextProvider;

        /// <summary>
        /// This logon type is intended for high performance servers to authenticate plaintext passwords.
        /// The LogonUser function does not cache credentials for this logon type.
        /// </summary>
        const int LOGON32_LOGON_NETWORK = 3;

        /// <summary>
        /// Use the standard logon provider for the system.
        /// The default security provider is negotiate, unless you pass NULL for the domain name and the user name
        /// is not in UPN format. In this case, the default provider is NTLM.
        /// NOTE: Windows 2000/NT:   The default security provider is NTLM.
        /// </summary>
        const int LOGON32_PROVIDER_DEFAULT = 0;

        public DirectoryServicesService(ISystemLog log,
            IDirectoryServicesObjectNameNormalizer objectNameNormalizer,
            IDirectoryServicesContextProvider contextProvider)
        {
            this.log = log;
            this.objectNameNormalizer = objectNameNormalizer;
            this.contextProvider = contextProvider;
        }

        public UserValidationResult ValidateCredentials(string username, string password, CancellationToken cancellationToken)
        {
            log.Verbose($"Validating credentials provided for '{username}'...");

            var domainUser = objectNameNormalizer.NormalizeName(username);

            using (var context = contextProvider.GetContext(domainUser.Domain))
            {
                var principal = UserPrincipal.FindByIdentity(context, domainUser.NormalizedName);

                if (principal == null)
                {
                    var searchedContext = domainUser.Domain ?? context.Name ?? context.ConnectedServer;
                    log.Info($"A principal identifiable by '{username}' was not found in '{searchedContext}'");
                    if (domainUser.NormalizedName.Contains("@"))
                    {
                        return new UserValidationResult("Invalid username or password.  UPN format may not be supported for your domain configuration.");
                    }
                    return new UserValidationResult("Invalid username or password.");
                }

                var hToken = IntPtr.Zero;
                try
                {
                    var logon = domainUser.Domain == null ? principal.UserPrincipalName : domainUser.NormalizedName;
                    log.Verbose($"Calling LogonUser(\"{logon}\", \"{domainUser.Domain}\", ...)");

                    if (!LogonUser(logon, domainUser.Domain, password, LOGON32_LOGON_NETWORK, LOGON32_PROVIDER_DEFAULT, out hToken))
                    {
                        var error = new Win32Exception();
                        log.Warn(error, $"Principal '{logon}' (Domain: '{domainUser.Domain}') could not be logged on via WIN32: 0x{error.NativeErrorCode:X8}.");

                        return new UserValidationResult("Invalid username or password.");
                    }
                }
                finally
                {
                    if (hToken != IntPtr.Zero) CloseHandle(hToken);
                }

                log.Verbose($"Credentials for '{username}' validated, mapped to principal '{principal.UserPrincipalName ?? ("(NTAccount)" + principal.Name)}'");

                return new UserValidationResult(principal, domainUser.Domain);
            }
        }

        public UserValidationResult FindByIdentity(string username)
        {
            var domainUser = objectNameNormalizer.NormalizeName(username);

            using (var context = contextProvider.GetContext(domainUser.Domain))
            {
                var principal = UserPrincipal.FindByIdentity(context, domainUser.NormalizedName);

                if (principal == null)
                {
                    var searchedContext = domainUser.Domain ?? context.Name ?? context.ConnectedServer;
                    return new UserValidationResult($"A principal identifiable by '{username}' was not found in '{searchedContext}'");
                }
                return new UserValidationResult(principal, domainUser.Domain);
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LogonUser(
            string lpszUsername,
            string? lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out IntPtr phToken
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);
    }
}
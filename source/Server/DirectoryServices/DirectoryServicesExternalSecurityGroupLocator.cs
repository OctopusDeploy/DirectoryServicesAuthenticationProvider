using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.Extensions;
using Octopus.Server.Extensibility.Authentication.HostServices;
using Octopus.Server.Extensibility.Results;
using Polly;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    class DirectoryServicesExternalSecurityGroupLocator : IDirectoryServicesExternalSecurityGroupLocator
    {
        readonly ISystemLog log;
        readonly IDirectoryServicesContextProvider contextProvider;
        readonly IDirectoryServicesObjectNameNormalizer objectNameNormalizer;
        readonly IDirectoryServicesConfigurationStore configurationStore;
        readonly IUserPrincipalFinder userPrincipalFinder;

        public DirectoryServicesExternalSecurityGroupLocator(
            ISystemLog log,
            IDirectoryServicesContextProvider contextProvider,
            IDirectoryServicesObjectNameNormalizer objectNameNormalizer,
            IDirectoryServicesConfigurationStore configurationStore,
            IUserPrincipalFinder userPrincipalFinder)
        {
            this.log = log;
            this.contextProvider = contextProvider;
            this.objectNameNormalizer = objectNameNormalizer;
            this.configurationStore = configurationStore;
            this.userPrincipalFinder = userPrincipalFinder;
        }

        public string IdentityProviderName => DirectoryServicesAuthentication.ProviderName;

        public IResultFromExtension<ExternalSecurityGroupResult> Search(string name, CancellationToken cancellationToken)
        {
            if (!configurationStore.GetIsEnabled() || !configurationStore.GetAreSecurityGroupsEnabled())
                return ResultFromExtension<ExternalSecurityGroupResult>.ExtensionDisabled();

            var groups = FindGroups(name, cancellationToken);
            var result = new ExternalSecurityGroupResult(groups);

            return ResultFromExtension<ExternalSecurityGroupResult>.Success(result);
        }

        ExternalSecurityGroup[] FindGroups(string name, CancellationToken cancellationToken)
        {
            if (!configurationStore.GetAreSecurityGroupsEnabled())
                return Array.Empty<ExternalSecurityGroup>();

            var results = new List<ExternalSecurityGroup>();
            var domainUser = objectNameNormalizer.NormalizeName(name);
            using (var context = contextProvider.GetContext(domainUser.Domain))
            {
                var searcher = new PrincipalSearcher();
                searcher.QueryFilter = new GroupPrincipal(context) { Name = "*" + domainUser.NormalizedName + "*" };

                var iterGroup = searcher.FindAll().GetEnumerator();
                using (iterGroup)
                {
                    while (iterGroup.MoveNext())
                    {
                        try
                        {
                            var p = iterGroup.Current as GroupPrincipal;
                            if (p == null || !(p.IsSecurityGroup ?? false))
                                continue;

                            results.Add(new ExternalSecurityGroup { Id = p.Sid.ToString(), DisplayName = p.Name });
                        }
                        catch (NoMatchingPrincipalException)
                        {
                        }

                        if (cancellationToken.IsCancellationRequested) return Array.Empty<ExternalSecurityGroup>();
                    }
                }
            }

            return results.OrderBy(o => o.DisplayName).ToArray();
        }

        public DirectoryServicesExternalSecurityGroupLocatorResult GetGroupIdsForUser(string samAccountName, CancellationToken cancellationToken)
        {
            if (samAccountName == null) throw new ArgumentNullException(nameof(samAccountName), "The external identity is null indicating we were not able to associate this Octopus User Account with an identifier from Active Directory.");

            if (!configurationStore.GetAreSecurityGroupsEnabled())
                return new DirectoryServicesExternalSecurityGroupLocatorResult(new List<string>());

            var groups = new List<string>();

            try
            {
                log.Verbose($"Finding external security groups for '{samAccountName}'...");

                var domainUser = objectNameNormalizer.NormalizeName(samAccountName);

                using (var context = contextProvider.GetContext(domainUser.Domain))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (var principal = userPrincipalFinder.FindByIdentity(context, domainUser.NormalizedName))
                    {
                        if (principal == null)
                        {
                            var searchedContext = domainUser.Domain ?? context.Name ?? context.ConnectedServer;
                            log.Trace(
                                $"While loading security groups, a principal identifiable by '{samAccountName}' was not found in '{searchedContext}'");
                            return new DirectoryServicesExternalSecurityGroupLocatorResult();
                        }

                        try
                        {
                            // Reads inherited groups - this fails in some situations
                            ReadAuthorizationGroups(principal, groups, cancellationToken);
                            return new DirectoryServicesExternalSecurityGroupLocatorResult(groups);
                        }
                        catch (Exception ex) when (!(ex is PrincipalServerDownException))
                        {
                            // Don't log it as an Error, it's expected to fail in some situations
                            log.Verbose(ex);
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // Reads just the groups they are a member of - more reliable but not ideal
                    using (var principal = userPrincipalFinder.FindByIdentity(context, samAccountName))
                        ReadUserGroups(principal, groups, cancellationToken);

                    return new DirectoryServicesExternalSecurityGroupLocatorResult(groups);
                }
            }
            catch (OperationCanceledException)
            {
                return new DirectoryServicesExternalSecurityGroupLocatorResult();
            }
            catch (Exception ex)
            {
                log.ErrorFormat(ex, "Active Directory search for {0} failed.", samAccountName);
                return new DirectoryServicesExternalSecurityGroupLocatorResult();
            }
        }

        static void ReadAuthorizationGroups(IUserPrincipalWrapper principal, ICollection<string> groups, CancellationToken cancellationToken)
        {
            var retryPolicy = Policy
                .Handle<PrincipalOperationException>()
                .WaitAndRetry(5, retryAttempt => TimeSpan.FromMilliseconds(250 * retryAttempt));

            retryPolicy.Execute(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ReadGroups(principal.GetAuthorizationGroups(cancellationToken), groups);
            });
        }

        static void ReadUserGroups(IUserPrincipalWrapper principal, ICollection<string> groups, CancellationToken cancellationToken)
        {
            ReadGroups(principal.GetGroups(cancellationToken), groups);
        }

        static void ReadGroups(IEnumerable<IPrincipalWrapper> groupPrincipals, ICollection<string> groups)
        {
            foreach (var principal in groupPrincipals)
            {
                groups.Add(principal.Sid.Value);
            }
        }
    }
}
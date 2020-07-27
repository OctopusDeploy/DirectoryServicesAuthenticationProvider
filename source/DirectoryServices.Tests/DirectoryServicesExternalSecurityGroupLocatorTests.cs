using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.Threading;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;
using Shouldly;

namespace DirectoryServices.Tests
{
    public class DirectoryServicesExternalSecurityGroupLocatorTests
    {
        const string groupSid = "S-1-5-32-544";

        DirectoryServicesExternalSecurityGroupLocator locator;
        IUserPrincipalFinder userPrincipalFinder;
        InMemoryLog log;

        [SetUp]
        public void SetUp()
        {
            log = new InMemoryLog();
            var configurationStore = Substitute.For<IDirectoryServicesConfigurationStore>();

            var contextProvider = Substitute.For<IDirectoryServicesContextProvider>();

            userPrincipalFinder = Substitute.For<IUserPrincipalFinder>();

            var directoryServicesObjectNameNormalizer = Substitute.For<IDirectoryServicesObjectNameNormalizer>();
            directoryServicesObjectNameNormalizer.NormalizeName(Arg.Any<string>())
                .Returns(x => new DomainUser(null, ((string)x.Args()[0])));
            
            locator = new DirectoryServicesExternalSecurityGroupLocator(
                log,
                contextProvider,
                directoryServicesObjectNameNormalizer,
                configurationStore,
                userPrincipalFinder
            );
            
            configurationStore.GetAreSecurityGroupsEnabled().Returns(true);
            contextProvider.GetContext(null).ReturnsForAnyArgs(new PrincipalContext(ContextType.Machine));

        }
        
        [Test]
        public void GetGroupIdsForUser_NotFound()
        {
            userPrincipalFinder.FindByIdentity(null, null).ReturnsForAnyArgs((IUserPrincipalWrapper) null);
                
            var result = locator.GetGroupIdsForUser("Bob", CancellationToken.None);
            result.WasAbleToRetrieveGroups.ShouldBeFalse();
            
            log.Logs.ShouldContain((LogCategory.Trace, null, $"While loading security groups, a principal identifiable by 'Bob' was not found in '{Environment.MachineName}'"));
        }
        
        [Test]
        public void GetGroupIdsForUser_ErrorGettingAuthorizationGroups()
        {
            
            var userPrincipal = Substitute.For<IUserPrincipalWrapper>();
            userPrincipalFinder.FindByIdentity(null, null).ReturnsForAnyArgs(userPrincipal);

            var authGroupsException = new Exception("AuthorizationGroups Exception");
            userPrincipal.GetAuthorizationGroups(CancellationToken.None).ThrowsForAnyArgs(authGroupsException);
            userPrincipal.GetGroups(CancellationToken.None).Returns(new[] {new FakeGroupPrincipal(groupSid)});
                
            var result = locator.GetGroupIdsForUser("Bob", CancellationToken.None);
            result.WasAbleToRetrieveGroups.ShouldBeTrue();
            result.GroupsIds.ShouldBe(new[] { groupSid});
     
            log.Logs.ShouldContain(l => l.category == LogCategory.Verbose && l.exception == authGroupsException);
            log.Logs.ShouldNotContain(l => l.category == LogCategory.Error || l.category == LogCategory.Fatal);
        }

        [Test]
        public void GetGroupIdsForUser_ErrorGettingAnyGroups()
        {
            var userPrincipal = Substitute.For<IUserPrincipalWrapper>();
            userPrincipalFinder.FindByIdentity(null, null).ReturnsForAnyArgs(userPrincipal);

            var authGroupsException = new Exception("AuthorizationGroups Exception");
            userPrincipal.GetAuthorizationGroups(CancellationToken.None).ThrowsForAnyArgs(authGroupsException);
            
            var groupsException = new Exception("Groups Exception");
            userPrincipal.GetGroups(CancellationToken.None).ThrowsForAnyArgs(groupsException);
                
            var result = locator.GetGroupIdsForUser("Bob", CancellationToken.None);
            
            result.WasAbleToRetrieveGroups.ShouldBeFalse();
            result.GroupsIds.ShouldBeNull();
     
            log.Logs.ShouldContain(l => l.category == LogCategory.Verbose && l.exception == authGroupsException);
            log.Logs.ShouldContain(l => l.category == LogCategory.Error && l.exception == groupsException);
        }
        
        [Test]
        public void GetGroupIdsForUser_Success()
        {
            
            var userPrincipal = Substitute.For<IUserPrincipalWrapper>();
            userPrincipalFinder.FindByIdentity(null, null).ReturnsForAnyArgs(userPrincipal);

            userPrincipal.GetAuthorizationGroups(CancellationToken.None).ReturnsForAnyArgs(new[] {new FakeGroupPrincipal(groupSid)});
            
            var result = locator.GetGroupIdsForUser("Bob", CancellationToken.None);
            
            result.WasAbleToRetrieveGroups.ShouldBeTrue();
            result.GroupsIds.ShouldBe(new[] { groupSid});
            log.Logs.ShouldNotContain(l => l.category == LogCategory.Error || l.category == LogCategory.Fatal);

        }

        class FakeGroupPrincipal : IPrincipalWrapper
        {
            readonly string sid;

            public FakeGroupPrincipal(string sid)
            {
                this.sid = sid;
            }

            public SecurityIdentifier Sid => new SecurityIdentifier(sid);
        }
    }
}
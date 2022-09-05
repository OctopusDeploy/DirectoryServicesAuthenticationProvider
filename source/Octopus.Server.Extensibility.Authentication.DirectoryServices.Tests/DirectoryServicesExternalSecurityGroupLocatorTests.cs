using System;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;
using Shouldly;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Tests
{
    [SupportedOSPlatform("Windows")]
    public class DirectoryServicesExternalSecurityGroupLocatorTests
    {
        const string GroupSid = "S-1-5-32-544";

        IDirectoryServicesExternalSecurityGroupLocator locator = null!;
        IUserPrincipalFinder userPrincipalFinder = null!;
        ISystemLog log = null!;

        [SetUp]
        public void SetUp()
        {
            log = Substitute.For<ISystemLog>();
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
            userPrincipalFinder.FindByIdentity(Arg.Any<PrincipalContext>(), Arg.Any<string>()).Returns((IUserPrincipalWrapper) null!);

            var result = locator.GetGroupIdsForUser("Bob", CancellationToken.None);
            result.WasAbleToRetrieveGroups.ShouldBeFalse();

            log.Received().Trace($"While loading security groups, a principal identifiable by 'Bob' was not found in '{Environment.MachineName}'");
        }

        [Test]
        public void GetGroupIdsForUser_ErrorGettingAuthorizationGroups()
        {

            var userPrincipal = Substitute.For<IUserPrincipalWrapper>();
            userPrincipalFinder.FindByIdentity(Arg.Any<PrincipalContext>(), Arg.Any<string>()).Returns(userPrincipal);

            var authGroupsException = new Exception("AuthorizationGroups Exception");
            userPrincipal.GetAuthorizationGroups(CancellationToken.None).ThrowsForAnyArgs(authGroupsException);
            userPrincipal.GetGroups(CancellationToken.None).Returns(new[] {new FakeGroupPrincipal(GroupSid)});

            var result = locator.GetGroupIdsForUser("Bob", CancellationToken.None);
            result.WasAbleToRetrieveGroups.ShouldBeTrue();
            result.GroupsIds.ShouldBe(new[] { GroupSid});

            log.Received().Verbose(authGroupsException);
            log.DidNotReceive().Error(Arg.Any<string>());
            log.DidNotReceive().Error(Arg.Any<Exception>());
            log.DidNotReceive().Fatal(Arg.Any<string>());
            log.DidNotReceive().Fatal(Arg.Any<Exception>());
        }

        [Test]
        public void GetGroupIdsForUser_ErrorGettingAnyGroups()
        {
            var userPrincipal = Substitute.For<IUserPrincipalWrapper>();
            userPrincipalFinder.FindByIdentity(Arg.Any<PrincipalContext>(), Arg.Any<string>()).Returns(userPrincipal);

            var authGroupsException = new Exception("AuthorizationGroups Exception");
            userPrincipal.GetAuthorizationGroups(CancellationToken.None).ThrowsForAnyArgs(authGroupsException);

            var groupsException = new Exception("Groups Exception");
            userPrincipal.GetGroups(CancellationToken.None).ThrowsForAnyArgs(groupsException);

            var result = locator.GetGroupIdsForUser("Bob", CancellationToken.None);

            result.WasAbleToRetrieveGroups.ShouldBeFalse();
            result.GroupsIds.ShouldBeNull();

            log.Received().Verbose(authGroupsException);
            log.Received().ErrorFormat(groupsException, "Active Directory search for {0} failed.", "Bob");
        }

        [Test]
        public void GetGroupIdsForUser_Success()
        {

            var userPrincipal = Substitute.For<IUserPrincipalWrapper>();
            userPrincipalFinder.FindByIdentity(Arg.Any<PrincipalContext>(), Arg.Any<string>()).Returns(userPrincipal);

            userPrincipal.GetAuthorizationGroups(CancellationToken.None).ReturnsForAnyArgs(new[] {new FakeGroupPrincipal(GroupSid)});

            var result = locator.GetGroupIdsForUser("Bob", CancellationToken.None);

            result.WasAbleToRetrieveGroups.ShouldBeTrue();
            result.GroupsIds.ShouldBe(new[] { GroupSid});

            log.DidNotReceive().Error(Arg.Any<string>());
            log.DidNotReceive().Error(Arg.Any<Exception>());
            log.DidNotReceive().Fatal(Arg.Any<string>());
            log.DidNotReceive().Fatal(Arg.Any<Exception>());
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
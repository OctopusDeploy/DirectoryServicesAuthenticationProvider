using System;
using System.Collections.Generic;
using System.Threading;
using NSubstitute;
using NUnit.Framework;
using Octopus.Data;
using Octopus.Data.Model;
using Octopus.Data.Model.User;
using Octopus.Data.Storage.User;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Identities;
using Octopus.Server.Extensibility.Authentication.HostServices;
using Octopus.Server.Extensibility.Results;
using Shouldly;

namespace DirectoryServices.Tests
{
    [TestFixture]
    public class CredentialValidatorTests
    {
        IDirectoryServicesConfigurationStore directoryServicesConfigurationStore;
        IUpdateableUserStore updateableUserStore;
        IDirectoryServicesService directoryServicesService;
        DirectoryServicesCredentialValidator validator;
        IdentityCreator identityCreator;

        [SetUp]
        public void SetUp()
        {
            directoryServicesConfigurationStore = Substitute.For<IDirectoryServicesConfigurationStore>();
            directoryServicesConfigurationStore.GetIsEnabled().Returns(true);
            directoryServicesConfigurationStore.GetAllowFormsAuthenticationForDomainUsers().Returns(true);
            directoryServicesConfigurationStore.GetAllowAutoUserCreation().Returns(true);

            updateableUserStore = Substitute.For<IUpdateableUserStore>();

            directoryServicesService = Substitute.For<IDirectoryServicesService>();

            var log = Substitute.For<ILog>();

            identityCreator = new IdentityCreator();

            validator = new DirectoryServicesCredentialValidator(log,
                new DirectoryServicesObjectNameNormalizer(log),
                updateableUserStore,
                directoryServicesConfigurationStore,
                identityCreator,
                directoryServicesService);
        }
        [Test]
        public void InvalidUser()
        {
            directoryServicesService.ValidateCredentials("invalidUser", "testPassword", CancellationToken.None).Returns(new UserValidationResult("User not found"));

            var result = validator.ValidateCredentials("invalidUser", "testPassword", CancellationToken.None);

            result.ShouldBeAssignableTo<FailureResult>();
            ((FailureResult)result).ErrorString.ShouldBe("User not found");
            updateableUserStore.DidNotReceive().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
        }

        [Test]
        public void ExistingUserWithMatchingIdentity()
        {
            directoryServicesService.ValidateCredentials("existingUser", "testPassword", CancellationToken.None)
                .Returns(new UserValidationResult("existingUser@test.com", "existingUser", "TestDomain", string.Empty, String.Empty));

            var user = new User("Users-100", "existingUser", identityCreator.Create(string.Empty, "existingUser@test.com", "TestDomain\\existingUser", string.Empty));

            updateableUserStore.GetByIdentity(Arg.Any<Identity>()).Returns(new [] { user });

            var result = validator.ValidateCredentials("existingUser", "testPassword", CancellationToken.None);

            result.ShouldBeOfType<ResultFromExtension<IUser>>();
            updateableUserStore.DidNotReceive().UpdateIdentity(Arg.Any<string>(), Arg.Any<Identity>(), CancellationToken.None);
        }

        [Test]
        public void ExistingUserWithMultipleIdentities()
        {
            directoryServicesService.ValidateCredentials("existingUser1@test.com", "testPassword", CancellationToken.None)
                .Returns(new UserValidationResult("existingUser1@test.com", "\\existingUser1", "TestDomain", string.Empty, String.Empty));

            var user = new User("Users-100", "existingUser", identityCreator.Create(string.Empty, string.Empty, "TestDomain\\existingUser", string.Empty));
            user.Identities.Add(identityCreator.Create("existingUser@test.com", "existingUser1@test.com","TestDomain\\existingUser1", string.Empty));

            updateableUserStore.GetByIdentity(Arg.Any<Identity>()).Returns(new [] { user });
            directoryServicesService.FindByIdentity("TestDomain\\existingUser").Returns(new UserValidationResult("existingUser@test.com", "existingUser", "TestDomain", string.Empty, "tester@test.com"));

            var result = validator.ValidateCredentials("existingUser1@test.com", "testPassword", CancellationToken.None);

            result.ShouldBeOfType<ResultFromExtension<IUser>>();
            updateableUserStore.Received(1).UpdateIdentity(Arg.Any<string>(), Arg.Any<Identity>(), CancellationToken.None);
        }

        [Test]
        public void NewUserWithNoMatchingIdentity()
        {
            directoryServicesService.ValidateCredentials("newUser", "testPassword", CancellationToken.None)
                .Returns(new UserValidationResult("newUser@test.com", "newUser", "TestDomain", string.Empty, String.Empty));

            updateableUserStore.GetByIdentity(Arg.Any<Identity>()).Returns(new IUser[0]);

            var user = new User("Users-100", "newUser", identityCreator.Create(string.Empty, "newUser@test.com", "TestDomain\\newUser", string.Empty));
            updateableUserStore.Create("newUser@test.com", Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None, Arg.Any<ProviderUserGroups>(), Arg.Any<IEnumerable<Identity>>())
                .Returns(ResultFromExtension<IUser>.Success(user));

            var result = validator.ValidateCredentials("newUser", "testPassword", CancellationToken.None);

            result.ShouldBeOfType<ResultFromExtension<IUser>>();
            updateableUserStore.Received(1).Create("newUser@test.com", Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None, Arg.Any<ProviderUserGroups>(), Arg.Any<IEnumerable<Identity>>());
        }

        [Test]
        public void NewUserWithPartialMatchOnExistingEmail()
        {
            directoryServicesService.ValidateCredentials("newUser", "testPassword", CancellationToken.None)
                .Returns(new UserValidationResult("newUser@test.com", "newUser", "TestDomain", string.Empty, "tester@test.com"));

            var user = new User("Users-100", "existingUser", identityCreator.Create("tester@test.com", "existingUser@test.com", "TestDomain\\existingUser", ""));

            updateableUserStore.GetByIdentity(Arg.Any<Identity>()).Returns(new[] { user });

            updateableUserStore.Create("newUser@test.com", Arg.Any<string>(), "tester@test.com", CancellationToken.None, Arg.Any<ProviderUserGroups>(), Arg.Any<IEnumerable<Identity>>())
                .Returns(ResultFromExtension<IUser>.Success(user));

            directoryServicesService.FindByIdentity("TestDomain\\existingUser").Returns(new UserValidationResult("existingUser@test.com", "existingUser", "TestDomain", string.Empty, "tester@test.com"));

            var result = validator.ValidateCredentials("newUser", "testPassword", CancellationToken.None);

            result.ShouldBeOfType<ResultFromExtension<IUser>>();
            updateableUserStore.Received(1).Create("newUser@test.com", Arg.Any<string>(), "tester@test.com", CancellationToken.None, Arg.Any<ProviderUserGroups>(), Arg.Any<IEnumerable<Identity>>());
        }

        [Test]
        public void ExistingUsersWithPartialMatchOnEmailButMatchingIdentity()
        {
            directoryServicesService.ValidateCredentials("existingUser2", "testPassword", CancellationToken.None)
                .Returns(new UserValidationResult("existingUser2@test.com", "existingUser2", "TestDomain", string.Empty, "tester@test.com"));

            var user1 = new User("Users-100", "existingUser1", identityCreator.Create("tester@test.com", "existingUser1@test.com", "TestDomain\\existingUser1", ""));
            var user2 = new User("Users-101", "existingUser2", identityCreator.Create("tester@test.com", "existingUser2@test.com", "TestDomain\\existingUser2", ""));

            updateableUserStore.GetByIdentity(Arg.Any<Identity>()).Returns(new[] { user1, user2 });

            var result = validator.ValidateCredentials("existingUser2", "testPassword", CancellationToken.None);

            result.ShouldBeOfType<ResultFromExtension<IUser>>();
            updateableUserStore.DidNotReceive().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
        }

        [Test]
        public void NewUserWhenCreateInNotAllowed()
        {
            directoryServicesConfigurationStore.GetAllowAutoUserCreation().Returns(false);

            directoryServicesService.ValidateCredentials("newUser", "testPassword", CancellationToken.None)
                .Returns(new UserValidationResult("newUser@test.com", "newUser", "TestDomain", string.Empty, String.Empty));

            updateableUserStore.GetByIdentity(Arg.Any<Identity>()).Returns(new IUser[0]);

            var result = validator.ValidateCredentials("newUser", "testPassword", CancellationToken.None);

            result.ShouldBeAssignableTo<FailureResult>();
            ((FailureResult)result).ErrorString.ShouldBe("User could not be located and auto user creation is not enabled.");
        }

        [Test]
        public void NewUserFromAnotherDomain()
        {
            directoryServicesService.ValidateCredentials("Domain2\\newUser", "testPassword", CancellationToken.None)
                .Returns(new UserValidationResult("newUser@domain2.com", "newUser", "Domain2", string.Empty, String.Empty));

            updateableUserStore.GetByIdentity(Arg.Any<Identity>()).Returns(new IUser[0]);

            var user = new User("Users-100", "newUser", identityCreator.Create(string.Empty, "newUser@domain2.com", "Domain2\\newUser", string.Empty));
            updateableUserStore.Create("newUser@domain2.com", Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None, Arg.Any<ProviderUserGroups>(), Arg.Any<IEnumerable<Identity>>())
                .Returns(ResultFromExtension<IUser>.Success(user));

            var result = validator.ValidateCredentials("Domain2\\newUser", "testPassword", CancellationToken.None);

            result.ShouldBeOfType<ResultFromExtension<IUser>>();
            updateableUserStore.Received(1).Create("newUser@domain2.com", Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None, Arg.Any<ProviderUserGroups>(), Arg.Any<IEnumerable<Identity>>());
        }

        [Test]
        public void ExistingUserWhoHadTheirUpnChanged()
        {
            directoryServicesService.ValidateCredentials("existingUser", "testPassword", CancellationToken.None)
                .Returns(new UserValidationResult("existingUser@new.test.com", "existingUser", "TestDomain", string.Empty, String.Empty));

            var user = new User("Users-100", "existingUser", identityCreator.Create(string.Empty, "existingUser@test.com", "TestDomain\\existingUser", string.Empty));

            updateableUserStore.GetByIdentity(Arg.Any<Identity>()).Returns(new[] { user });

            var result = validator.ValidateCredentials("existingUser", "testPassword", CancellationToken.None);

            result.ShouldBeOfType<ResultFromExtension<IUser>>();
            updateableUserStore.Received(1).UpdateIdentity("Users-100", Arg.Any<Identity>(), CancellationToken.None);
            updateableUserStore.DidNotReceive().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
        }

        [Test]
        public void ExistingUserWhoHadTheirSamAccountNameChanged()
        {
            directoryServicesService.ValidateCredentials("existingUserWithNewSam", "testPassword", CancellationToken.None)
                .Returns(new UserValidationResult("existingUser@test.com", "existingUserWithNewSam", "TestDomain", string.Empty, String.Empty));

            var user = new User("Users-100", "existingUser", identityCreator.Create(string.Empty, "existingUser@test.com", "TestDomain\\existingUser", string.Empty));

            updateableUserStore.GetByIdentity(Arg.Any<Identity>()).Returns(new[] { user });

            var result = validator.ValidateCredentials("existingUserWithNewSam", "testPassword", CancellationToken.None);

            result.ShouldBeOfType<ResultFromExtension<IUser>>();
            updateableUserStore.Received(1).UpdateIdentity("Users-100", Arg.Any<Identity>(), CancellationToken.None);
            updateableUserStore.DidNotReceive().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
        }

        [Test]
        public void ExistingUserWhoHadTheirUpnAndSamAccountNameChanged()
        {
            directoryServicesService.ValidateCredentials("existingUserWithNewSam", "testPassword", CancellationToken.None)
                .Returns(new UserValidationResult("existingUser@new.test.com", "existingUserWithNewSam", "TestDomain", string.Empty, String.Empty));

            var user = new User("Users-100", "existingUser", identityCreator.Create(string.Empty, "existingUser@test.com", "TestDomain\\existingUser", string.Empty));

            updateableUserStore.GetByIdentity(Arg.Any<Identity>()).Returns(new[] { user });

            directoryServicesService.FindByIdentity("TestDomain\\existingUser").Returns(new UserValidationResult("User not found"));

            var result = validator.ValidateCredentials("existingUserWithNewSam", "testPassword", CancellationToken.None);

            result.ShouldBeOfType<ResultFromExtension<IUser>>();
            updateableUserStore.Received(1).UpdateIdentity("Users-100", Arg.Any<Identity>(), CancellationToken.None);
            updateableUserStore.DidNotReceive().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
        }

        private class User : IUser
        {
            public User(string id, string username, Identity identity)
            {
                Id = id;
                Username = username;
                Identities = new HashSet<Identity>(new [] {identity});
            }

            public string Id { get; }
            public string Username { get; }
            public Guid IdentificationToken { get; }
            public string DisplayName { get; set; }
            public string EmailAddress { get; set; }
            public bool IsService { get; set; }
            public bool IsActive { get; set; }
            public ReferenceCollection ExternalIdentifiers { get; }
            public HashSet<Identity> Identities { get; }

            public void SetPassword(string plainTextPassword)
            {
                throw new NotImplementedException();
            }

            public bool ValidatePassword(string plainTextPassword)
            {
                throw new NotImplementedException();
            }

            public SecurityGroups GetSecurityGroups(string identityProviderName)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetSecurityGroups()
            {
                throw new NotImplementedException();
            }
        }
    }
}
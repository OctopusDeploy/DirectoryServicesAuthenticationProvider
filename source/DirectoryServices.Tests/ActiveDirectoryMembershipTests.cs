using NSubstitute;
using NUnit.Framework;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;

namespace DirectoryServices.Tests
{
    [TestFixture]
    public class ActiveDirectoryMembershipTests
    {
        [Test]
        [TestCase("joe", "joe", null)]
        [TestCase("joe@example", "joe@example", null)]
        [TestCase("EXAMPLE\\joe", "joe", "EXAMPLE")]
        public void CredentialsAreNormalized(string rawUsername, string usedUsername, string usedDomain)
        {
            var log = Substitute.For<ILog>();
            var credentialNormalizer = new DirectoryServicesObjectNameNormalizer(log);
            var domainUser = credentialNormalizer.NormalizeName(rawUsername);
            Assert.AreEqual(usedUsername, domainUser.NormalizedName);
            Assert.AreEqual(usedDomain, domainUser.Domain);
        }
    }
}
using System;
using System.Linq;
using NUnit.Framework;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Identities;
using Octopus.Server.Extensibility.Authentication.Resources.Identities;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Tests
{
    [TestFixture]
    public class ClaimsDescriptorsTests
    {
        [Test]
        public void EnsureAllClaimDescriptorsAreReturnedByTheCreator()
        {
            var creator = new IdentityCreator();

            var creatorClaims = creator.Create("test@octopus.com", "testUpn@octopus.com", "octopus\test", "Test User");

            Assert.AreEqual("test@octopus.com", creatorClaims.Claims[ClaimDescriptor.EmailClaimType].Value);
            Assert.AreEqual("testUpn@octopus.com", creatorClaims.Claims[IdentityCreator.UpnClaimType].Value);
            Assert.AreEqual("octopus\test", creatorClaims.Claims[IdentityCreator.SamAccountNameClaimType].Value);
            Assert.AreEqual("Test User", creatorClaims.Claims[ClaimDescriptor.DisplayNameClaimType].Value);
        }

        [Test]
        public void EnsureAllClaimDescriptorsAreReturnedByTheProvider()
        {
            var provider = new DirectoryServicesAuthenticationProvider(null!);
            var creator = new IdentityCreator();

            var creatorClaims = creator.Create("test@octopus.com", "testUpn@octopus.com", "octopus\test", "Test User");
            var metadata = provider.GetMetadata();

            var missingClaims = creatorClaims.Claims.Where(c => !c.Value.IsServerSideOnly && metadata.ClaimDescriptors.All(d => d.Type != c.Key)).ToArray();
            foreach (var missingClaim in missingClaims)
            {
                Console.WriteLine($"Missing claim type {missingClaim.Key}");
            }
            Assert.IsEmpty(missingClaims);
        }
    }
}
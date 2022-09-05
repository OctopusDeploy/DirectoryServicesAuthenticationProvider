using System;
using NSubstitute;
using NUnit.Framework;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.IntegratedAuthentication;
using Octopus.Time;
using Shouldly;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Tests
{
    [TestFixture]
    public class IntegratedChallengeCoordinatorTests
    {
        [Test]
        public void InsideAnHourAbandonedConnectionsArentRemoved()
        {
            var clock = Substitute.For<IClock>();
            clock.GetUtcTime().Returns(DateTimeOffset.Now.AddMinutes(-5));

            var tracker = new IntegratedChallengeCoordinator(clock);
            tracker.SetChallengeInitiated("someId-1");
            
            clock.GetUtcTime().Returns(DateTimeOffset.Now.AddSeconds(-2));
            tracker.SetChallengeInitiated("someId-2");
            clock.GetUtcTime().Returns(DateTimeOffset.Now);
            tracker.SetChallengeCompleted("someId-2");
            
            tracker.IsNewChallengeRequest("someId-1").ShouldBeFalse("someId-1 should still be active");
        }

        [Test]
        public void AfterAnHourOldConnectionsAreRemoved()
        {
            var clock = Substitute.For<IClock>();
            clock.GetUtcTime().Returns(DateTimeOffset.Now.AddMinutes(-61));

            var tracker = new IntegratedChallengeCoordinator(clock);
            tracker.SetChallengeInitiated("someId-1");
            
            clock.GetUtcTime().Returns(DateTimeOffset.Now.AddSeconds(-2));
            tracker.SetChallengeInitiated("someId-2");
            clock.GetUtcTime().Returns(DateTimeOffset.Now);
            tracker.SetChallengeCompleted("someId-2");
            
            tracker.IsNewChallengeRequest("someId-1").ShouldBeTrue("someId-1 should have been removed");
        }
    }
}
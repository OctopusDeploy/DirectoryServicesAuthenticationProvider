using Microsoft.AspNetCore.Http;
using Octopus.Server.Extensibility.Authentication.Resources;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.IntegratedAuthentication
{
    interface IIntegratedChallengeCoordinator
    {
        IntegratedChallengeTrackerStatus SetupResponseIfChallengeHasNotSucceededYet(HttpContext context, LoginState? state);
    }
}
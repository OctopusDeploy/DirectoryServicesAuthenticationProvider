using Microsoft.AspNetCore.Http;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.HostServices;
using Octopus.Server.Extensibility.Authentication.Resources;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.IntegratedAuthentication
{
    interface IIntegratedChallengeCoordinator
    {
        IntegratedChallengeTrackerStatus SetupResponseIfChallengeHasNotSucceededYet(HttpContext context, LoginState? state, IAuthenticationConfigurationStore authenticationConfigurationStore, ISystemLog log);
    }
}
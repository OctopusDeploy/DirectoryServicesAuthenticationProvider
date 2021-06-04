using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Web.Extensions;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.IntegratedAuthentication
{
    [ApiController]
    public class IntegratedAuthenticationController : SystemScopedApiController
    {
        readonly IIntegratedAuthenticationHandler integratedAuthenticationHandler;
        readonly IDirectoryServicesConfigurationStore directoryServicesConfigurationStore;

        public IntegratedAuthenticationController(IIntegratedAuthenticationHandler integratedAuthenticationHandler, IDirectoryServicesConfigurationStore directoryServicesConfigurationStore)
        {
            this.integratedAuthenticationHandler = integratedAuthenticationHandler;
            this.directoryServicesConfigurationStore = directoryServicesConfigurationStore;
        }

        [AllowAnonymous]
        [HttpGet("integrated-challenge")]
        public async Task IntegratedChallenge()
        {
            if (!directoryServicesConfigurationStore.GetIsEnabled())
            {
                Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return;
            }

            if (string.IsNullOrWhiteSpace(HttpContext.User.Identity?.Name))
            {
                await Request.HttpContext.ChallengeAsync(NegotiateDefaults.AuthenticationScheme);
            }
            else
            {
                await integratedAuthenticationHandler.HandleRequest(Request.HttpContext);
            }
        }
    }
}
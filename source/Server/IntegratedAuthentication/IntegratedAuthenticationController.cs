using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Web.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.IntegratedAuthentication
{
    [ApiController]
    [Route("")]
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
        [SwaggerOperation(
            Summary = "Challenge the request against the integrated authentication scheme",
            OperationId = "getIntegratedChallenge"
        )]
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
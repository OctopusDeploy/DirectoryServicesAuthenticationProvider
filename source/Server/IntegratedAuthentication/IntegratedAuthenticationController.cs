﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.IntegratedAuthentication
{
    [ApiController]
    public class IntegratedAuthenticationController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet("integrated-challenge")]
        public async Task Auth()
        {
            //TODO: Shane saw something! 👀
            if (!Request.HttpContext.User.Identity?.IsAuthenticated ?? false)
            {
                await Request.HttpContext.ChallengeAsync(NegotiateDefaults.AuthenticationScheme);
            }
        }

        //TODO: Delete me please
        [AllowAnonymous]
        [HttpGet("integrated-test")]
        public string Test()
        {
            return $"Hello {Request.HttpContext.User.Identity?.Name}! Auth Type: {Request.HttpContext.User.Identity?.AuthenticationType}";
        }
    }
}
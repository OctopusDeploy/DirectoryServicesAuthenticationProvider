using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Data;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;
using Octopus.Server.Extensibility.Authentication.Extensions;
using Octopus.Server.Extensibility.Authentication.HostServices;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Web
{
    class ListSecurityGroupsAction : IAsyncApiAction
    {
        static readonly IRequiredParameter<string> PartialName = new RequiredQueryParameterProperty<string>("partialName", "Partial group name to lookup");
        static readonly OctopusJsonRegistration<ExternalSecurityGroup[]> SearchResults = new OctopusJsonRegistration<ExternalSecurityGroup[]>();

        readonly IDirectoryServicesExternalSecurityGroupLocator externalSecurityGroupLocator;

        public ListSecurityGroupsAction(
            IDirectoryServicesExternalSecurityGroupLocator externalSecurityGroupLocator)
        {
            this.externalSecurityGroupLocator = externalSecurityGroupLocator;
        }

        public Task<IOctoResponseProvider> ExecuteAsync(IOctoRequest request)
        {
            return request
                .WithParameter(PartialName)
                .HandleAsync(name =>
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                {
                    var result = externalSecurityGroupLocator.Search(name, cts.Token);
                    if (result is ISuccessResult<ExternalSecurityGroupResult> successResult)
                        context.Response.AsOctopusJson(successResult.Value.Groups);
                    else
                        context.Response.BadRequest($"The {DirectoryServicesAuthentication.ProviderName} is currently disabled");
                }
            });
        }

        ExternalSecurityGroup[] SearchByName(string name, CancellationToken cancellationToken)
        {
            return externalSecurityGroupLocator.Search(name, cancellationToken).Groups;
        }
    }
}
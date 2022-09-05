using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Data;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;
using Octopus.Server.Extensibility.Authentication.Extensions;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Web
{
    internal class UserLookupAction : IAsyncApiAction
    {
        static readonly IRequiredParameter<string> PartialName = new RequiredQueryParameterProperty<string>("partialName", "Partial username to lookup");
        static readonly BadRequestRegistration Disabled = new BadRequestRegistration($"The {DirectoryServicesAuthentication.ProviderName} is currently disabled");
        static readonly OctopusJsonRegistration<ExternalUserLookupResult> SearchResults = new OctopusJsonRegistration<ExternalUserLookupResult>();

        readonly ICanSearchActiveDirectoryUsers userSearch;

        public UserLookupAction(ICanSearchActiveDirectoryUsers userSearch)
        {
            this.userSearch = userSearch;
        }

        public Task<IOctoResponseProvider> ExecuteAsync(IOctoRequest request)
        {
            return request
                .HandleAsync(PartialName, name =>
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                {
                    var externalUserLookupResult = userSearch.Search(name, cts.Token);
                    if (externalUserLookupResult is ISuccessResult<ExternalUserLookupResult> successResult)
                        return Task.FromResult(SearchResults.Response(successResult.Value));
                    return Task.FromResult(Disabled.Response());
                }
            });
        }
    }
}
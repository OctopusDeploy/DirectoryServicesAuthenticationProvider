using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;
using Octopus.Server.Extensibility.Authentication.Extensions;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Web
{
    class UserLookupAction : IAsyncApiAction
    {
        static readonly IRequiredParameter<string> PartialName = new RequiredQueryParameterProperty<string>("partialName", "Partial username to lookup");
        static readonly OctopusJsonRegistration<ExternalUserLookupResult> searchResults = new OctopusJsonRegistration<ExternalUserLookupResult>();

        readonly ICanSearchActiveDirectoryUsers userSearch;

        public UserLookupAction(ICanSearchActiveDirectoryUsers userSearch)
        {
            this.userSearch = userSearch;
        }

        public Task<IOctoResponseProvider> ExecuteAsync(IOctoRequest request)
        {
            return request.GetParameterValue(PartialName, name =>
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                {
                    return Task.FromResult(searchResults.Response(userSearch.Search(name, cts.Token)));
                }
            });
        }
    }
}
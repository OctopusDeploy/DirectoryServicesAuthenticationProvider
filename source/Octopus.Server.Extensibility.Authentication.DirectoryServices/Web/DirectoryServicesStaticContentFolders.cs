using System.Collections.Generic;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Content;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Web
{
    class DirectoryServicesStaticContentFolders : IContributesStaticContentFolders
    {
        public IEnumerable<StaticContentEmbeddedResourcesFolder> GetStaticContentFolders()
        {
            var type = typeof(DirectoryServicesStaticContentFolders);
            var assembly = type.Assembly;
            return new[] { new StaticContentEmbeddedResourcesFolder("", assembly, type.Namespace + ".Static") };
        }
    }
}
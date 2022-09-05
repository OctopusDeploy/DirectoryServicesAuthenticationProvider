using System;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration
{
    class DirectoryServicesConfigurationMapping : IConfigurationDocumentMapper
    {
        public Type GetTypeToMap() => typeof(DirectoryServicesConfiguration);
    }
}
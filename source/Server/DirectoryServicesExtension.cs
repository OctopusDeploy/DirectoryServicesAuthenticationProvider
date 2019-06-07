using Autofac;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Identities;
using Octopus.Server.Extensibility.Authentication.Extensions;
using Octopus.Server.Extensibility.Extensions;
using Octopus.Server.Extensibility.Extensions.Infrastructure;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Server.Extensibility.Extensions.Mappings;
using Octopus.Server.Extensibility.HostServices.Web;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices
{
    [OctopusPlugin("Directory Services", "Octopus Deploy")]
    public abstract class DirectoryServicesExtension : IOctopusExtension
    {
        public virtual void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DirectoryServicesConfigurationMapping>().As<IConfigurationDocumentMapper>().InstancePerDependency();

            builder.RegisterType<DatabaseInitializer>().As<IExecuteWhenDatabaseInitializes>().InstancePerDependency();

            builder.RegisterType<IdentityCreator>().As<IIdentityCreator>().SingleInstance();

            builder.RegisterType<DirectoryServicesConfigurationStore>()
                .As<IDirectoryServicesConfigurationStore>()
                .InstancePerDependency();

            builder.RegisterType<DirectoryServicesConfigurationSettings>()
                .As<IDirectoryServicesConfigurationSettings>()
                .As<IHasConfigurationSettings>()
                .As<IHasConfigurationSettingsResource>()
                .As<IContributeMappings>()
                .InstancePerDependency();

            builder.RegisterType<DirectoryServicesUserCreationFromPrincipal>().As<ISupportsAutoUserCreationFromPrincipal>().InstancePerDependency();

            builder.RegisterType<DirectoryServicesContextProvider>().As<IDirectoryServicesContextProvider>().InstancePerDependency();
            builder.RegisterType<DirectoryServicesObjectNameNormalizer>().As<IDirectoryServicesObjectNameNormalizer>().InstancePerDependency();
            builder.RegisterType<DirectoryServicesExternalSecurityGroupLocator>()
                .As<IDirectoryServicesExternalSecurityGroupLocator>()
                .As<ICanSearchExternalGroups>()
                .InstancePerDependency();

            builder.RegisterType<DirectoryServicesService>()
                .As<IDirectoryServicesService>()
                .InstancePerDependency();

            builder.RegisterType<DirectoryServicesCredentialValidator>()
                .As<IDirectoryServicesCredentialValidator>()
                .As<IDoesBasicAuthentication>()
                .InstancePerDependency();

            builder.RegisterType<GroupRetriever>()
                .As<IExternalGroupRetriever>()
                .InstancePerDependency();

            builder.RegisterType<UserSearch>().As<ICanSearchExternalUsers>().As<ICanSearchActiveDirectoryUsers>().InstancePerDependency();
            builder.RegisterType<UserMatcher>().As<ICanMatchExternalUser>().InstancePerDependency();

            builder.RegisterType<DirectoryServicesHomeLinksContributor>().As<IHomeLinksContributor>().InstancePerDependency();
        }
    }
}
using Autofac;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Identities;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.IntegratedAuthentication;
using Octopus.Server.Extensibility.Authentication.DirectoryServices.Web;
using Octopus.Server.Extensibility.Authentication.Extensions;
using Octopus.Server.Extensibility.Authentication.Extensions.Identities;
using Octopus.Server.Extensibility.Extensions;
using Octopus.Server.Extensibility.Extensions.Infrastructure;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Content;
using Octopus.Server.Extensibility.Extensions.Mappings;
using Octopus.Server.Extensibility.HostServices.Web;
using Octopus.Server.Extensibility.Web.Api;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices
{
    [OctopusPlugin("Directory Services", "Octopus Deploy")]
    public class DirectoryServicesExtension : IOctopusExtension
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
            
             builder.RegisterType<DirectoryServicesUserCreationFromPrincipal>()
                 .AsSelf()
                 .As<ISupportsAutoUserCreationFromPrincipal>()
                 .InstancePerDependency();
            
             builder.RegisterType<DirectoryServicesContextProvider>().As<IDirectoryServicesContextProvider>().InstancePerDependency();
             builder.RegisterType<DirectoryServicesObjectNameNormalizer>().As<IDirectoryServicesObjectNameNormalizer>().InstancePerDependency();
             builder.RegisterType<UserPrincipalFinder>().As<IUserPrincipalFinder>().InstancePerDependency();
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
            
             builder.RegisterType<DirectoryServicesConfigureCommands>()
                 .As<IContributeToConfigureCommand>()
                 .InstancePerDependency();
            
             builder.RegisterType<DirectoryServicesStaticContentFolders>().As<IContributesStaticContentFolders>().InstancePerDependency();
            
             builder.RegisterType<ListSecurityGroupsAction>().AsSelf().InstancePerDependency();
             builder.RegisterType<UserLookupAction>().AsSelf().InstancePerDependency();

             builder.RegisterType<DirectoryServicesAuthenticationProvider>()
                 .As<IAuthenticationProvider>()
                 .As<IAuthenticationProviderWithGroupSupport>()
                 .As<IContributesCSS>()
                 .As<IContributesJavascript>()
                 .As<IUseAuthenticationIdentities>()
                 .AsSelf()
                 .InstancePerDependency();

            builder.RegisterType<IntegratedAuthenticationHandler>().As<IIntegratedAuthenticationHandler>().InstancePerDependency();
            builder.RegisterType<IntegratedAuthenticationHost>().As<IShareWebHostLifetime>().SingleInstance();
            builder.RegisterType<IntegratedChallengeCoordinator>().As<IIntegratedChallengeCoordinator>().SingleInstance();

            builder.RegisterType<DirectoryServicesWebServiceContributor>().As<IContributeToWebServices>();
        }
    }
}
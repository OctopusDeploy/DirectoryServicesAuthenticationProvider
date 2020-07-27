using System.ComponentModel;
using System.Net;
using Octopus.Data.Resources.Attributes;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.Configuration
{
    class DirectoryServicesConfigurationResource : ExtensionConfigurationResource
    {

        public const string ActiveDirectoryContainerDescription = "Set the active directory container used for authentication.";
        
        public const string AuthenticationSchemeDescription = "When Domain authentication is used, specifies the scheme (Basic, Digest, IntegratedWindowsAuthentication, Negotiate, Ntlm). You will need to restart all Octopus Server nodes in your cluster for these changes to take effect. Please note that using Negotiate or IntegratedWindowsAuthentication [may require additional server configuration](https://g.octopushq.com/AuthAD) in order to work correctly.";

        public const string AllowFormsAuthenticationForDomainUsersDescription = "When Domain authentication is used, specifies whether the HTML-based username/password form can be used to sign in.";

        public const string AreSecurityGroupsEnabledDescription = "When Domain authentication is used, specifies whether to support security groups from AD.";

        public const string AllowAutoUserCreationDescription = "Whether unknown users will be automatically created upon successful login.";

        [DisplayName("Active Directory Container")]
        [Description(ActiveDirectoryContainerDescription)]
        [Writeable]
        public string? ActiveDirectoryContainer { get; set; }

        [DisplayName("Authentication Scheme")]
        [Description(AuthenticationSchemeDescription)]
        [Writeable]
        [HasOptions(SelectMode.Single)]
        public AuthenticationSchemes AuthenticationScheme { get; set; }

        [DisplayName("Allow Forms Authentication For Domain Users")]
        [Description(AllowFormsAuthenticationForDomainUsersDescription)]
        [Writeable]
        public bool AllowFormsAuthenticationForDomainUsers { get; set; }

        [DisplayName("Security Groups Enabled")]
        [Description(AreSecurityGroupsEnabledDescription)]
        [Writeable]
        public bool AreSecurityGroupsEnabled { get; set; }

        [DisplayName("Allow Auto User Creation")]
        [Description(AllowAutoUserCreationDescription)]
        [Writeable]
        public bool? AllowAutoUserCreation { get; set; }
    }
}

﻿using System.ComponentModel.DataAnnotations;
using System.Net;
using Octopus.Data.Resources;
using Octopus.Node.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Node.Extensibility.Authentication.DirectoryServices.Configuration
{
    public class DirectoryServicesConfiguration : ExtensionConfigurationDocument
    {
        public DirectoryServicesConfiguration() : base("DirectoryServices", "Octopus Deploy", "1.0")
        {
            Id = DirectoryServicesConfigurationStore.SingletonId;
            AllowFormsAuthenticationForDomainUsers = true;
            AreSecurityGroupsEnabled = true;
        }

        /// <summary>
        /// Gets or sets the active directory container, if not specified default container is used
        /// </summary>
        [Display(Name = "")]
        public string ActiveDirectoryContainer { get; set; }

        /// <summary>
        /// Gets or sets the authentication scheme to use when authentication Domain users.
        /// </summary>
        public AuthenticationSchemes AuthenticationScheme { get; set; }

        /// <summary>
        /// Gets or sets the when the HTML-based username/password form will be presented for domain users. Defaults to true. 
        /// </summary>
        public bool AllowFormsAuthenticationForDomainUsers { get; set; }

        /// <summary>
        /// Gets or sets whether to allow the use of security groups from AD.
        /// </summary>
        public bool AreSecurityGroupsEnabled { get; set; }
        /// <summary>
        /// Gets or sets whether user records will be automatically created when someone passes authentication but is unknown.
        /// </summary>
        public bool? AllowAutoUserCreation { get; set; }
    }
}
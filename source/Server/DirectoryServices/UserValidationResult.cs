using System.DirectoryServices.AccountManagement;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.DirectoryServices
{
    class UserValidationResult
    {
        public UserValidationResult(UserPrincipal userPrincipal, string? domain)
            :this(userPrincipal.UserPrincipalName,
                $"{domain}\\{userPrincipal.SamAccountName}",
                domain, 
                string.IsNullOrWhiteSpace(userPrincipal.DisplayName) ? userPrincipal.Name : userPrincipal.DisplayName,
                userPrincipal.EmailAddress)
        {
        }

        public UserValidationResult(string userPrincipalName, string samAccountName, string? domain, string displayName, string emailAddress)
        {
            UserPrincipalName = userPrincipalName;
            SamAccountName = samAccountName.Contains("\\") ? samAccountName : $"{domain}\\{samAccountName}";
            Domain = domain;
            DisplayName = displayName;
            EmailAddress = emailAddress;

            Success = true;
        }

        public UserValidationResult(string validationMessage)
        {
            ValidationMessage = validationMessage;
        }

        public string? UserPrincipalName { get; }
        public string? SamAccountName { get; }
        public string? Domain { get; }

        public string? DisplayName { get; }
        public string? EmailAddress { get; }

        public bool Success { get; }
        public string? ValidationMessage { get; }
    }
}
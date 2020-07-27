using Octopus.Data.Model.User;

namespace Octopus.Server.Extensibility.Authentication.DirectoryServices.IntegratedAuthentication
{
    class OctopusAuthenticationResult
    {
        OctopusAuthenticationResult(IUser user)
        {
            User = user;
        }

        public IUser User { get; }
            
        public static OctopusAuthenticationResult Authenticated(IUser user)
        {
            return new OctopusAuthenticationResult(user);
        }
    }
}
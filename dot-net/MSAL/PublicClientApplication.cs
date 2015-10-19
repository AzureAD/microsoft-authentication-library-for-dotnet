using System.Threading.Tasks;

namespace MSAL
{
    public class PublicClientApplication
    {

        public string ClientId { get; set; }

        public string RedirectUri { get; set; }

        /// <summary>
        /// default false
        /// </summary>
        public string RestrictToSingleUser { get; set; }

        public string DefaultAuthority { get; set; }

        public TokenCache TokenCache { get; set; }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, IPlatformParameters parameters)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, UserIdentifier userId)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, UserIdentifier userId,
            string extraQueryParameters)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, UserIdentifier userId,
            string extraQueryParameters, string[] additionalScope, string authority)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenWithIntegratedAuthAsync(string[] scope)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenWithIntegratedAuthAsync(string[] scope, string authority)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, UserIdentifier userId)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, UserIdentifier userId,
            string authority)
        {
            return null;
        }

    }
}

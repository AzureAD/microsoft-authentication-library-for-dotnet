using System.Threading.Tasks;

namespace MSAL
{
    public class PublicClientApplication
    {
        //TODO it would be nice to have a uber constructor just match API footprint with other platforms.
        // Other platforms do not have syntatic sugar like
        // new Instance(value){
        //  Property = prop
        // }
        // It would be more developer friendly to have a constructor instead.
        // what about validateAuthority? Do we really need it?

        public PublicClientApplication()
        {
        }

        public PublicClientApplication(string clientId, string redirectUri, TokenCache tokenCache)
        {
            this.ClientId = clientId;
            this.RedirectUri = redirectUri;
            this.TokenCache = tokenCache;
        }

        public string ClientId { get; set; }
        
        public string RedirectUri { get; set; }

        /// <summary>
        /// default false
        /// </summary>
        public string RestrictToSingleUser { get; set; }

        //how to efficiently tell people to set only  scheme://hostname/tenant/ part as the authority?
        public string DefaultAuthority { get; set; }

        public TokenCache TokenCache { get; set; }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, IPlatformParameters parameters)
        {
            return null;
        }

        // AcquireTokenAsync(string[] scope, IPlatformParameters parameters) will collide with AcquireTokenAsync(string[] scope, UserIdentifier userId)
        // if null is passed for 2nd parameter
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

        //what about device code methods?
    }
}

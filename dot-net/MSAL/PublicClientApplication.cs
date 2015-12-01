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
        //TODO: what about validateAuthority? Do we really need it?

        public PublicClientApplication()
        {
        }

        //TODO: consider this for other platforms
        /*        public PublicClientApplication(string clientId, string redirectUri, TokenCache tokenCache)
                {
                    this.ClientId = clientId;
                    this.RedirectUri = redirectUri;
                    this.TokenCache = tokenCache;
                }*/


        //default is true
        public bool ValidateAuthority { get; set; }

        public string ClientId { get; set; }
        
        public string RedirectUri { get; set; }

        public IPlatformParameters PlatformParameters { get; set; }

        /// <summary>
        /// default false
        /// </summary>
        public string RestrictToSingleUser { get; set; }

        //TODO: how to efficiently tell people to set only  scheme://hostname/tenant/ part as the authority?
        public string DefaultAuthority { get; set; }

        public TokenCache TokenCache { get; set; }

        //TODO look into adding user identifier when domain cannot be queried or privacy settings are against you
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

        //Iplatformparameter is required for android.
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, UserIdentifier userId,
            string authority)
        {
            return null;
        }

        //what about device code methods?
        //TODO we should look at them later.


    }
}

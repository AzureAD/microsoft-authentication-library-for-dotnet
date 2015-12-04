using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MSAL
{
    /// <summary>
    /// Native applications (desktop/phone/iOS/Android).
    /// </summary>
    public class PublicClientApplication
    {

        
        /// <summary>
        /// Default consutructor of the application. It is here to emphasise the lack of parameters.
        /// </summary>
        public PublicClientApplication()
        {
        }

        //TODO: consider this for other platforms
        /// <summary>
        /// TODO it would be nice to have a uber constructor just match API footprint with other platforms.
        /// Other platforms do not have syntatic sugar like
        /// new Instance(value){
        ///  Property = prop
        /// }
        /// It would be more developer friendly to have a constructor instead.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        public PublicClientApplication(string clientId, string redirectUri)
                {
                    this.ClientId = clientId;
                    this.RedirectUri = redirectUri;
                }


        //default is true
        public bool ValidateAuthority { get; set; }


        /// <summary>
        /// Will be a default value. Can be overriden by the developer.
        /// </summary>
        public string ClientId { get; set; }
        

        /// <summary>
        /// Redirect Uri configured in the portal. Will have a default value. Not required, if the developer is using the default client ID.
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// .NET specific property that allows configuration of platform specific properties. For example, in iOS/Android it woul include the flag to enable/disable broker.
        /// </summary>
        public IPlatformParameters PlatformParameters { get; set; }

        /// <summary>
        /// default false
        /// </summary>
        public string RestrictToSingleUser { get; set; }

        /// <summary>
        /// Default will point to login.microsoftonline.com/common. Developer will be able to point to other instances like china/fairfax/blackforest.
        /// </summary>
        public string DefaultAuthority { get; set; }

        /// <summary>
        /// Default cache will be 
        /// </summary>
        public TokenCache TokenCache { get; set; }

        /// <summary>
        /// Returns a User centric view over the cache that provides a list of all the signed in users.
        /// </summary>
        public IEnumerable<User> Users { get; }

        //TODO look into adding user identifier when domain cannot be queried or privacy settings are against you
        /// <summary>
        /// .NET specific method for intergrated auth. To support Xamarin, we would need to move these to platform specific libraries.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenWithIntegratedAuthAsync(string[] scope)
        {
            return null;
        }

        /// <summary>
        /// .NET specific method for intergrated auth. To support Xamarin, we would need to move these to platform specific libraries.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="authority"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenWithIntegratedAuthAsync(string[] scope, string authority)
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope)
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, UserIdentifier userId)
        {
            return null;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userId"></param>
        /// <param name="authority"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, UserIdentifier userId,
            string authority)
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope)
        {
            return null;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, UserIdentifier userId)
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userId"></param>
        /// <param name="extraQueryParameters"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, UserIdentifier userId,
            string extraQueryParameters)
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userId"></param>
        /// <param name="extraQueryParameters"></param>
        /// <param name="additionalScope"></param>
        /// <param name="authority"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, UserIdentifier userId,
            string extraQueryParameters, string[] additionalScope, string authority)
        {
            return null;
        }

        //what about device code methods?
        //TODO we should look at them later.
        
    }
}

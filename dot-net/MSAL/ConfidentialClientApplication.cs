using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
   public class ConfidentialClientApplication
   {
       private const string DEFAULT_AUTHORTIY = "default-authority";
        /// <summary>
        /// default false. TODO - Why would anyone build a single user, single tenant app. Consider removal.
        /// </summary>
        public string RestrictToSingleUser { get; set; }

        public string Authority { get; private set; }

        /// <summary>
        /// Will be a default value. Can be overriden by the developer.
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// Redirect Uri configured in the portal. Will have a default value. Not required, if the developer is using the default client ID.
        /// </summary>
        public string RedirectUri { get; private set; }

        public TokenCache TokenCache { get; set; }
        
       public ConfidentialClientApplication(string clientId, string redirectUri,
           ClientCredential clientCredential):this(DEFAULT_AUTHORTIY, clientId, redirectUri, clientCredential)
       {
           
       }

       public ConfidentialClientApplication(string authority, string clientId, string redirectUri, ClientCredential clientCredential)
       {
           this.Authority = authority;
           this.ClientId = clientId;
           this.RedirectUri = redirectUri;
       }

       public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope)
       {
           return null;
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, string authority)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, User userId)
        {
            return null;
        }


        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(string[] scope, UserAssertion userAssertion)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(string[] scope, UserAssertion userAssertion, string authority)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string[] scope, string authorizationCode)
        {
            return null;
        }

        public Uri GetAuthorizationRequestURL(string[] scope, string userId, string extraQueryParameters)
        {
            return null;
        }

        public Uri GetAuthorizationRequestURL(string[] scope, string redirectUri, string userId, string extraQueryParameters, string[] additionalScope, string authority)
        {
            return null;
        }



    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
   public class ConfidentialClientApplication
    {

        /// <summary>
        /// default false
        /// </summary>
        public string RestrictToSingleUser { get; set; }

        public string DefaultAuthority { get; set; }

        public TokenCache TokenCache { get; set; }

        public ConfidentialClientApplication(string clientId, string redirectUri, ClientCredential clientCredential)
       {
           
       }

       public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope)
       {
           return null;
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, string authority)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, UserIdentifier userId)
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

        public Uri GetAuthorizationRequestURL(string[] scope, UserIdentifier userId, string extraQueryParameters)
        {
            return null;
        }

        public Uri GetAuthorizationRequestURL(string[] scope, string redirectUri, UserIdentifier userId, string extraQueryParameters, string[] additionalScope, string authority)
        {
            return null;
        }



    }
}

using System;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
   public sealed class ConfidentialClientApplication : AbstractClientApplication
   {
       /// <summary>
       /// 
       /// </summary>
       public ClientCredential ClientCredential { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="clientCredential"></param>
       public ConfidentialClientApplication(string clientId, string redirectUri,
           ClientCredential clientCredential):this(DEFAULT_AUTHORTIY, clientId, redirectUri, clientCredential)
       {
       }
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="clientCredential"></param>
       public ConfidentialClientApplication(string authority, string clientId, string redirectUri, ClientCredential clientCredential):base(authority, clientId, redirectUri)
        {
            this.ClientCredential = clientCredential;
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

        public async Task<Uri> GetAuthorizationRequestURL(string[] scope, string userId, string extraQueryParameters)
        {
            return null;
        }

        public async Task<Uri> GetAuthorizationRequestURL(string[] scope, string redirectUri, string userId, string extraQueryParameters, string[] additionalScope, string authority)
        {
            var handler = new AcquireTokenInteractiveHandler(this.Authenticator, this.TokenCache, scope, this.ClientId, new Uri(this.RedirectUri), null, userId, extraQueryParameters, null);
            return await handler.CreateAuthorizationUriAsync(this.CorrelationId);
        }

        /// <summary>
        /// Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="scope">Identifier of the target scope that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        public async Task<Uri> GetAuthorizationRequestUrlAsync(string[] scope, string clientId, Uri redirectUri, UserIdentifier userId, string extraQueryParameters)
        {
            var handler = new AcquireTokenInteractiveHandler(this.Authenticator, this.TokenCache, scope, clientId, redirectUri, null, userId, extraQueryParameters, null);
            return await handler.CreateAuthorizationUriAsync(this.CorrelationId);
        }
    }
}

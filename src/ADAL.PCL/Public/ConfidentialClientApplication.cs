using System;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
   public sealed class ConfidentialClientApplication : AbstractClientApplication
   {
       /// <summary>
       /// 
       /// </summary>
       public ClientCredential ClientCredential { get; private set; }

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
       public ConfidentialClientApplication(string authority, string clientId, string redirectUri, ClientCredential clientCredential):base(authority, clientId, redirectUri, true)
        {
            this.ClientCredential = clientCredential;
        }

       public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope)
       {
            this.
       }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, User userId)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, string authority)
        {
            return null;
        }

        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(string[] scope, UserAssertion userAssertion)
        {
            return
                await
                    this.AcquireTokenOnBehalfCommonAsync(this.Authenticator, scope,
                        new ClientKey(this.ClientId, this.ClientCredential, this.Authenticator), userAssertion, null)
                        .ConfigureAwait(false);
        }
    

        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(string[] scope, UserAssertion userAssertion, string authority, string policy)
        {
            Authenticator localAuthenticator = new Authenticator(authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenOnBehalfCommonAsync(localAuthenticator, scope,
                        new ClientKey(this.ClientId, this.ClientCredential, localAuthenticator), userAssertion, policy)
                        .ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string[] scope, string authorizationCode, string policy)
        {
            return
                await
                    this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, scope, new Uri(this.RedirectUri),
                        new ClientKey(this.ClientId, this.ClientCredential, this.Authenticator), policy).ConfigureAwait(false);
        }


        private async Task<AuthenticationResult> AcquireTokenForClientCommonAsync(string[] scope, ClientKey clientKey)
        {
            var handler = new AcquireTokenForClientHandler(this.Authenticator, this.TokenCache, scope, clientKey);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenOnBehalfCommonAsync(Authenticator authenticator, string[] scope, ClientKey clientKey, UserAssertion userAssertion, string policy)
        {
            var handler = new AcquireTokenOnBehalfHandler(authenticator, this.TokenCache, scope, clientKey, userAssertion, policy);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeCommonAsync(string authorizationCode, string[] scope, Uri redirectUri, ClientKey clientKey, string policy)
        {
            var handler = new AcquireTokenByAuthorizationCodeHandler(this.Authenticator, this.TokenCache, scope, clientKey, authorizationCode, redirectUri, policy);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(Authenticator authenticator, string[] scope, string clientId, UserAssertion userAssertion, string policy)
        {
            var handler = new AcquireTokenNonInteractiveHandler(authenticator, this.TokenCache, scope, clientId, userAssertion, policy);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        public async Task<Uri> GetAuthorizationRequestURL(string[] scope, string userId, string extraQueryParameters)
        {
            return null;
        }

        public async Task<Uri> GetAuthorizationRequestURL(string[] scope, string redirectUri, string userId, string extraQueryParameters, string[] additionalScope, string authority, string policy)
        {
            return null;
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
        public async Task<Uri> GetAuthorizationRequestUrlAsync(string[] scope, string clientId, Uri redirectUri, User userId, string extraQueryParameters)
        {
            return null;
        }
    }
}

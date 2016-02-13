using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Native applications (desktop/phone/iOS/Android).
    /// </summary>
    public sealed class PublicClientApplication : AbstractClientApplication
    {
        private const string DEFAULT_CLIENT_ID = "default-client-id";
        private const string DEFAULT_REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";

        /// <summary>
        /// Default consutructor of the application. It is here to emphasise the lack of parameters.
        /// </summary>
        public PublicClientApplication():this(DEFAULT_AUTHORTIY)
        {
        }

        public PublicClientApplication(string authority):base(authority, DEFAULT_CLIENT_ID, DEFAULT_REDIRECT_URI, true)
        {
        }

        public PublicClientApplication(string authority, string clientId) : base(authority, clientId, DEFAULT_REDIRECT_URI, true)
        {
            this.TokenCache = TokenCache.DefaultShared;
        }

        /// <summary>
        /// .NET specific property that allows configuration of platform specific properties. For example, in iOS/Android it would include the flag to enable/disable broker.
        /// </summary>
        public IPlatformParameters PlatformParameters { get; set; }

        //TODO look into adding user identifier when domain cannot be queried or privacy settings are against you
        /// <summary>
        /// .NET specific method for intergrated auth. To support Xamarin, we would need to move these to platform specific libraries.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        internal async Task<AuthenticationResult> AcquireTokenWithIntegratedAuthInternalAsync(string[] scope)
        {
            return
                await
                    this.AcquireTokenUsingIntegratedAuthCommonAsync(this.Authenticator, scope, this.ClientId,
                        new UserCredential(), null).ConfigureAwait(false);
        }

        /// <summary>
        /// .NET specific method for intergrated auth. To support Xamarin, we would need to move these to platform specific libraries.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="authority"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        internal async Task<AuthenticationResult> AcquireTokenWithIntegratedAuthInternalAsync(string[] scope, string authority, string policy)
        {
            Authenticator localAuthenticator = new Authenticator(this.Authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenUsingIntegratedAuthCommonAsync(localAuthenticator, scope, this.ClientId,
                        new UserCredential(), policy).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope)
        {
            return await this.AcquireTokenSilentCommonAsync(this.Authenticator, scope, new ClientKey(this.ClientId), (User)null, this.PlatformParameters, null).ConfigureAwait(false);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, User userId)
        {
            return await this.AcquireTokenSilentCommonAsync(this.Authenticator, scope, new ClientKey(this.ClientId), userId, this.PlatformParameters, null).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, string userIdentifier)
        {
            return await this.AcquireTokenSilentCommonAsync(this.Authenticator, scope, new ClientKey(this.ClientId), userIdentifier, this.PlatformParameters, null).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userId"></param>
        /// <param name="authority"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, string userIdentifier,
            string authority, string policy)
        {
            Authenticator localAuthenticator = new Authenticator(this.Authority, this.ValidateAuthority);
            return await this.AcquireTokenSilentCommonAsync(localAuthenticator, scope, new ClientKey(this.ClientId), userIdentifier, this.PlatformParameters, null).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userId"></param>
        /// <param name="authority"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, User userId,
            string authority, string policy)
        {
            Authenticator localAuthenticator = new Authenticator(this.Authority, this.ValidateAuthority);
            return await this.AcquireTokenSilentCommonAsync(localAuthenticator, scope, new ClientKey(this.ClientId), userId, this.PlatformParameters, null).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope)
        {
            return
                await
                    this.AcquireTokenCommonAsync(this.Authenticator, scope, null, this.ClientId,
                        new Uri(this.RedirectUri), null, UiOptions.SelectAccount, null, null).ConfigureAwait(false);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, string identifier)
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
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, string identifier,
            UiOptions options, string extraQueryParameters)
        {
            return
                await
                    this.AcquireTokenCommonAsync(this.Authenticator, scope, null, this.ClientId,
                        new Uri(this.RedirectUri), identifier, options, extraQueryParameters, null).ConfigureAwait(false);
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
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, string identifier,
            UiOptions options, string extraQueryParameters, string[] additionalScope, string authority)
        {
            Authenticator localAuthenticator = new Authenticator(this.Authority, this.ValidateAuthority);
            return null;
        }
        
        internal IWebUI CreateWebAuthenticationDialog(IPlatformParameters parameters)
        {
            return PlatformPlugin.WebUIFactory.CreateAuthenticationDialog(parameters);
        }

        private async Task<AuthenticationResult> AcquireTokenUsingIntegratedAuthCommonAsync(Authenticator authenticator, string[] scope, string clientId, UserCredential userCredential, string policy)
        {
            var handler = new AcquireTokenNonInteractiveHandler(authenticator, this.TokenCache, scope, clientId, userCredential, policy);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(Authenticator authenticator, string[] scope, string clientId, UserAssertion userAssertion, string policy)
        {
            var handler = new AcquireTokenNonInteractiveHandler(authenticator, this.TokenCache, scope, clientId, userAssertion, policy);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(Authenticator authenticator, string[] scope, string[] additionalScope, string clientId, Uri redirectUri, string loginHint, UiOptions uiOptions, string extraQueryParameters, string policy)
        {
            var handler = new AcquireTokenInteractiveHandler(this.Authenticator, this.TokenCache, scope, additionalScope, clientId, redirectUri, this.PlatformParameters, loginHint, uiOptions, extraQueryParameters, policy, this.CreateWebAuthenticationDialog(this.PlatformParameters));
            return await handler.RunAsync().ConfigureAwait(false);
        }


    }
}

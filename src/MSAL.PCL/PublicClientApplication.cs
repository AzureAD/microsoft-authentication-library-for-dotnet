using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Handlers;
using Microsoft.Identity.Client.Interfaces;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Native applications (desktop/phone/iOS/Android).
    /// </summary>
    public sealed class PublicClientApplication : AbstractClientApplication
    {
        //TODO add default client id
        private const string DEFAULT_CLIENT_ID = "default-client-id";
        private const string DEFAULT_REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";

        /// <summary>
        /// Default consutructor of the application. It is here to emphasise the lack of parameters.
        /// </summary>
        public PublicClientApplication():this(DEFAULT_AUTHORTIY)
        {
        }

        public PublicClientApplication(string authority):this(authority, DEFAULT_CLIENT_ID)
        {
        }

        public PublicClientApplication(string authority, string clientId) : base(authority, clientId, DEFAULT_REDIRECT_URI, true)
        {
            this.UserTokenCache = TokenCache.DefaultShared;
        }

        /// <summary>
        /// .NET specific property that allows configuration of platform specific properties. For example, in iOS/Android it would include the flag to enable/disable broker.
        /// </summary>
        public IPlatformParameters PlatformParameters { get; set; }

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
            Authenticator localAuthenticator = new Authenticator(authority, this.ValidateAuthority);
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
            Authenticator localAuthenticator = new Authenticator(authority, this.ValidateAuthority);
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
                        new Uri(this.RedirectUri), (string)null, UiOptions.SelectAccount, null, null).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, string identifier)
        {
            return
                await
                    this.AcquireTokenCommonAsync(this.Authenticator, scope, null, this.ClientId,
                        new Uri(this.RedirectUri), identifier, UiOptions.SelectAccount, null, null).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="identifier"></param>
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
        /// <param name="identifier"></param>
        /// <param name="extraQueryParameters"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, User user,
            UiOptions options, string extraQueryParameters)
        {
            return
                await
                    this.AcquireTokenCommonAsync(this.Authenticator, scope, null, this.ClientId,
                        new Uri(this.RedirectUri), user, options, extraQueryParameters, null).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="identifier"></param>
        /// <param name="extraQueryParameters"></param>
        /// <param name="options"></param>
        /// <param name="additionalScope"></param>
        /// <param name="authority"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, string identifier,
            UiOptions options, string extraQueryParameters, string[] additionalScope, string authority, string policy)
        {
            Authenticator localAuthenticator = new Authenticator(authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenCommonAsync(localAuthenticator, scope, additionalScope, this.ClientId,
                        new Uri(this.RedirectUri), identifier, options, extraQueryParameters, policy).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="identifier"></param>
        /// <param name="extraQueryParameters"></param>
        /// <param name="options"></param>
        /// <param name="additionalScope"></param>
        /// <param name="authority"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope, User user,
            UiOptions options, string extraQueryParameters, string[] additionalScope, string authority, string policy)
        {
            Authenticator localAuthenticator = new Authenticator(authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenCommonAsync(localAuthenticator, scope, additionalScope, this.ClientId,
                        new Uri(this.RedirectUri), user, options, extraQueryParameters, policy).ConfigureAwait(false);
        }
        
        internal IWebUI CreateWebAuthenticationDialog(IPlatformParameters parameters)
        {
            return PlatformPlugin.WebUIFactory.CreateAuthenticationDialog(parameters);
        }

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
            Authenticator localAuthenticator = new Authenticator(authority, this.ValidateAuthority);
            return
                await
                    this.AcquireTokenUsingIntegratedAuthCommonAsync(localAuthenticator, scope, this.ClientId,
                        new UserCredential(), policy).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenUsingIntegratedAuthCommonAsync(Authenticator authenticator, string[] scope, string clientId, UserCredential userCredential, string policy)
        {
            var handler = new AcquireTokenNonInteractiveHandler(authenticator, this.UserTokenCache, scope, clientId, userCredential, policy, this.RestrictToSingleUser);
            return await handler.RunAsync().ConfigureAwait(false);
        }


        /// <summary>
        /// interactive call using login_hint
        /// </summary>
        /// <param name="authenticator"></param>
        /// <param name="scope"></param>
        /// <param name="additionalScope"></param>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="loginHint"></param>
        /// <param name="uiOptions"></param>
        /// <param name="extraQueryParameters"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        private async Task<AuthenticationResult> AcquireTokenCommonAsync(Authenticator authenticator, string[] scope, string[] additionalScope, string clientId, Uri redirectUri, string loginHint, UiOptions uiOptions, string extraQueryParameters, string policy)
        {
            if (this.PlatformParameters == null)
            {
                this.PlatformParameters = PlatformPlugin.DefaultPlatformParameters;
            }

            var handler = new AcquireTokenInteractiveHandler(authenticator, this.UserTokenCache, scope, additionalScope, clientId, redirectUri, this.PlatformParameters, loginHint, uiOptions, extraQueryParameters, policy, this.CreateWebAuthenticationDialog(this.PlatformParameters));
            return await handler.RunAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// interactive call using User object.
        /// </summary>
        /// <param name="authenticator"></param>
        /// <param name="scope"></param>
        /// <param name="additionalScope"></param>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="loginHint"></param>
        /// <param name="uiOptions"></param>
        /// <param name="extraQueryParameters"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        private async Task<AuthenticationResult> AcquireTokenCommonAsync(Authenticator authenticator, string[] scope, string[] additionalScope, string clientId, Uri redirectUri, User user, UiOptions uiOptions, string extraQueryParameters, string policy)
        {
            if (this.PlatformParameters == null)
            {
                this.PlatformParameters = PlatformPlugin.DefaultPlatformParameters;
            }

            var handler = new AcquireTokenInteractiveHandler(authenticator, this.UserTokenCache, scope, additionalScope, clientId, redirectUri, this.PlatformParameters, user, uiOptions, extraQueryParameters, policy, this.CreateWebAuthenticationDialog(this.PlatformParameters));
            return await handler.RunAsync().ConfigureAwait(false);
        }
    }
}

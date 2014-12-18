//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal enum AuthorityValidationType
    {
        True,
        False,
        NotProvided
    }

    /// <summary>
    /// The AuthenticationContext class retrieves authentication tokens from Azure Active Directory and ADFS services.
    /// </summary>
    public sealed partial class AuthenticationContext
    {
        internal Authenticator Authenticator;

        static AuthenticationContext()
        {
            Logger.Information(null, string.Format("ADAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}' is running...",
                PlatformSpecificHelper.GetProductName(), AdalIdHelper.GetAdalVersion(), AdalIdHelper.GetAssemblyFileVersion(), AdalIdHelper.GetAssemblyInformationalVersion()));
        }

        /// <summary>
        /// Constructor to create the context with the address of the authority.
        /// Using this constructor will turn ON validation of the authority URL by default if validation is supported for the authority address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
#if ADAL_WINPHONE
        private AuthenticationContext(string authority)
#else
        public AuthenticationContext(string authority)
#endif
            : this(authority, AuthorityValidationType.NotProvided, TokenCache.DefaultShared)
        {
        }

        /// <summary>
        /// Constructor to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
#if ADAL_WINPHONE
        private AuthenticationContext(string authority, bool validateAuthority)
#else
        public AuthenticationContext(string authority, bool validateAuthority)
#endif
            : this(authority, validateAuthority ? AuthorityValidationType.True : AuthorityValidationType.False, TokenCache.DefaultShared)
        {
        }

#if ADAL_NET
        /// <summary>
        /// Constructor to create the context with the address of the authority.
        /// Using this constructor will turn ON validation of the authority URL by default if validation is supported for the authority address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="tokenCache">Token cache used to lookup cached tokens on calls to AcquireToken</param>
        public AuthenticationContext(string authority, TokenCache tokenCache)
            : this(authority, AuthorityValidationType.NotProvided, tokenCache)
        {
        }
#endif

        /// <summary>
        /// Constructor to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
        /// <param name="tokenCache">Token cache used to lookup cached tokens on calls to AcquireToken</param>
#if ADAL_WINPHONE
        private AuthenticationContext(string authority, bool validateAuthority, TokenCache tokenCache)
#else
        public AuthenticationContext(string authority, bool validateAuthority, TokenCache tokenCache)
#endif
            : this(authority, validateAuthority ? AuthorityValidationType.True : AuthorityValidationType.False, tokenCache)
        {
        }

        private AuthenticationContext(string authority, AuthorityValidationType validateAuthority, TokenCache tokenCache)
        {
            // If authorityType is not provided (via first constructor), we validate by default (except for ASG and Office tenants).
            this.Authenticator = new Authenticator(authority, (validateAuthority != AuthorityValidationType.False));

            this.TokenCache = tokenCache;
        }

        /// <summary>
        /// Gets address of the authority to issue token.
        /// </summary>
        public string Authority
        {
            get
            {
                return this.Authenticator.Authority;
            }
        }

        /// <summary>
        /// Gets a value indicating whether address validation is ON or OFF.
        /// </summary>
        public bool ValidateAuthority
        {
            get
            {
                return this.Authenticator.ValidateAuthority;
            }
        }

#if ADAL_NET
        /// <summary>
        /// Gets the TokenCache
        /// </summary>
        /// <remarks>
        /// By default, TokenCache is an in-memory collection of key/value pairs. 
        /// Library will automatically save tokens in the cache when AcquireToken is called.  
        /// The default token cache is static so all tokens will available to all instances of AuthenticationContext. To use a custom TokenCache pass one to the <see cref="AuthenticationContext">.constructor</see>.
        /// To turn OFF token caching, use the constructor and set TokenCache to null.
        /// </remarks>
#else
        /// <summary>
        /// Property to provide ADAL's token cache. By default, TokenCache is a persistent cache based on application's local settings. 
        /// Library will automatically save tokens in default TokenCache whenever you obtain them. Cached tokens will be available only to the application that saved them. 
        /// Cached tokens in default token cache will outlive the application's execution, and will be available in subsequent runs.
        /// To turn OFF token caching, set TokenCache to null. 
        /// </summary>
#endif
        public TokenCache TokenCache { get; private set; }

        /// <summary>
        /// Gets or sets correlation Id which would be sent to the service with the next request. 
        /// Correlation Id is to be used for diagnostics purposes. 
        /// </summary>
        public Guid CorrelationId
        {
            get
            {
                return this.Authenticator.CorrelationId;
            }

            set
            {
                this.Authenticator.CorrelationId = value;                
            }
        }

#if !ADAL_WINPHONE
        private async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, string clientId, UserCredential userCredential, bool callSync = false)
        {
            var handler = new AcquireTokenNonInteractiveHandler(this.Authenticator, this.TokenCache, resource, clientId, userCredential, callSync);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, string clientId, UserAssertion userAssertion, bool callSync = false)
        {
            var handler = new AcquireTokenNonInteractiveHandler(this.Authenticator, this.TokenCache, resource, clientId, userAssertion, callSync);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, string clientId, Uri redirectUri, PromptBehavior promptBehavior, UserIdentifier userId, string extraQueryParameters = null, bool callSync = false)
        {
            var handler = new AcquireTokenInteractiveHandler(this.Authenticator, this.TokenCache, resource, clientId, redirectUri, promptBehavior, userId, extraQueryParameters, this.CreateWebAuthenticationDialog(promptBehavior), callSync);
            return await handler.RunAsync();
        }
#endif

        private async Task<AuthenticationResult> AcquireTokenByRefreshTokenCommonAsync(string refreshToken, ClientKey clientKey, string resource, bool callSync = false)
        {
            var handler = new AcquireTokenByRefreshTokenHandler(this.Authenticator, this.TokenCache, resource, clientKey, refreshToken, callSync);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(string resource, ClientKey clientKey, UserIdentifier userId, bool callSync = false)
        {
            var handler = new AcquireTokenSilentHandler(this.Authenticator, this.TokenCache, resource, clientKey, userId, callSync);
            return await handler.RunAsync();
        }
    }
}

//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Globalization;
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
    public sealed class AuthenticationContext
    {
        internal Authenticator Authenticator;

        static AuthenticationContext()
        {
            PlatformPlugin.Logger.Information(null, string.Format(CultureInfo.CurrentCulture, "ADAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}' is running...",
                PlatformPlugin.PlatformInformation.GetProductName(), AdalIdHelper.GetAdalVersion(), AdalIdHelper.GetAssemblyFileVersion(), AdalIdHelper.GetAssemblyInformationalVersion()));
        }

        /// <summary>
        /// Constructor to create the context with the address of the authority.
        /// Using this constructor will turn ON validation of the authority URL by default if validation is supported for the authority address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        public AuthenticationContext(string authority)
            : this(authority, AuthorityValidationType.NotProvided, TokenCache.DefaultShared)
        {
        }

        /// <summary>
        /// Constructor to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
        public AuthenticationContext(string authority, bool validateAuthority)
            : this(authority, validateAuthority ? AuthorityValidationType.True : AuthorityValidationType.False, TokenCache.DefaultShared)
        {
        }

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

        /// <summary>
        /// Constructor to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
        /// <param name="tokenCache">Token cache used to lookup cached tokens on calls to AcquireToken</param>
        public AuthenticationContext(string authority, bool validateAuthority, TokenCache tokenCache)
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

        /// <summary>
        /// Property to provide ADAL's token cache. Depending on the platform, TokenCache may have a default persistent cache or not. 
        /// Library will automatically save tokens in default TokenCache whenever you obtain them. Cached tokens will be available only to the application that saved them. 
        /// If the cache is persistent, the tokens stored in it will outlive the application's execution, and will be available in subsequent runs.
        /// To turn OFF token caching, set TokenCache to null. 
        /// </summary>
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
        
        /// <summary>
        /// Acquires device code from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<DeviceCodeResult> AcquireDeviceCodeAsync(string resource, string clientId)
        {
            return await this.AcquireDeviceCodeAsync(resource, clientId, null);
        }

        /// <summary>
        /// Acquires device code from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<DeviceCodeResult> AcquireDeviceCodeAsync(string resource, string clientId, string extraQueryParameters)
        {
            var handler = new AcquireDeviceCodeHandler(this.Authenticator, resource, clientId, extraQueryParameters);
            return await handler.RunHandlerAsync();
        }

        /// <summary>
        /// Acquires security token from the authority using an device code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="deviceCodeResult">The device code result received from calling AcquireDeviceCodeAsync.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByDeviceCodeAsync(DeviceCodeResult deviceCodeResult)
        {
            var handler = new AcquireTokenByDeviceCodeHandler(this.Authenticator, this.TokenCache, deviceCodeResult);
            return await handler.RunAsync();
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userAssertion">The assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId, UserAssertion userAssertion)
        {
            return await this.AcquireTokenCommonAsync(resource, clientId, userAssertion);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>        
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientCredential clientCredential)
        {
            return await this.AcquireTokenForClientCommonAsync(resource, new ClientKey(clientCredential));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, IClientAssertionCertificate clientCertificate)
        {
            return await this.AcquireTokenForClientCommonAsync(resource, new ClientKey(clientCertificate, this.Authenticator));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientAssertion clientAssertion)
        {
            return await this.AcquireTokenForClientCommonAsync(resource, new ClientKey(clientAssertion));
        }

        /// <summary>
        /// Acquires security token from the authority using authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="clientCredential">The credential to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientCredential clientCredential)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCredential), null);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="clientCredential">The credential to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientCredential clientCredential, string resource)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCredential), resource);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertion clientAssertion)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientAssertion), null);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertion clientAssertion, string resource)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientAssertion), resource);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, IClientAssertionCertificate clientCertificate)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCertificate, this.Authenticator), null);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, IClientAssertionCertificate clientCertificate, string resource)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCertificate, this.Authenticator), resource);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientCredential clientCredential, UserAssertion userAssertion)
        {
            return await this.AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientCredential), userAssertion);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, IClientAssertionCertificate clientCertificate, UserAssertion userAssertion)
        {
            return await this.AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientCertificate, this.Authenticator), userAssertion);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientAssertion clientAssertion, UserAssertion userAssertion)
        {
            return await this.AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientAssertion), userAssertion);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId)
        {
            return await this.AcquireTokenSilentAsync(resource, clientId, UserIdentifier.AnyUser);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId, UserIdentifier userId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientId), userId, null);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="parameters">Instance of PlatformParameters containing platform specific arguments and information.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId, UserIdentifier userId, IPlatformParameters parameters)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientId), userId, parameters);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, ClientCredential clientCredential, UserIdentifier userId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientCredential), userId, null);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, IClientAssertionCertificate clientCertificate, UserIdentifier userId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientCertificate, this.Authenticator), userId, null);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, ClientAssertion clientAssertion, UserIdentifier userId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientAssertion), userId, null);
        }

        /// <summary>
        /// Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        public async Task<Uri> GetAuthorizationRequestUrlAsync(string resource, string clientId, Uri redirectUri, UserIdentifier userId, string extraQueryParameters)
        {
            var handler = new AcquireTokenInteractiveHandler(this.Authenticator, this.TokenCache, resource, clientId, redirectUri, null, userId, extraQueryParameters, null);
            return await handler.CreateAuthorizationUriAsync(this.CorrelationId);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="parameters">An object of type PlatformParameters which may pass additional parameters used for authorization.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId, Uri redirectUri, IPlatformParameters parameters)
        {
            return await this.AcquireTokenCommonAsync(resource, clientId, redirectUri, parameters, UserIdentifier.AnyUser);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="parameters">An object of type PlatformParameters which may pass additional parameters used for authorization.</param>
        /// <param name="userId">Identifier of the user token is requested for. If created from DisplayableId, this parameter will be used to pre-populate the username field in the authentication form. Please note that the end user can still edit the username field and authenticate as a different user. 
        /// If you want to be notified of such change with an exception, create UserIdentifier with type RequiredDisplayableId. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId, Uri redirectUri, IPlatformParameters parameters, UserIdentifier userId)
        {
            return await this.AcquireTokenCommonAsync(resource, clientId, redirectUri, parameters, userId);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="userId">Identifier of the user token is requested for. If created from DisplayableId, this parameter will be used to pre-populate the username field in the authentication form. Please note that the end user can still edit the username field and authenticate as a different user. 
        /// If you want to be notified of such change with an exception, create UserIdentifier with type RequiredDisplayableId. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="parameters">Parameters needed for interactive flow requesting authorization code. Pass an instance of PlatformParameters.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId, Uri redirectUri, IPlatformParameters parameters, UserIdentifier userId, string extraQueryParameters)
        {
            return await this.AcquireTokenCommonAsync(resource, clientId, redirectUri, parameters, userId, extraQueryParameters);
        }

        private async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeCommonAsync(string authorizationCode, Uri redirectUri, ClientKey clientKey, string resource)
        {
            var handler = new AcquireTokenByAuthorizationCodeHandler(this.Authenticator, this.TokenCache, resource, clientKey, authorizationCode, redirectUri);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenForClientCommonAsync(string resource, ClientKey clientKey)
        {
            var handler = new AcquireTokenForClientHandler(this.Authenticator, this.TokenCache, resource, clientKey);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenOnBehalfCommonAsync(string resource, ClientKey clientKey, UserAssertion userAssertion)
        {
            var handler = new AcquireTokenOnBehalfHandler(this.Authenticator, this.TokenCache, resource, clientKey, userAssertion);
            return await handler.RunAsync();
        }

        internal IWebUI CreateWebAuthenticationDialog(IPlatformParameters parameters)
        {
            return PlatformPlugin.WebUIFactory.CreateAuthenticationDialog(parameters);
        }

        internal async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, string clientId, UserCredential userCredential)
        {
            var handler = new AcquireTokenNonInteractiveHandler(this.Authenticator, this.TokenCache, resource, clientId, userCredential);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, string clientId, UserAssertion userAssertion)
        {
            var handler = new AcquireTokenNonInteractiveHandler(this.Authenticator, this.TokenCache, resource, clientId, userAssertion);
            return await handler.RunAsync();
        }
        
        private async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, string clientId, Uri redirectUri, IPlatformParameters parameters, UserIdentifier userId, string extraQueryParameters = null)
        {
            var handler = new AcquireTokenInteractiveHandler(this.Authenticator, this.TokenCache, resource, clientId, redirectUri, parameters, userId, extraQueryParameters, this.CreateWebAuthenticationDialog(parameters));
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(string resource, ClientKey clientKey, UserIdentifier userId, IPlatformParameters parameters)
        {
            var handler = new AcquireTokenSilentHandler(this.Authenticator, this.TokenCache, resource, clientKey, userId, parameters);
            return await handler.RunAsync();
        }
    }
}

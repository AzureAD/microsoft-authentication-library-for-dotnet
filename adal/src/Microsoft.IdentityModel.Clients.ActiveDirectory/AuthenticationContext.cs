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
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.UI;
using Microsoft.Identity.Core.WsTrust;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.ClientCreds;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Flows;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Instance;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;

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
        private readonly IHttpManager _httpManager;
        private readonly IWsTrustWebRequestManager _wsTrustWebRequestManager;

        static AuthenticationContext()
        {
            ModuleInitializer.EnsureModuleInitialized();
        }

        internal Authenticator Authenticator;

        /// <summary>
        /// Constructor to create the context with the address of the authority.
        /// Using this constructor will turn ON validation of the authority URL by default if validation is supported for the authority address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        public AuthenticationContext(string authority)
            : this(null, authority, AuthorityValidationType.NotProvided, TokenCache.DefaultShared)
        {
        }

        /// <summary>
        /// Constructor to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
        public AuthenticationContext(string authority, bool validateAuthority)
            : this(null, authority, validateAuthority ? AuthorityValidationType.True : AuthorityValidationType.False,
                TokenCache.DefaultShared)
        {
        }

        /// <summary>
        /// Constructor to create the context with the address of the authority.
        /// Using this constructor will turn ON validation of the authority URL by default if validation is supported for the authority address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="tokenCache">Token cache used to lookup cached tokens on calls to AcquireToken</param>
        public AuthenticationContext(string authority, TokenCache tokenCache)
            : this(null, authority, AuthorityValidationType.NotProvided, tokenCache)
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
            : this(null, authority, validateAuthority ? AuthorityValidationType.True : AuthorityValidationType.False,
                tokenCache)
        {
        }

        internal AuthenticationContext(IHttpManager httpManager, string authority, AuthorityValidationType validateAuthority,
            TokenCache tokenCache)
        {
            // If authorityType is not provided (via first constructor), we validate by default (except for ASG and Office tenants).
            this.Authenticator = new Authenticator(authority, (validateAuthority != AuthorityValidationType.False));
            this.TokenCache = tokenCache;

            _httpManager = httpManager ?? new HttpManager();
            _wsTrustWebRequestManager = new WsTrustWebRequestManager(_httpManager);
        }

        /// <summary>
        /// Used to set the flag for AAD extended lifetime
        /// </summary>
        public bool ExtendedLifeTimeEnabled { get; set; }

        /// <summary>
        /// Gets address of the authority to issue token.
        /// </summary>
        public string Authority
        {
            get { return this.Authenticator.Authority; }
        }

        /// <summary>
        /// Gets a value indicating whether address validation is ON or OFF.
        /// </summary>
        public bool ValidateAuthority
        {
            get { return this.Authenticator.ValidateAuthority; }
        }

        /// <summary>
        /// ADAL's token cache, where tokens are automatically saved.
        /// On some platforms, e.g. iOS, Android and UWP, the TokenCache is backed by a persistent store which is implemeted by ADAL.
        /// On other platforms, e.g. .net and .net core - the developer is responsible for implementing a persistent store.
        /// If not using a persistent store, an in-memory store is used, which is destroyed once the application stops. 
        /// To find out more about leveraging the token cache visit: https://aka.ms/adal-net-using-cached-tokens
        /// To find out more about implementing a persistent store, visit: https://aka.ms/adal-net-cache-serialization
        /// </summary>
        public TokenCache TokenCache { get; private set; }

        /// <summary>
        /// Gets or sets correlation Id which would be sent to the service with the next request.
        /// Correlation Id is to be used for diagnostics purposes.
        /// </summary>
        public Guid CorrelationId
        {
            get { return this.Authenticator.CorrelationId; }

            set { this.Authenticator.CorrelationId = value; }
        }

#if iOS

        private string keychainSecurityGroup;

        /// <summary>
        /// Xamarin iOS specific property enables the application to share the token cache with other applications sharing the same keychain security group.
        /// If you provide this key, you MUST add the capability to your Application Entitlement.
        /// For more details, please see https://aka.ms/adal-net-sharing-cache-on-ios
        /// </summary>
        /// <remarks>This API may change in future release.</remarks>
        public string KeychainSecurityGroup
        {
            get
            {
                return keychainSecurityGroup;
            }
            set
            {
                keychainSecurityGroup = value;
                StorageDelegates.LegacyCachePersistence.SetKeychainSecurityGroup(value);
                TokenCache.tokenCacheAccessor.SetKeychainSecurityGroup(value);
            }
        }
#endif

        /// <summary>
        /// Acquires device code from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <returns>It contains Device Code, its expiration time, User Code.</returns>
#if iOS
        [Obsolete("This device profile API should only be used on text-only devices, and not on this target platform which is offering an interactive authentication experience. For details please see https://aka.ms/AdalNetConfFlows")]
#endif
        public async Task<DeviceCodeResult> AcquireDeviceCodeAsync(string resource, string clientId)
        {
            return await this.AcquireDeviceCodeAsync(resource, clientId, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires device code from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>It contains Device Code, its expiration time, User Code.</returns>
#if iOS
        [Obsolete("This device profile API should only be used on text-only devices, and not on this target platform which is offering an interactive authentication experience. For details please see https://aka.ms/AdalNetConfFlows")]
#endif
        public async Task<DeviceCodeResult> AcquireDeviceCodeAsync(string resource, string clientId,
            string extraQueryParameters)
        {
            var handler = new AcquireDeviceCodeHandler(this.Authenticator, resource, clientId, extraQueryParameters);
            return await handler.RunHandlerAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority using an device code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="deviceCodeResult">The device code result received from calling AcquireDeviceCodeAsync.</param>
        /// <returns>It contains Access Token, its expiration time, user information.</returns>
#if iOS
        [Obsolete("This device profile API should only be used on text-only devices, and not on this target platform which is offering an interactive authentication experience. For details please see https://aka.ms/AdalNetConfFlows")]
#endif
        public async Task<AuthenticationResult> AcquireTokenByDeviceCodeAsync(DeviceCodeResult deviceCodeResult)
        {
            if (deviceCodeResult == null)
            {
                throw new ArgumentNullException("deviceCodeResult");
            }

            RequestData requestData = new RequestData
            {
                Authenticator = this.Authenticator,
                TokenCache = this.TokenCache,
                ExtendedLifeTimeEnabled = this.ExtendedLifeTimeEnabled,
                Resource = deviceCodeResult.Resource,
                ClientKey = new ClientKey(deviceCodeResult.ClientId)
            };

            var handler = new AcquireTokenByDeviceCodeHandler(requestData, deviceCodeResult);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user, passing in the necessary claims for authentication. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="parameters">Instance of PlatformParameters containing platform specific arguments and information.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="claims">Additional claims that are needed for authentication. Acquired from the AdalClaimChallengeException</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId, Uri redirectUri,
            IPlatformParameters parameters,
            UserIdentifier userId, string extraQueryParameters, string claims)
        {
            return await this.AcquireTokenWithClaimsCommonAsync(resource, new ClientKey(clientId), redirectUri,
                    parameters,
                    userId, extraQueryParameters, this.CreateWebAuthenticationDialog((PlatformParameters)parameters), claims)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <returns>It contains Access Token, its expiration time, user information. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId)
        {
            return await this.AcquireTokenSilentAsync(resource, clientId, UserIdentifier.AnyUser).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, its expiration time, user information. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId,
            UserIdentifier userId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientId), userId, null)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="parameters">Instance of PlatformParameters containing platform specific arguments and information.</param>
        /// <returns>It contains Access Token, its expiration time, user information. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId,
            UserIdentifier userId, IPlatformParameters parameters)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientId), userId, parameters)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="parameters">An object of type PlatformParameters which may pass additional parameters used for authorization.</param>
        /// <returns>It contains Access Token, its expiration time, user information.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId, Uri redirectUri,
            IPlatformParameters parameters)
        {
            return await this
                .AcquireTokenCommonAsync(resource, clientId, redirectUri, parameters, UserIdentifier.AnyUser)
                .ConfigureAwait(false);
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
        /// <returns>It contains Access Token, its expiration time, user information.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId, Uri redirectUri,
            IPlatformParameters parameters, UserIdentifier userId)
        {
            return await this.AcquireTokenCommonAsync(resource, clientId, redirectUri, parameters, userId)
                .ConfigureAwait(false);
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
        /// <returns>It contains Access Token, its expiration time, user information.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId, Uri redirectUri,
            IPlatformParameters parameters, UserIdentifier userId, string extraQueryParameters)
        {
            return await this.AcquireTokenCommonAsync(resource, clientId, redirectUri, parameters, userId,
                extraQueryParameters).ConfigureAwait(false);
        }

        internal async Task<Uri> GetAuthorizationRequestUrlCommonAsync(string resource, string clientId,
            Uri redirectUri,
            UserIdentifier userId, string extraQueryParameters, string claims)
        {
            RequestData requestData = new RequestData
            {
                Authenticator = this.Authenticator,
                TokenCache = this.TokenCache,
                Resource = resource,
                ClientKey = new ClientKey(clientId),
                ExtendedLifeTimeEnabled = ExtendedLifeTimeEnabled
            };
            var handler = new AcquireTokenInteractiveHandler(requestData, redirectUri, null, userId,
                extraQueryParameters, null, claims);
            return await handler.CreateAuthorizationUriAsync(this.CorrelationId).ConfigureAwait(false);
        }

        internal async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeCommonAsync(string authorizationCode,
            Uri redirectUri, ClientKey clientKey, string resource)
        {
            const string nullResource = "null_resource_as_optional";
            RequestData requestData = new RequestData
            {
                Authenticator = this.Authenticator,
                TokenCache = this.TokenCache,
                Resource = resource,
                ClientKey = clientKey,
                ExtendedLifeTimeEnabled = this.ExtendedLifeTimeEnabled
            };
            if (requestData.Resource == null)
            {
                requestData.Resource = nullResource;
            }
            var handler = new AcquireTokenByAuthorizationCodeHandler(requestData, authorizationCode, redirectUri);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal async Task<AuthenticationResult> AcquireTokenForClientCommonAsync(string resource, ClientKey clientKey)
        {
            RequestData requestData = new RequestData
            {
                Authenticator = this.Authenticator,
                TokenCache = this.TokenCache,
                Resource = resource,
                ClientKey = clientKey,
                ExtendedLifeTimeEnabled = this.ExtendedLifeTimeEnabled,
                SubjectType = TokenSubjectType.Client
            };
            var handler = new AcquireTokenForClientHandler(requestData);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal async Task<AuthenticationResult> AcquireTokenOnBehalfCommonAsync(string resource, ClientKey clientKey,
            UserAssertion userAssertion)
        {
            RequestData requestData = new RequestData
            {
                Authenticator = this.Authenticator,
                TokenCache = this.TokenCache,
                Resource = resource,
                ClientKey = clientKey,
                ExtendedLifeTimeEnabled = this.ExtendedLifeTimeEnabled
            };

            var handler = new AcquireTokenOnBehalfHandler(requestData, userAssertion);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenWithClaimsCommonAsync(string resource, ClientKey clientKey,
            Uri redirectUri, IPlatformParameters parameters,
            UserIdentifier userId, string extraQueryParameters, IWebUI webUI, string claims)
        {
            RequestData requestData = new RequestData
            {
                Authenticator = this.Authenticator,
                TokenCache = this.TokenCache,
                Resource = resource,
                ClientKey = clientKey,
                ExtendedLifeTimeEnabled = this.ExtendedLifeTimeEnabled
            };

            var handler = new AcquireTokenInteractiveHandler(requestData, redirectUri, parameters, userId,
                extraQueryParameters, webUI, claims);

            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal IWebUI CreateWebAuthenticationDialog(PlatformParameters parameters)
        {
            return WebUIFactoryProvider.WebUIFactory.CreateAuthenticationDialog(parameters.GetCoreUIParent(), null);
        }

        internal async Task<AuthenticationResult> AcquireTokenCommonAsync(
           string resource,
           string clientId,
           UsernamePasswordInput upInput)
        {
            RequestData requestData = new RequestData
            {
                Authenticator = this.Authenticator,
                TokenCache = this.TokenCache,
                Resource = resource,
                ClientKey = new ClientKey(clientId),
                ExtendedLifeTimeEnabled = this.ExtendedLifeTimeEnabled
            };

            var handler = new AcquireTokenUsernamePasswordHandler(_wsTrustWebRequestManager, requestData, upInput);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal async Task<AuthenticationResult> AcquireTokenCommonAsync(
            string resource,
            string clientId,
            IntegratedWindowsAuthInput iwaInput)
        {
            RequestData requestData = new RequestData
            {
                Authenticator = this.Authenticator,
                TokenCache = this.TokenCache,
                Resource = resource,
                ClientKey = new ClientKey(clientId),
                ExtendedLifeTimeEnabled = this.ExtendedLifeTimeEnabled
            };

            var handler = new AcquireTokenIWAHandler(_wsTrustWebRequestManager, requestData, iwaInput);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal async Task<AuthenticationResult> AcquireTokenCommonAsync(
            string resource,
            string clientId,
            UserAssertion userAssertion)
        {
            RequestData requestData = new RequestData
            {
                Authenticator = this.Authenticator,
                TokenCache = this.TokenCache,
                Resource = resource,
                ClientKey = new ClientKey(clientId),
                ExtendedLifeTimeEnabled = this.ExtendedLifeTimeEnabled,
            };
            var handler = new AcquireTokenUserAssertionHandler(requestData, userAssertion);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, string clientId,
            Uri redirectUri, IPlatformParameters parameters, UserIdentifier userId, string extraQueryParameters = null,
            string claims = null)
        {
            RequestData requestData = new RequestData
            {
                Authenticator = this.Authenticator,
                TokenCache = this.TokenCache,
                Resource = resource,
                ClientKey = new ClientKey(clientId),
                ExtendedLifeTimeEnabled = this.ExtendedLifeTimeEnabled,
            };
            var handler = new AcquireTokenInteractiveHandler(requestData, redirectUri, parameters, userId,
                extraQueryParameters, this.CreateWebAuthenticationDialog((PlatformParameters)parameters), claims);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(string resource, ClientKey clientKey,
            UserIdentifier userId, IPlatformParameters parameters)
        {
            RequestData requestData = new RequestData
            {
                Authenticator = Authenticator,
                TokenCache = this.TokenCache,
                Resource = resource,
                ExtendedLifeTimeEnabled = this.ExtendedLifeTimeEnabled,
                ClientKey = clientKey
            };

            var handler = new AcquireTokenSilentHandler(requestData, userId, parameters);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        // confidential client APIs

        /// <summary>
        /// Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("This API should is only relevant for confidential client use in .NET Framework 4.5 and .NET Core. You should not use it to build against the chosen platform. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<Uri> GetAuthorizationRequestUrlAsync(string resource,
            string clientId, Uri redirectUri,
            UserIdentifier userId, string extraQueryParameters)
        {
            return await GetAuthorizationRequestUrlAsync(resource, clientId, redirectUri, userId,
                extraQueryParameters,
                null).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="claims">Additional claims that are needed for authentication. Acquired from the AdalClaimChallengeException. This parameter can be null.</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("This API should is only relevant for confidential client use in .NET Framework 4.5 and .NET Core. You should not use it to build against the chosen platform. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<Uri> GetAuthorizationRequestUrlAsync(string resource,
            string clientId, Uri redirectUri,
            UserIdentifier userId, string extraQueryParameters, string claims)
        {
            return await GetAuthorizationRequestUrlCommonAsync(resource, clientId, redirectUri, userId, extraQueryParameters,
                    claims).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, its expiration time, user information. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource,
            ClientCredential clientCredential, UserIdentifier userId)
        {
            return await AcquireTokenSilentCommonAsync(resource, new ClientKey(clientCredential), userId, null)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, its expiration time, user information. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource,
            IClientAssertionCertificate clientCertificate, UserIdentifier userId)
        {
            return await AcquireTokenSilentCommonAsync(resource,
                new ClientKey(clientCertificate, Authenticator), userId, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, its expiration time, user information. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource,
            ClientAssertion clientAssertion, UserIdentifier userId)
        {
            return await AcquireTokenSilentCommonAsync(resource, new ClientKey(clientAssertion), userId, null)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority using authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="clientCredential">The credential to use for token acquisition.</param>
        /// <returns>It contains Access Token, its expiration time, user information.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode,
            Uri redirectUri, ClientCredential clientCredential)
        {
            return await AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                    new ClientKey(clientCredential), null).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="clientCredential">The credential to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, its expiration time, user information.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode,
            Uri redirectUri, ClientCredential clientCredential, string resource)
        {
            return await AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                    new ClientKey(clientCredential), resource).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup the token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token, its expiration time, user information.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode,
            Uri redirectUri, ClientAssertion clientAssertion)
        {
            return await AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                    new ClientKey(clientAssertion), null).ConfigureAwait(false);
        }

        /// <summary>
        /// In a Web App, attemps to acquire a security token from the authority using an authorization code previously received
        /// (after a call to one of the overrides of <see cref="M:AcquireTokenByAuthorizationCodeAsync">AcquireTokenByAuthorizationCodeAsync</see>). 
        /// For more details see https://aka.ms/adal-net-authorization-code. This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, its expiration time, user information.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode,
            Uri redirectUri, ClientAssertion clientAssertion, string resource)
        {
            return await AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                    new ClientKey(clientAssertion), resource).ConfigureAwait(false);
        }

#if !(ANDROID || iOS || WINDOWS_APP)
        /// <summary>
        /// In a Web App, attempts to acquire a security token from the authority using an authorization code previously received
        /// (after a call to one of the overrides of <see cref="M:AcquireTokenByAuthorizationCodeAsync">AcquireTokenByAuthorizationCodeAsync</see>). 
        /// For more details see https://aka.ms/adal-net-authorization-code. This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <param name="sendX5c">This parameter enables application developers to achieve easy certificates roll-over
        /// in Azure AD: setting this parameter to true will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or powershell/CLI operation)</param>
        /// <returns>It contains Access Token, its expiration time, user information.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode,
            Uri redirectUri, IClientAssertionCertificate clientCertificate, string resource, bool sendX5c)
        {
            return await AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                new ClientKey(clientCertificate, Authenticator) { SendX5c = sendX5c }, resource).ConfigureAwait(false);
        }
#endif

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup the token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token, its expiration time, user information.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode,
            Uri redirectUri, IClientAssertionCertificate clientCertificate)
        {
            return await AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                new ClientKey(clientCertificate, Authenticator), null).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup the token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, its expiration time, user information.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode,
            Uri redirectUri, IClientAssertionCertificate clientCertificate, string resource)
        {
            return await AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri,
                new ClientKey(clientCertificate, Authenticator), resource).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientCredential clientCredential,
            UserAssertion userAssertion)
        {
            return await AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientCredential), userAssertion)
                .ConfigureAwait(false);
        }


        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource,
            IClientAssertionCertificate clientCertificate, UserAssertion userAssertion)
        {
            return await AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientCertificate, Authenticator),
                    userAssertion).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// For more details, see https://aka.ms/adal-net-on-behalf-of
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <param name="sendX5c">This parameter enables application developers to achieve easy certificates roll-over
        /// in Azure AD: setting this parameter to true will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or powershell/CLI operation)</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource,
            IClientAssertionCertificate clientCertificate, UserAssertion userAssertion, bool sendX5c)
        {
            return await AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientCertificate, Authenticator) { SendX5c = sendX5c },
                    userAssertion).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientAssertion clientAssertion,
            UserAssertion userAssertion)
        {
            return await AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientAssertion), userAssertion)
                .ConfigureAwait(false);
        }


        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userAssertion">The assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId,
            UserAssertion userAssertion)
        {
            return await AcquireTokenCommonAsync(resource, clientId, userAssertion).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource,
            IClientAssertionCertificate clientCertificate)
        {
            return await AcquireTokenForClientCommonAsync(resource, new ClientKey(clientCertificate, Authenticator))
                .ConfigureAwait(false);
        }

#if !(ANDROID || iOS || WINDOWS_APP)
        /// <summary>
        /// Acquire a security token for the application (without a user) from the authority while enabling simplified Azure AD certificate roll over. 
        /// For more details, see https://aka.ms/adal-net-client-credentials
        /// IMPORTANT: this flow isn’t enabled on the service at the time of this SDK release (ADAL.Net 3.19).
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="sendX5c">This parameter enables application developers to achieve easy certificates roll-over 
        /// in Azure AD: setting this parameter to true will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or powershell/CLI operation)</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource,
            IClientAssertionCertificate clientCertificate, bool sendX5c)
        {
            return await AcquireTokenForClientCommonAsync(resource, new ClientKey(clientCertificate, Authenticator) { SendX5c = sendX5c })
                .ConfigureAwait(false);
        }
#endif

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")] 
#endif
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientAssertion clientAssertion)
        {
            return await AcquireTokenForClientCommonAsync(resource, new ClientKey(clientAssertion))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
#if ANDROID || iOS || WINDOWS_APP
        [Obsolete("As a security hygiene, this confidential flow API should not be used on this platform which only supports public client applications. For details please see https://aka.ms/AdalNetConfFlows")]
#endif
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientCredential clientCredential)
        {
            return await AcquireTokenForClientCommonAsync(resource, new ClientKey(clientCredential))
                .ConfigureAwait(false);
        }
    }
}
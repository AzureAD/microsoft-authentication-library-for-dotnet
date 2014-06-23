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
using System.Collections.Generic;
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
    public sealed partial class AuthenticationContext
    {
        internal Authenticator Authenticator;

        private readonly AuthorityType authorityType;
        private readonly TokenCacheManager tokenCacheManager;

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

#if ADAL_WINRT
#else
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
        public AuthenticationContext(string authority, bool validateAuthority, TokenCache tokenCache)
            : this(authority, validateAuthority ? AuthorityValidationType.True : AuthorityValidationType.False, tokenCache)
        {
        }

        private AuthenticationContext(string authority, AuthorityValidationType validateAuthority, TokenCache tokenCache)
        {
            this.Authority = AuthenticationMetadata.CanonicalizeUri(authority);

            this.authorityType = AuthenticationMetadata.DetectAuthorityType(this.Authority);

            // If authorityType is not provided (via first constructor), we validate by default (except for ASG and Office tenants).
            this.ValidateAuthority = (validateAuthority != AuthorityValidationType.False);

            this.tokenCacheManager = new TokenCacheManager(this.Authority, tokenCache, this.RefreshAccessTokenAsync);
        }

        /// <summary>
        /// Gets address of the authority to issue token.
        /// </summary>
        public string Authority { get; private set; }

        /// <summary>
        /// Gets a value indicating whether address validation is ON or OFF.
        /// </summary>
        public bool ValidateAuthority { get; private set; }

#if ADAL_WINRT
        /// <summary>
        /// Property to provide ADAL's token cache. By default, TokenCache is a persistent cache based on application's local settings. 
        /// Library will automatically save tokens in default TokenCache whenever you obtain them. Cached tokens will be available only to the application that saved them. 
        /// Cached tokens in default token cache will outlive the application's execution, and will be available in subsequent runs.
        /// To turn OFF token caching, set TokenCacheStore to null. 
        /// </summary>
#else
        /// <summary>
        /// Gets the TokenCache
        /// </summary>
        /// <remarks>
        /// By default, TokenCache is an in-memory collection of key/value pairs. 
        /// Library will automatically save tokens in the cache when AcquireToken is called.  
        /// The default token cache is static so all tokens will available to all instances of AuthenticationContext. To use a custom TokenCache pass one to the <see cref="AuthenticationContext">.constructor</see>.
        /// To turn OFF token caching, use the constructor and set TokenCache to null.
        /// </remarks>
#endif
        public TokenCache TokenCache
        {
            get
            {
                return this.tokenCacheManager.TokenCache;
            }
        }

        /// <summary>
        /// Gets or sets correlation Id which would be sent to the service with the next request. 
        /// Correlation Id is to be used for diagnostics purposes. 
        /// </summary>
        public Guid CorrelationId { get; set; }

        internal async Task CreateAuthenticatorAsync(CallState callState)
        {
            if (this.Authenticator == null)
            {
                this.Authenticator = await AuthenticationMetadata.CreateAuthenticatorAsync(this.ValidateAuthority, this.Authority, callState, this.authorityType);
            }
        }

        private static void VerifyUserMatch(UserIdentifier userId, AuthenticationResult result)
        {
            if (userId == null || userId.Type == UserIdentifierType.OptionalDisplayableId)
            {
                return;
            }

            string uniqueId = (result.UserInfo != null && result.UserInfo.UniqueId != null) ? result.UserInfo.UniqueId : "NULL";
            string displayableId = (result.UserInfo != null) ? result.UserInfo.DisplayableId : "NULL";

            if (userId.Type == UserIdentifierType.UniqueId
                && string.Compare(uniqueId, userId.Id, StringComparison.Ordinal) != 0)
            {
                throw new AdalUserMismatchException(userId.Id, uniqueId);    
            }
                
            if (userId.Type == UserIdentifierType.RequiredDisplayableId
                && string.Compare(displayableId, userId.Id, StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new AdalUserMismatchException(userId.Id, displayableId);    
            }
        }

        private static void LogReturnedToken(AuthenticationResult result, CallState callState)
        {
            if (result.AccessToken != null)
            {
                string accessTokenHash = PlatformSpecificHelper.CreateSha256Hash(result.AccessToken);
                string logMessage;
                if (result.RefreshToken != null)
                {
                    string refreshTokenHash = PlatformSpecificHelper.CreateSha256Hash(result.RefreshToken);
                    logMessage = string.Format("Access Token with hash '{0}' and Refresh Token with hash '{1}' returned", accessTokenHash, refreshTokenHash);
                }
                else
                {
                    logMessage = string.Format("Access Token with hash '{0}' returned", accessTokenHash);                    
                }

                Logger.Verbose(callState, logMessage);
            }
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, string clientId, UserCredential credential, bool callSync = false)
        {
            CallState callState = this.CreateCallState(callSync);
            this.ValidateAuthorityType(callState, AuthorityType.AAD);

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentNullException("resource");
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            if (credential == null)
            {
                throw new ArgumentNullException("credential");
            }

            // We cannot move the following lines to UserCredential as one of these calls in async. 
            // It cannot be moved to constructor or property or a pure sync or async call. This is why we moved it here which is an async call already.
            if (string.IsNullOrWhiteSpace(credential.UserName))
            {
#if ADAL_WINRT
                credential.UserName = await PlatformSpecificHelper.GetUserPrincipalNameAsync();
#else
                credential.UserName = PlatformSpecificHelper.GetUserPrincipalName();
#endif
                if (string.IsNullOrWhiteSpace(credential.UserName))
                {
                    Logger.Information(callState, "Could not find UPN for logged in user");
                    throw new AdalException(AdalError.UnknownUser);
                }

                Logger.Information(callState, "Logged in user '{0}' detected", credential.UserName);
            }

            await this.CreateAuthenticatorAsync(callState);

            try
            {
                this.NotifyBeforeAccessCache(resource, clientId, null, credential.UserName);
                AuthenticationResult result = await this.tokenCacheManager.LoadFromCacheAndRefreshIfNeededAsync(resource, callState, clientId, credential.UserName);
                if (result == null)
                {
                    UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(this.Authenticator.UserRealmUri, credential.UserName, callState);
                    Logger.Information(callState, "User '{0}' detected as '{1}'", credential.UserName, userRealmResponse.AccountType);
                    if (string.Compare(userRealmResponse.AccountType, "federated", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (string.IsNullOrWhiteSpace(userRealmResponse.FederationMetadataUrl))
                        {
                            throw new AdalException(AdalError.MissingFederationMetadataUrl);
                        }

                        Uri wsTrustUrl = await MexParser.FetchWsTrustAddressFromMexAsync(userRealmResponse.FederationMetadataUrl, credential.UserAuthType, callState);
                        Logger.Information(callState, "WS-Trust endpoint '{0}' fetched from MEX at '{1}'", wsTrustUrl, userRealmResponse.FederationMetadataUrl);

                        WsTrustResponse wsTrustResponse = await WsTrustRequest.SendRequestAsync(wsTrustUrl, credential, callState);
                        Logger.Information(callState, "Token of type '{0}' acquired from WS-Trust endpoint", wsTrustResponse.TokenType);

                        // We assume that if the response token type is not SAML 1.1, it is SAML 2
                        var samlCredential = new UserAssertion(
                            wsTrustResponse.Token,
                            (wsTrustResponse.TokenType == WsTrustResponse.Saml1Assertion) ? OAuthGrantType.Saml11Bearer : OAuthGrantType.Saml20Bearer);

                        result = await OAuth2Request.SendTokenRequestWithUserAssertionAsync(this.Authenticator.TokenUri, resource, clientId, samlCredential, callState);
                        Logger.Information(callState, "Token of type '{0}' acquired from OAuth endpoint '{1}'", result.AccessTokenType, this.Authenticator.TokenUri);

                        await this.UpdateAuthorityTenantAsync(result.TenantId, callState);
                        this.tokenCacheManager.StoreToCache(result, resource, clientId);
                    }
                    else if (string.Compare(userRealmResponse.AccountType, "managed", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        //handle password grant flow for the managed user
                        if (credential.PasswordToCharArray() == null)
                        {
                            throw new AdalException(AdalError.PasswordRequiredForManagedUserError);
                        }

                        result = await OAuth2Request.SendTokenRequestWithUserCredentialAsync(this.Authenticator.TokenUri, resource, clientId, credential, callState);
                    }
                    else
                    {
                        throw new AdalException(AdalError.UnknownUserType);
                    }
                }

                LogReturnedToken(result, callState);
                return result;
            }
            finally
            {
                this.NotifyAfterAccessCache(resource, clientId, null, credential.UserName);
            }
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, string clientId, UserAssertion credential, bool callSync = false)
        {
            CallState callState = this.CreateCallState(callSync);
            this.ValidateAuthorityType(callState, AuthorityType.AAD);

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentNullException("resource");
            }

            if (credential == null)
            {
                throw new ArgumentNullException("credential");
            }

            if (string.IsNullOrWhiteSpace(credential.AssertionType))
            {
                throw new ArgumentException(AdalErrorMessage.UserCredentialAssertionTypeEmpty, "credential");
            }

            await this.CreateAuthenticatorAsync(callState);

            try
            {
                this.NotifyBeforeAccessCache(resource, clientId, null, credential.UserName);
                AuthenticationResult result = await this.tokenCacheManager.LoadFromCacheAndRefreshIfNeededAsync(resource, callState, clientId, credential.UserName);
                if (result == null)
                {
                    result = await OAuth2Request.SendTokenRequestWithUserAssertionAsync(this.Authenticator.TokenUri, resource, clientId, credential, callState);
                    Logger.Information(callState, "Token of type '{0}' acquired from OAuth endpoint '{1}'", result.AccessTokenType, this.Authenticator.TokenUri);

                    await this.UpdateAuthorityTenantAsync(result.TenantId, callState);
                    this.tokenCacheManager.StoreToCache(result, resource, clientId);
                }

                LogReturnedToken(result, callState);
                return result;
            }
            finally
            {
                this.NotifyAfterAccessCache(resource, clientId, null, credential.UserName);
            }
        }

        private async Task<AuthenticationResult> AcquireTokenByRefreshTokenCommonAsync(string refreshToken, string clientId, string resource, bool callSync = false)
        {
            CallState callState = this.CreateCallState(callSync);
            this.ValidateAuthorityType(callState, AuthorityType.AAD, AuthorityType.ADFS);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentNullException("refreshToken");
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            await this.CreateAuthenticatorAsync(callState);

            if (!string.IsNullOrWhiteSpace(resource) && this.authorityType != AuthorityType.AAD)
            {
                throw new ArgumentException(AdalErrorMessage.UnsupportedMultiRefreshToken, "resource");
            }

            AuthenticationResult result = await this.SendOAuth2RequestByRefreshTokenAsync(resource, refreshToken, clientId, callState);
            LogReturnedToken(result, callState);
            return result;
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, string clientId, Uri redirectUri, PromptBehavior promptBehavior = PromptBehavior.Auto, UserIdentifier userId = null, string extraQueryParameters = null, bool callSync = false)
        {
            CallState callState = this.CreateCallState(callSync);
            this.ValidateAuthorityType(callState, AuthorityType.AAD, AuthorityType.ADFS);

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentNullException("resource");
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            if (redirectUri == null)
            {
                throw new ArgumentNullException("redirectUri");
            }

            await this.CreateAuthenticatorAsync(callState);

            AuthenticationResult result = null;

            try
            {
                if (promptBehavior != PromptBehavior.Always && promptBehavior != PromptBehavior.RefreshSession)
                {
                    this.NotifyBeforeAccessCache(resource, clientId,
                        (userId != null && userId.Type == UserIdentifierType.UniqueId) ? userId.Id : null,
                        (userId != null && (userId.Type == UserIdentifierType.OptionalDisplayableId || userId.Type == UserIdentifierType.RequiredDisplayableId) ? userId.Id : null));

                    result = await this.tokenCacheManager.LoadFromCacheAndRefreshIfNeededAsync(resource, callState, clientId, userId);
                }

                result = result ?? await this.AcquireTokenFromStsAsync(resource, clientId, redirectUri, promptBehavior, userId, extraQueryParameters, callState);
                LogReturnedToken(result, callState);
                return result;
            }
            finally
            {
                this.NotifyAfterAccessCache(resource, clientId,
                    (userId != null && userId.Type == UserIdentifierType.UniqueId) ? userId.Id : null,
                    (userId != null && (userId.Type == UserIdentifierType.OptionalDisplayableId || userId.Type == UserIdentifierType.RequiredDisplayableId) ? userId.Id : null));
            }
        }

        private async Task<AuthenticationResult> AcquireTokenFromStsAsync(string resource, string clientId, Uri redirectUri, PromptBehavior promptBehavior, UserIdentifier userId, string extraQueryParameters, CallState callState)
        {
            AuthenticationResult result;
#if ADAL_WINRT
            AuthorizationResult authorizationResult = await this.AcquireAuthorizationAsync(resource, clientId, redirectUri, userId, promptBehavior, extraQueryParameters, callState);
#else
            // We do not have async interactive API in .NET, so we call this synchronous method instead.
            AuthorizationResult authorizationResult = this.AcquireAuthorization(resource, clientId, redirectUri, userId, promptBehavior, extraQueryParameters, callState);
#endif

            if (promptBehavior == PromptBehavior.Never && authorizationResult.Error == OAuthError.LoginRequired)
            {
                throw new AdalException(AdalError.UserInteractionRequired);
            }

            if (authorizationResult.Status == AuthorizationStatus.Success)
            {
                string uri = this.Authenticator.TokenUri;
                result = await OAuth2Request.SendTokenRequestAsync(uri, authorizationResult.Code, redirectUri, resource, clientId, callState);
                await this.UpdateAuthorityTenantAsync(result.TenantId, callState);
                this.tokenCacheManager.StoreToCache(result, resource, clientId);

                VerifyUserMatch(userId, result);
            }
            else
            {
                result = PlatformSpecificHelper.ProcessServiceError(authorizationResult.Error, authorizationResult.ErrorDescription);
            }

            return result;
        }

        private async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(string resource, string clientId, UserIdentifier userId)
        {
            CallState callState = this.CreateCallState(false);
            this.ValidateAuthorityType(callState, AuthorityType.AAD, AuthorityType.ADFS);

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentNullException("resource");
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            await this.CreateAuthenticatorAsync(callState);

            try
            {
                this.NotifyBeforeAccessCache(resource, clientId,
                    (userId != null && userId.Type == UserIdentifierType.UniqueId) ? userId.Id : null,
                    (userId != null && (userId.Type == UserIdentifierType.OptionalDisplayableId || userId.Type == UserIdentifierType.RequiredDisplayableId)) ? userId.Id : null);

                AuthenticationResult result = await this.tokenCacheManager.LoadFromCacheAndRefreshIfNeededAsync(resource, callState, clientId, userId);

                if (result != null)
                {
                    LogReturnedToken(result, callState);
                }
                else
                {
                    Logger.Verbose(callState, "No token matching arguments found in the cache");
                    throw new AdalException(AdalError.FailedToAcquireTokenSilently);
                }

                return result;
            }
            finally
            {
                this.NotifyAfterAccessCache(resource, clientId,
                    (userId != null && userId.Type == UserIdentifierType.UniqueId) ? userId.Id : null,
                    (userId != null && (userId.Type == UserIdentifierType.OptionalDisplayableId || userId.Type == UserIdentifierType.RequiredDisplayableId) ? userId.Id : null));
            }
        }

        private void ValidateAuthorityType(CallState callState, AuthorityType validAuthorityType1, AuthorityType validAuthorityType2 = AuthorityType.Unknown, AuthorityType validAuthorityType3 = AuthorityType.Unknown)
        {
            if ((this.authorityType != validAuthorityType1) &&
                (this.authorityType != validAuthorityType2 || validAuthorityType2 == AuthorityType.Unknown) &&
                (this.authorityType != validAuthorityType3 || validAuthorityType3 == AuthorityType.Unknown))
            {
                Logger.Error(callState, "Invalid authority type '{0}'", this.authorityType);
                throw new AdalException(AdalError.InvalidAuthorityType, 
                    string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.InvalidAuthorityTypeTemplate, this.Authority));
            }
        }

        private async Task<AuthenticationResult> SendOAuth2RequestByRefreshTokenAsync(string resource, string refreshToken, string clientId, CallState callState)
        {
            AuthenticationResult result = await OAuth2Request.SendTokenRequestByRefreshTokenAsync(Authenticator.TokenUri, resource, refreshToken, clientId, callState);
            await this.UpdateAuthorityTenantAsync(result.TenantId, callState);
            return result;
        }

        private async Task<AuthenticationResult> RefreshAccessTokenAsync(AuthenticationResult result, string resource, string clientId, CallState callState)
        {
            AuthenticationResult newResult = null;

            if (result != null && resource != null && clientId != null)
            { 
                try
                {
                    newResult = await this.SendOAuth2RequestByRefreshTokenAsync(resource, result.RefreshToken, clientId, callState);

                    if (newResult.IdToken == null)
                    {
                        // If Id token is not returned by token endpoint when refresh token is redeemed, we should copy tenant and user information from the cached token.
                        newResult.UpdateTenantAndUserInfo(result.TenantId, result.IdToken, result.UserInfo);
                    }
                }
                catch (AdalException) 
                {
                    // TODO: Verify if this is the only exception type
                    newResult = null;
                }
            }

            return newResult;
        }

        private async Task UpdateAuthorityTenantAsync(string tenantId, CallState callState)
        {
            if (this.Authenticator.IsTenantless && !string.IsNullOrWhiteSpace(tenantId))
            {
                this.Authority = AuthenticationMetadata.ReplaceTenantlessTenant(this.Authority, tenantId);
                this.tokenCacheManager.Authority = this.Authority;
                this.Authenticator = await AuthenticationMetadata.CreateAuthenticatorAsync(this.ValidateAuthority, this.Authority, callState);
            }
        }

        private CallState CreateCallState(bool callSync)
        {
            Guid correlationId = (this.CorrelationId != Guid.Empty) ? this.CorrelationId : Guid.NewGuid();
            return new CallState(correlationId, callSync);
        }

        private void NotifyBeforeAccessCache(string resource, string clientId, string uniqueId, string displayableId)
        {
            if (this.TokenCache != null)
            {
                this.TokenCache.OnBeforeAccess(new TokenCacheNotificationArgs
                    {
                        TokenCache = this.TokenCache,
                        Resource = resource,
                        ClientId = clientId,
                        UniqueId = uniqueId,
                        DisplayableId = displayableId
                    });
            }
        }

        private void NotifyAfterAccessCache(string resource, string clientId, string uniqueId, string displayableId)
        {
            if (this.TokenCache != null)
            {
                this.TokenCache.OnAfterAccess(new TokenCacheNotificationArgs
                    {
                        TokenCache = this.TokenCache,
                        Resource = resource,
                        ClientId = clientId,
                        UniqueId = uniqueId,
                        DisplayableId = displayableId
                    });
            }
        }
    }
}

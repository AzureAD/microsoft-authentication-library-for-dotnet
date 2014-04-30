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
            : this(authority, AuthorityValidationType.NotProvided, StaticTokenCacheStore)
        {
        }

        /// <summary>
        /// Constructor to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
        public AuthenticationContext(string authority, bool validateAuthority)
            : this(authority, validateAuthority ? AuthorityValidationType.True : AuthorityValidationType.False, StaticTokenCacheStore)
        {
        }

#if ADAL_WINRT
#else
        /// <summary>
        /// Constructor to create the context with the address of the authority.
        /// Using this constructor will turn ON validation of the authority URL by default if validation is supported for the authority address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="tokenCacheStore">Token Cache store used to lookup cached tokens on calls to AcquireToken</param>
        public AuthenticationContext(string authority, IDictionary<TokenCacheKey, string> tokenCacheStore)
            : this(authority, AuthorityValidationType.NotProvided, tokenCacheStore)
        {
        }
#endif

        /// <summary>
        /// Constructor to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
        /// <param name="tokenCacheStore">Token Cache store used to lookup cached tokens on calls to AcquireToken</param>
        public AuthenticationContext(string authority, bool validateAuthority, IDictionary<TokenCacheKey, string> tokenCacheStore)
            : this(authority, validateAuthority ? AuthorityValidationType.True : AuthorityValidationType.False, tokenCacheStore)
        {
        }

        private AuthenticationContext(string authority, AuthorityValidationType validateAuthority, IDictionary<TokenCacheKey, string> tokenCacheStore)
        {
            this.Authority = AuthenticationMetadata.CanonicalizeUri(authority);

            this.authorityType = AuthenticationMetadata.DetectAuthorityType(this.Authority);

            // If authorityType is not provided (via first constructor), we validate by default (except for ASG and Office tenants).
            this.ValidateAuthority = (validateAuthority != AuthorityValidationType.False);

            this.tokenCacheManager = new TokenCacheManager(this.Authority, tokenCacheStore, this.RefreshAccessTokenAsync);
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
        /// Property to provide storage for ADAL's token cache. By default, TokenCacheStore is set a persistent store based on application's local settings. 
        /// Library will automatically save tokens in default TokenCacheStore whenever you obtain them. Cached tokens will be available only to the application that saved them. 
        /// Cached tokens in default TokenCacheStore will outlive the application's execution, and will be available in subsequent runs.
        /// To turn OFF token caching, set TokenCacheStore to null. 
        /// </summary>
#else
        /// <summary>
        /// Gets the TokenCacheStore 
        /// </summary>
        /// <remarks>
        /// By default, TokenCacheStore is set to a Dictionary which makes the token cache an in-memory collection of key/value pairs. 
        /// Library will automatically save tokens in the cache when AcquireToken is called.  
        /// The default token cache is static so all tokens will available to all instances of AuthenticationContext. To use a custom TokenCacheStore pass one to the <see cref="AuthenticationContext">.constructor</see>.
        /// To turn OFF token caching, use the constructor and set TokenCacheStore to null.
        /// </remarks>
#endif
        public IDictionary<TokenCacheKey, string> TokenCacheStore
        {
            get
            {
                return this.tokenCacheManager.TokenCacheStore;
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

        private static void VerifyUserMatch(string userId, AuthenticationResult result)
        {
            if (!string.IsNullOrWhiteSpace(userId) && result.UserInfo != null && result.UserInfo.IsUserIdDisplayable && RegexUtilities.IsValidEmail(userId) &&
                string.Compare(result.UserInfo.UserId, userId, StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new ActiveDirectoryAuthenticationException(ActiveDirectoryAuthenticationError.UserMismatch,
                    string.Format(ActiveDirectoryAuthenticationErrorMessage.UserMismatch, result.UserInfo.UserId, userId));
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
            if (string.IsNullOrWhiteSpace(credential.UserId))
            {
#if ADAL_WINRT
                if (!Windows.System.UserProfile.UserInformation.NameAccessAllowed)
                {
                    throw new ActiveDirectoryAuthenticationException(
                        ActiveDirectoryAuthenticationError.CannotAccessUserInformation,
                        ActiveDirectoryAuthenticationErrorMessage.CannotAccessUserInformation);                    
                }

                try
                {
                    credential.UserId = await Windows.System.UserProfile.UserInformation.GetPrincipalNameAsync();
                    if (string.IsNullOrWhiteSpace(credential.UserId))
                    {
                        throw new ActiveDirectoryAuthenticationException(ActiveDirectoryAuthenticationError.UnknownUser);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new ActiveDirectoryAuthenticationException(
                        ActiveDirectoryAuthenticationError.UnauthorizedUserInformationAccess,
                        ActiveDirectoryAuthenticationErrorMessage.UnauthorizedUserInformationAccess,
                        ex);
                }
#else
                credential.UserId = System.DirectoryServices.AccountManagement.UserPrincipal.Current.UserPrincipalName;
#endif
                Logger.Information(callState, "Logged in user '{0}' detected", credential.UserId);
            }

            await this.CreateAuthenticatorAsync(callState);

            AuthenticationResult result = await this.tokenCacheManager.LoadFromCacheAndRefreshIfNeededAsync(resource, callState, clientId, credential.UserId);
            if (result == null)
            {
                UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(this.Authenticator.UserRealmUri, credential.UserId, callState);
                Logger.Information(callState, "User '{0}' detected as '{1}'", credential.UserId, userRealmResponse.AccountType);
                if (string.Compare(userRealmResponse.AccountType, "federated", StringComparison.OrdinalIgnoreCase) == 0)
                {
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

                    VerifyUserMatch(credential.UserId, result);
                }
                else if (string.Compare(userRealmResponse.AccountType, "managed", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    //handle password grant flow for the managed user
                    if (credential.PasswordToCharArray() == null)
                    {
                        throw new ArgumentNullException(
                            ActiveDirectoryAuthenticationErrorMessage.PasswordRequiredForManagedUserError,
                            (Exception) null);
                    }

                    result = await OAuth2Request.SendTokenRequestWithUserCredentialAsync(this.Authenticator.TokenUri, resource, clientId, credential, callState);
                }
                else
                {
                    throw new ActiveDirectoryAuthenticationException(ActiveDirectoryAuthenticationError.UnknownUserType);
                }
            }

            LogReturnedToken(result, callState);
            return result;
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
                throw new ArgumentException(ActiveDirectoryAuthenticationErrorMessage.UserCredentialAssertionTypeEmpty, "credential");
            }

            await this.CreateAuthenticatorAsync(callState);

            AuthenticationResult result = await this.tokenCacheManager.LoadFromCacheAndRefreshIfNeededAsync(resource, callState, clientId, credential.UserId);
            if (result == null)
            {
                result = await OAuth2Request.SendTokenRequestWithUserAssertionAsync(this.Authenticator.TokenUri, resource, clientId, credential, callState);
                Logger.Information(callState, "Token of type '{0}' acquired from OAuth endpoint '{1}'", result.AccessTokenType, this.Authenticator.TokenUri);

                await this.UpdateAuthorityTenantAsync(result.TenantId, callState);
                this.tokenCacheManager.StoreToCache(result, resource, clientId);

                VerifyUserMatch(credential.UserId, result);
            }

            LogReturnedToken(result, callState);
            return result;
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
                throw new ArgumentException(ActiveDirectoryAuthenticationErrorMessage.UnsupportedMultiRefreshToken, "resource");
            }

            AuthenticationResult result = await this.SendOAuth2RequestByRefreshTokenAsync(resource, refreshToken, clientId, callState);
            LogReturnedToken(result, callState);
            return result;
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, string clientId, Uri redirectUri, string userId = null, PromptBehavior promptBehavior = PromptBehavior.Auto, string extraQueryParameters = null, bool callSync = false)
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
            if (promptBehavior != PromptBehavior.Always)
            {
                result = await this.tokenCacheManager.LoadFromCacheAndRefreshIfNeededAsync(resource, callState, clientId, userId);
            }

            result = result ?? await this.AcquireTokenFromStsAsync(resource, clientId, redirectUri, userId, promptBehavior, extraQueryParameters, callState);
            LogReturnedToken(result, callState);
            return result;
        }

        private async Task<AuthenticationResult> AcquireTokenFromStsAsync(string resource, string clientId, Uri redirectUri, string userId, PromptBehavior promptBehavior, string extraQueryParameters, CallState callState)
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
                throw new ActiveDirectoryAuthenticationException(ActiveDirectoryAuthenticationError.UserInteractionRequired, ActiveDirectoryAuthenticationErrorMessage.UserInteractionRequired);
            }

            if (authorizationResult.Status == AuthorizationStatus.Succeeded)
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

        private void ValidateAuthorityType(CallState callState, AuthorityType validAuthorityType1, AuthorityType validAuthorityType2 = AuthorityType.Unknown, AuthorityType validAuthorityType3 = AuthorityType.Unknown)
        {
            if ((this.authorityType != validAuthorityType1) &&
                (this.authorityType != validAuthorityType2 || validAuthorityType2 == AuthorityType.Unknown) &&
                (this.authorityType != validAuthorityType3 || validAuthorityType3 == AuthorityType.Unknown))
            {
                Logger.Error(callState, "Invalid authority type '{0}'", this.authorityType);
                throw new ActiveDirectoryAuthenticationException(ActiveDirectoryAuthenticationError.InvalidAuthorityType, string.Format(CultureInfo.InvariantCulture, ActiveDirectoryAuthenticationErrorMessage.InvalidAuthorityTypeTemplate, this.Authority));
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

                    // Id token is not returned by token endpoint when refresh token is redeemed. Therefore, we should copy tenant and user information from the cached token.
                    newResult.UpdateTenantAndUserInfo(result.TenantId, result.UserInfo);
                }
                catch (ActiveDirectoryAuthenticationException) 
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
    }
}

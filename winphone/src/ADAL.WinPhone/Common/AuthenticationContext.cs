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
                    private AuthenticationContext(string authority)
            : this(authority, AuthorityValidationType.NotProvided, StaticTokenCacheStore)
        {
        }

        private AuthenticationContext(string authority, bool validateAuthority)
            : this(
                authority, validateAuthority ? AuthorityValidationType.True : AuthorityValidationType.False,
                StaticTokenCacheStore)
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

        /// <summary>
        /// Property to provide storage for ADAL's token cache. By default, TokenCacheStore is set a persistent store based on application's local settings. 
        /// Library will automatically save tokens in default TokenCacheStore whenever you obtain them. Cached tokens will be available only to the application that saved them. 
        /// Cached tokens in default TokenCacheStore will outlive the application's execution, and will be available in subsequent runs.
        /// To turn OFF token caching, set TokenCacheStore to null. 
        /// </summary>
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

        private async Task<AuthenticationResult> AcquireTokenByRefreshTokenCommonAsync(string refreshToken, string clientId, string resource)
        {
            CallState callState = this.CreateCallState();
            this.ValidateAuthorityType(callState, AuthorityType.AAD, AuthorityType.ADFS);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentNullException("refreshToken");
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            if (!string.IsNullOrWhiteSpace(resource) && this.authorityType != AuthorityType.AAD)
            {
                throw new ArgumentException(ActiveDirectoryAuthenticationErrorMessage.UnsupportedMultiRefreshToken, "resource");
            }

            AuthenticationResult result = await this.SendOAuth2RequestByRefreshTokenAsync(resource, refreshToken, clientId, callState);
            LogReturnedToken(result, callState);
            return result;
        }


        private async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(string resource, string clientId, string userId)
        {
            CallState callState = this.CreateCallState();
            this.ValidateAuthorityType(callState, AuthorityType.AAD, AuthorityType.ADFS);

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentNullException("resource");
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            //await this.CreateAuthenticatorAsync(callState);

            AuthenticationResult result = await this.tokenCacheManager.LoadFromCacheAndRefreshIfNeededAsync(resource, callState, clientId, userId);
            if (result != null)
            {
                LogReturnedToken(result, callState);
            }
            return result;
        }

        private void AcquireTokenAndContinueCommon(string resource, string clientId, Uri redirectUri, string userId = null, string extraQueryParameters = null)
        {
            CallState callState = this.CreateCallState();
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

            this.AcquireTokenFromSts(resource, clientId, redirectUri, userId, extraQueryParameters, callState);
        }

        private void AcquireTokenFromSts(string resource, string clientId, Uri redirectUri, string userId, string extraQueryParameters, CallState callState)
        {
            this.AcquireAuthorization(resource, clientId, redirectUri, userId, extraQueryParameters, callState);
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

        private CallState CreateCallState()
        {
            Guid correlationId = (this.CorrelationId != Guid.Empty) ? this.CorrelationId : Guid.NewGuid();
            return new CallState(correlationId);
        }
    }
}

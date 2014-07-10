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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal abstract class AcquireTokenHandlerBase
    {
        private readonly TokenCache tokenCache;

        protected const string NullResource = "null_resource_as_optional";

        protected AcquireTokenHandlerBase(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, TokenSubjectType subjectType, bool callSync)
        {
            this.Authenticator = authenticator;

            this.tokenCache = tokenCache;

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentNullException("resource");
            }

            this.Resource = (resource != NullResource) ? resource : null;

            this.ClientKey = clientKey;

            this.SubjectType = subjectType;

            this.CallState = this.CreateCallState(callSync);

            this.LoadFromCache = true;
            this.StoreToCache = true;
        }

        protected bool SupportADFS { get; set; }

        protected Authenticator Authenticator { get; private set; }

        protected string Resource { get; set; }

        protected ClientKey ClientKey { get; private set; }

        internal CallState CallState { get; set; }

        protected TokenSubjectType SubjectType { get; private set; }

        protected string UniqueId { get; set; }

        protected string DisplayableId { get; set; }

        protected UserIdentifierType UserIdentifierType { get; set; }

        protected bool LoadFromCache { get; set; }
        
        protected bool StoreToCache { get; set; }

        private CallState CreateCallState(bool callSync)
        {
            Guid correlationId = (this.Authenticator.CorrelationId != Guid.Empty) ? this.Authenticator.CorrelationId : Guid.NewGuid();
            return new CallState(correlationId, callSync);
        }

        public async Task<AuthenticationResult> RunAsync()
        {
            await this.Authenticator.UpdateFromMetadataAsync(this.CallState);
            this.ValidateAuthorityType();

#if ADAL_WINRT
            await SetUserIdentifiersAsync();
#else
            SetUserIdentifiers();
#endif

            try
            {
                AuthenticationResult result = null;
                if (this.LoadFromCache && this.tokenCache != null)
                { 
                    this.NotifyBeforeAccessCache(this.Resource, this.ClientKey.ClientId, this.UniqueId, this.DisplayableId);
                    result = this.tokenCache.LoadFromCache(this.Authenticator.Authority, this.Resource, this.CallState, this.ClientKey, this.Authenticator.SelfSignedJwtAudience, this.UniqueId, this.DisplayableId, SubjectType);
                }
                
                if (result != null && result.RequiresRefresh)
                {
                    AuthenticationResult refreshedResult = await this.RefreshAccessTokenAsync(result, this.Resource, this.ClientKey, this.Authenticator.SelfSignedJwtAudience, this.CallState);
                    if (refreshedResult != null)
                    {
                        this.tokenCache.StoreToCache(refreshedResult, this.Authenticator.Authority, this.Resource, this.SubjectType, this.ClientKey.ClientId);
                    }

                    result = refreshedResult;                    
                }

                if (result == null)
                {
                    result = await this.SendTokenRequestAsync();

                    await this.Authenticator.UpdateAuthorityTenantAsync(result.TenantId, this.CallState);

                    if (this.StoreToCache && this.tokenCache != null)
                    {
                        this.tokenCache.StoreToCache(result, this.Authenticator.Authority, this.Resource, this.SubjectType, this.ClientKey.ClientId);
                    }
                }

                LogReturnedToken(result, this.CallState);
                return result;
            }
            finally
            {
                if (this.StoreToCache)
                {
                    this.NotifyAfterAccessCache(this.Resource, this.ClientKey.ClientId, this.UniqueId, this.DisplayableId);
                }
            }
        }

#if ADAL_WINRT
        protected virtual Task SetUserIdentifiersAsync()
        {
            return Task.FromResult(false);
        }
#else
        protected virtual void SetUserIdentifiers()
        {
        }
#endif

        protected abstract Task<AuthenticationResult> SendTokenRequestAsync();

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

        protected void NotifyBeforeAccessCache(string resource, string clientId, string uniqueId, string displayableId)
        {
            if (this.tokenCache != null)
            {
                this.tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs
                {
                    TokenCache = this.tokenCache,
                    Resource = resource,
                    ClientId = clientId,
                    UniqueId = uniqueId,
                    DisplayableId = displayableId
                });
            }
        }

        private void ValidateAuthorityType()
        {
            if (!this.SupportADFS && this.Authenticator.AuthorityType == AuthorityType.ADFS)
            {
                Logger.Error(this.CallState, "Invalid authority type '{0}'", this.Authenticator.AuthorityType);
                throw new AdalException(AdalError.InvalidAuthorityType,
                    string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.InvalidAuthorityTypeTemplate, this.Authenticator.Authority));
            }
        }

        private void NotifyAfterAccessCache(string resource, string clientId, string uniqueId, string displayableId)
        {
            if (this.tokenCache != null)
            {
                this.tokenCache.OnAfterAccess(new TokenCacheNotificationArgs
                {
                    TokenCache = this.tokenCache,
                    Resource = resource,
                    ClientId = clientId,
                    UniqueId = uniqueId,
                    DisplayableId = displayableId
                });
            }
        }

        private async Task<AuthenticationResult> RefreshAccessTokenAsync(AuthenticationResult result, string resource, ClientKey clientKey, string audience, CallState callState)
        {
            AuthenticationResult newResult = null;

            if (result != null && resource != null && clientKey != null)
            {
                try
                {
                    newResult = await this.SendOAuth2RequestByRefreshTokenAsync(resource, result.RefreshToken, clientKey, audience, callState);

                    if (newResult.IdToken == null)
                    {
                        // If Id token is not returned by token endpoint when refresh token is redeemed, we should copy tenant and user information from the cached token.
                        newResult.UpdateTenantAndUserInfo(result.TenantId, result.IdToken, result.UserInfo);
                    }
                }
                catch (AdalException ex)
                {
                    AdalServiceException serviceException = ex as AdalServiceException;
                    if (serviceException != null && serviceException.ErrorCode == "invalid_request")
                    {
                        throw new AdalServiceException(
                            AdalError.FailedToRefreshToken,
                            AdalErrorMessage.FailedToRefreshToken + ". " + serviceException.Message,
                            (WebException)serviceException.InnerException);
                    }

                    newResult = null;
                }
            }

            return newResult;
        }

        private async Task<AuthenticationResult> SendOAuth2RequestByRefreshTokenAsync(string resource, string refreshToken, ClientKey clientKey, string audience, CallState callState)
        {
            AuthenticationResult result = await OAuth2Request.SendTokenRequestByRefreshTokenAsync(this.Authenticator.TokenUri, resource, refreshToken, clientKey, audience, callState);
            await this.Authenticator.UpdateAuthorityTenantAsync(result.TenantId, callState);
            return result;
        }
    }
}

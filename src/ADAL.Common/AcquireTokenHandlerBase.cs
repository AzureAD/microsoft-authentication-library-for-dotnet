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
using System.Globalization;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal abstract class AcquireTokenHandlerBase
    {
        protected const string NullResource = "null_resource_as_optional";
        protected readonly static Task CompletedTask = Task.FromResult(false);
        private readonly TokenCache tokenCache;
        protected Exception RefreshException;

        protected AcquireTokenHandlerBase(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, TokenSubjectType subjectType, bool callSync)
        {
            this.Authenticator = authenticator;
            this.CallState = CreateCallState(this.Authenticator.CorrelationId, callSync);
            Logger.Information(this.CallState, 
                string.Format("=== Token Acquisition started:\n\tAuthority: {0}\n\tResource: {1}\n\tClientId: {2}\n\tCacheType: {3}\n\tAuthentication Target: {4}\n\t",
                authenticator.Authority, resource, clientKey.ClientId,
                (tokenCache != null) ? tokenCache.GetType().FullName + string.Format(" ({0} items)", tokenCache.Count) : "null",
                subjectType));

            this.tokenCache = tokenCache;
            this.RefreshException = null;
            if (string.IsNullOrWhiteSpace(resource))
            {
                var ex = new ArgumentNullException("resource");
                Logger.Error(this.CallState, ex);
                throw ex;
            }

            this.Resource = (resource != NullResource) ? resource : null;
            this.ClientKey = clientKey;
            this.TokenSubjectType = subjectType;

            this.LoadFromCache = (tokenCache != null);
            this.StoreToCache = (tokenCache != null);
            this.SupportADFS = false;
        }

        internal CallState CallState { get; set; }

        protected bool SupportADFS { get; set; }

        protected Authenticator Authenticator { get; private set; }

        protected string Resource { get; set; }

        protected ClientKey ClientKey { get; private set; }

        protected TokenSubjectType TokenSubjectType { get; private set; }

        protected string UniqueId { get; set; }

        protected string DisplayableId { get; set; }

        protected UserIdentifierType UserIdentifierType { get; set; }

        protected bool LoadFromCache { get; set; }
        
        protected bool StoreToCache { get; set; }

        public async Task<AuthenticationResult> RunAsync()
        {
            bool notifiedBeforeAccessCache = false;

            try
            {
                await this.PreRunAsync();

                AuthenticationResult result = null;
                if (this.LoadFromCache)
                {
                    this.NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;

                    result = this.tokenCache.LoadFromCache(this.Authenticator.Authority, this.Resource,
                        this.ClientKey.ClientId, this.TokenSubjectType, this.UniqueId, this.DisplayableId,
                        this.CallState);
                    result = this.ValidateResult(result);

                    if (result != null && result.AccessToken == null && result.RefreshToken != null)
                    {
                        result = await this.RefreshAccessTokenAsync(result);
                        if (result != null)
                        {
                            this.tokenCache.StoreToCache(result, this.Authenticator.Authority, this.Resource, this.ClientKey.ClientId, this.TokenSubjectType, this.CallState);
                        }
                    }
                }

                if (result == null)
                {
                    await this.PreTokenRequest();
                    result = await this.SendTokenRequestAsync();
                    this.PostTokenRequest(result);

                    if (this.StoreToCache)
                    {
                        if (!notifiedBeforeAccessCache)
                        {
                            this.NotifyBeforeAccessCache();
                            notifiedBeforeAccessCache = true;
                        }

                        this.tokenCache.StoreToCache(result, this.Authenticator.Authority, this.Resource, this.ClientKey.ClientId, this.TokenSubjectType, this.CallState);
                    }
                }

                await this.PostRunAsync(result);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(this.CallState, ex);
                throw;
            }
            finally
            {
                if (notifiedBeforeAccessCache)
                {
                    this.NotifyAfterAccessCache();
                }
            }
        }

        protected virtual AuthenticationResult ValidateResult(AuthenticationResult result)
        {
            return result;
        }

        public static CallState CreateCallState(Guid correlationId, bool callSync)
        {
            correlationId = (correlationId != Guid.Empty) ? correlationId : Guid.NewGuid();
            return new CallState(correlationId, callSync);
        }

        protected virtual Task PostRunAsync(AuthenticationResult result)
        {
            LogReturnedToken(result);

            return CompletedTask;
        }

        protected virtual async Task PreRunAsync()
        {
            await this.Authenticator.UpdateFromTemplateAsync(this.CallState);
            this.ValidateAuthorityType();
        }

        protected virtual Task PreTokenRequest()
        {
            return CompletedTask;
        }

        protected virtual void PostTokenRequest(AuthenticationResult result)
        {
            this.Authenticator.UpdateTenantId(result.TenantId);
        }

        protected abstract void AddAditionalRequestParameters(RequestParameters requestParameters);

        protected virtual async Task<AuthenticationResult> SendTokenRequestAsync()
        {
            RequestParameters requestParameters = new RequestParameters(this.Resource, this.ClientKey);
            this.AddAditionalRequestParameters(requestParameters);
            return await this.SendHttpMessageAsync(requestParameters);
        }

        protected async Task<AuthenticationResult> SendTokenRequestByRefreshTokenAsync(string refreshToken)
        {
            RequestParameters requestParameters = new RequestParameters(this.Resource, this.ClientKey);
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.RefreshToken;
            requestParameters[OAuthParameter.RefreshToken] = refreshToken;
            return await this.SendHttpMessageAsync(requestParameters);
        }

        private async Task<AuthenticationResult> RefreshAccessTokenAsync(AuthenticationResult result)
        {
            AuthenticationResult newResult = null;

            if (this.Resource != null)
            {
                Logger.Verbose(this.CallState, "Refreshing access token...");

                try
                {
                    newResult = await this.SendTokenRequestByRefreshTokenAsync(result.RefreshToken);
                    this.Authenticator.UpdateTenantId(result.TenantId);

                    if (newResult.IdToken == null)
                    {
                        // If Id token is not returned by token endpoint when refresh token is redeemed, we should copy tenant and user information from the cached token.
                        newResult.UpdateTenantAndUserInfo(result.TenantId, result.IdToken, result.UserInfo);
                    }
                }
                catch (AdalException ex)
                {
                    Logger.Error(this.CallState, ex);
                    AdalServiceException serviceException = ex as AdalServiceException;
                    if (serviceException != null && serviceException.ErrorCode == "invalid_request")
                    {
                        throw new AdalServiceException(
                            AdalError.FailedToRefreshToken,
                            AdalErrorMessage.FailedToRefreshToken + ". " + serviceException.Message,
                            serviceException.ServiceErrorCodes,
                            (WebException)serviceException.InnerException);
                    }

                    this.RefreshException = ex;
                    newResult = null;
                }
            }

            return newResult;
        }

        private async Task<AuthenticationResult> SendHttpMessageAsync(RequestParameters requestParameters)
        {
            string uri = HttpHelper.CheckForExtraQueryParameter(this.Authenticator.TokenUri);

            TokenResponse tokenResponse = await HttpHelper.SendPostRequestAndDeserializeJsonResponseAsync<TokenResponse>(uri, requestParameters, this.CallState);

            AuthenticationResult result = OAuth2Response.ParseTokenResponse(tokenResponse, this.CallState);

            if (result.RefreshToken == null && requestParameters.ContainsKey(OAuthParameter.RefreshToken))
            {
                result.RefreshToken = requestParameters[OAuthParameter.RefreshToken];
                Logger.Verbose(this.CallState, "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
            }

            result.IsMultipleResourceRefreshToken = (!string.IsNullOrWhiteSpace(result.RefreshToken) && !string.IsNullOrWhiteSpace(tokenResponse.Resource));
            return result;
        }

        private void NotifyBeforeAccessCache()
        {
            this.tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs
            {
                TokenCache = this.tokenCache,
                Resource = this.Resource,
                ClientId = this.ClientKey.ClientId,
                UniqueId = this.UniqueId,
                DisplayableId = this.DisplayableId
            });
        }

        private void NotifyAfterAccessCache()
        {
            this.tokenCache.OnAfterAccess(new TokenCacheNotificationArgs
            {
                TokenCache = this.tokenCache,
                Resource = this.Resource,
                ClientId = this.ClientKey.ClientId,
                UniqueId = this.UniqueId,
                DisplayableId = this.DisplayableId
            });
        }

        private void LogReturnedToken(AuthenticationResult result)
        {
            if (result.AccessToken != null)
            {
                string accessTokenHash = PlatformSpecificHelper.CreateSha256Hash(result.AccessToken);
                string refreshTokenHash;
                if (result.RefreshToken != null)
                {
                    refreshTokenHash = PlatformSpecificHelper.CreateSha256Hash(result.RefreshToken);
                }
                else
                {
                    refreshTokenHash = "[No Refresh Token]";
                }

                Logger.Information(this.CallState, "=== Token Acquisition finished successfully. An access token was retuned:\n\tAccess Token Hash: {0}\n\tRefresh Token Hash: {1}\n\tExpiration Time: {2}\n\tUser Hash: {3}\n\t",
                    accessTokenHash, refreshTokenHash, result.ExpiresOn, 
                    result.UserInfo != null ? PlatformSpecificHelper.CreateSha256Hash(result.UserInfo.UniqueId) : "null");
            }
        }

        private void ValidateAuthorityType()
        {
            if (!this.SupportADFS && this.Authenticator.AuthorityType == AuthorityType.ADFS)
            {
                throw new AdalException(AdalError.InvalidAuthorityType,
                    string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.InvalidAuthorityTypeTemplate, this.Authenticator.Authority));
            }
        }
    }
}

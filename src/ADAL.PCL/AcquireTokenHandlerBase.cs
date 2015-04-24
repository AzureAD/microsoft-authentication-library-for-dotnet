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
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal abstract class AcquireTokenHandlerBase
    {
        protected const string NullResource = "null_resource_as_optional";
        protected readonly static Task CompletedTask = Task.FromResult(false);
        private readonly TokenCache tokenCache;

        protected AcquireTokenHandlerBase(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, TokenSubjectType subjectType)
        {
            this.Authenticator = authenticator;
            this.CallState = CreateCallState(this.Authenticator.CorrelationId);
            PlatformPlugin.Logger.Information(this.CallState,
                string.Format("=== Token Acquisition started:\n\tAuthority: {0}\n\tResource: {1}\n\tClientId: {2}\n\tCacheType: {3}\n\tAuthentication Target: {4}\n\t",
                authenticator.Authority, resource, clientKey.ClientId,
                (tokenCache != null) ? tokenCache.GetType().FullName + string.Format(" ({0} items)", tokenCache.Count) : "null",
                subjectType));

            this.tokenCache = tokenCache;

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentNullException("resource");
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

                AuthenticationResultEx resultEx = null;
                if (this.LoadFromCache)
                {
                    this.NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;

                    resultEx = this.tokenCache.LoadFromCache(this.Authenticator.Authority, this.Resource, this.ClientKey.ClientId, this.TokenSubjectType, this.UniqueId, this.DisplayableId, this.CallState);
                    if (resultEx != null && resultEx.Result.AccessToken == null && resultEx.RefreshToken != null)
                    {
                        resultEx = await this.RefreshAccessTokenAsync(resultEx);
                        if (resultEx != null)
                        {
                            this.tokenCache.StoreToCache(resultEx, this.Authenticator.Authority, this.Resource, this.ClientKey.ClientId, this.TokenSubjectType, this.CallState);
                        }
                    }
                }

                if (resultEx == null)
                {
                    await this.PreTokenRequest();
                    resultEx = await this.SendTokenRequestAsync();
                    this.PostTokenRequest(resultEx);

                    if (this.StoreToCache)
                    {
                        if (!notifiedBeforeAccessCache)
                        {
                            this.NotifyBeforeAccessCache();
                            notifiedBeforeAccessCache = true;
                        }

                        this.tokenCache.StoreToCache(resultEx, this.Authenticator.Authority, this.Resource, this.ClientKey.ClientId, this.TokenSubjectType, this.CallState);
                    }
                }

                await this.PostRunAsync(resultEx.Result);
                return resultEx.Result;
            }
            catch (Exception ex)
            {
                PlatformPlugin.Logger.Error(this.CallState, ex);
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

        public static CallState CreateCallState(Guid correlationId)
        {
            correlationId = (correlationId != Guid.Empty) ? correlationId : Guid.NewGuid();
            return new CallState(correlationId);
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

        protected virtual void PostTokenRequest(AuthenticationResultEx result)
        {
            this.Authenticator.UpdateTenantId(result.Result.TenantId);
        }

        protected abstract void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters);

        protected virtual async Task<AuthenticationResultEx> SendTokenRequestAsync()
        {
            var requestParameters = new DictionaryRequestParameters(this.Resource, this.ClientKey);
            this.AddAditionalRequestParameters(requestParameters);
            return await this.SendHttpMessageAsync(requestParameters);
        }

        protected async Task<AuthenticationResultEx> SendTokenRequestByRefreshTokenAsync(string refreshToken)
        {
            var requestParameters = new DictionaryRequestParameters(this.Resource, this.ClientKey);
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.RefreshToken;
            requestParameters[OAuthParameter.RefreshToken] = refreshToken;
            AuthenticationResultEx result = await this.SendHttpMessageAsync(requestParameters);

            if (result.RefreshToken == null)
            {
                result.RefreshToken = refreshToken;
                PlatformPlugin.Logger.Verbose(this.CallState, "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
            }

            return result;
        }

        private async Task<AuthenticationResultEx> RefreshAccessTokenAsync(AuthenticationResultEx result)
        {
            AuthenticationResultEx newResultEx = null;

            if (this.Resource != null)
            {
                PlatformPlugin.Logger.Verbose(this.CallState, "Refreshing access token...");

                try
                {
                    newResultEx = await this.SendTokenRequestByRefreshTokenAsync(result.RefreshToken);
                    this.Authenticator.UpdateTenantId(result.Result.TenantId);

                    if (newResultEx.Result.IdToken == null)
                    {
                        // If Id token is not returned by token endpoint when refresh token is redeemed, we should copy tenant and user information from the cached token.
                        newResultEx.Result.UpdateTenantAndUserInfo(result.Result.TenantId, result.Result.IdToken, result.Result.UserInfo);
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
                            serviceException.ServiceErrorCodes,
                            serviceException.InnerException);
                    }

                    newResultEx = null;
                }
            }

            return newResultEx;
        }

        private async Task<AuthenticationResultEx> SendHttpMessageAsync(IRequestParameters requestParameters)
        {
            var client = new AdalHttpClient(this.Authenticator.TokenUri, this.CallState) { Client = { BodyParameters = requestParameters } };
            TokenResponse tokenResponse = await client.GetResponseAsync<TokenResponse>(ClientMetricsEndpointType.Token);

            return tokenResponse.GetResult();
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
                string accessTokenHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.AccessToken);

                PlatformPlugin.Logger.Information(this.CallState, string.Format("=== Token Acquisition finished successfully. An access token was retuned:\n\tAccess Token Hash: {0}\n\tExpiration Time: {1}\n\tUser Hash: {2}\n\t",
                    accessTokenHash,
                    result.ExpiresOn,                    
                    result.UserInfo != null ? PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.UserInfo.UniqueId) : "null"));
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

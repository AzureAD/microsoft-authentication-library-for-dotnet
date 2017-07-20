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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal abstract class AcquireTokenHandlerBase
    {
        protected const string NullResource = "null_resource_as_optional";
        protected static readonly Task CompletedTask = Task.FromResult(false);
        private readonly TokenCache tokenCache;
        protected readonly IDictionary<string, string> brokerParameters;
        protected CacheQueryData CacheQueryData = new CacheQueryData();

        private AdalHttpClient client = null;

        protected AcquireTokenHandlerBase(RequestData requestData)
        {
            this.Authenticator = requestData.Authenticator;
            this.CallState = CreateCallState(this.Authenticator.CorrelationId);
            PlatformPlugin.Logger.Information(this.CallState,
                string.Format(CultureInfo.CurrentCulture,
                    "=== Token Acquisition started:\n\tAuthority: {0}\n\tResource: {1}\n\tClientId: {2}\n\tCacheType: {3}\n\tAuthentication Target: {4}\n\t",
                    requestData.Authenticator.Authority, requestData.Resource, requestData.ClientKey.ClientId,
                    (tokenCache != null)
                        ? tokenCache.GetType().FullName +
                          string.Format(CultureInfo.CurrentCulture, " ({0} items)", tokenCache.Count)
                        : "null",
                    requestData.SubjectType));

            this.tokenCache = requestData.TokenCache;

            if (string.IsNullOrWhiteSpace(requestData.Resource))
            {
                throw new ArgumentNullException("resource");
            }

            this.Resource = (requestData.Resource != NullResource) ? requestData.Resource : null;
            this.ClientKey = requestData.ClientKey;
            this.TokenSubjectType = requestData.SubjectType;

            this.LoadFromCache = (tokenCache != null);
            this.StoreToCache = (tokenCache != null);
            this.SupportADFS = false;

            this.brokerParameters = new Dictionary<string, string>();
            brokerParameters["authority"] = requestData.Authenticator.Authority;
            brokerParameters["resource"] = requestData.Resource;
            brokerParameters["client_id"] = requestData.ClientKey.ClientId;
            brokerParameters["correlation_id"] = this.CallState.CorrelationId.ToString();
            brokerParameters["client_version"] = AdalIdHelper.GetAdalVersion();
            this.ResultEx = null;

            CacheQueryData.ExtendedLifeTimeEnabled = requestData.ExtendedLifeTimeEnabled;
        }

        internal CallState CallState { get; set; }

        protected bool SupportADFS { get; set; }

        protected Authenticator Authenticator { get; private set; }

        protected string Resource { get; set; }

        protected ClientKey ClientKey { get; private set; }

        protected AuthenticationResultEx ResultEx { get; set; }

        protected TokenSubjectType TokenSubjectType { get; private set; }

        protected string UniqueId { get; set; }

        protected string DisplayableId { get; set; }

        protected UserIdentifierType UserIdentifierType { get; set; }

        protected bool LoadFromCache { get; set; }

        protected bool StoreToCache { get; set; }


        public async Task<AuthenticationResult> RunAsync()
        {
            bool notifiedBeforeAccessCache = false;
            AuthenticationResultEx extendedLifetimeResultEx = null;

            try
            {
                await this.PreRunAsync().ConfigureAwait(false);

                if (this.LoadFromCache)
                {
                    CacheQueryData.Authority = Authenticator.Authority;
                    CacheQueryData.Resource = this.Resource;
                    CacheQueryData.ClientId = this.ClientKey.ClientId;
                    CacheQueryData.SubjectType = this.TokenSubjectType;
                    CacheQueryData.UniqueId = this.UniqueId;
                    CacheQueryData.DisplayableId = this.DisplayableId;

                    this.NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;
                    ResultEx = this.tokenCache.LoadFromCache(CacheQueryData, this.CallState);
                    extendedLifetimeResultEx = ResultEx;

                    if (ResultEx != null && ResultEx.Result != null)
                    {
                        if ((ResultEx.Result.AccessToken == null && ResultEx.RefreshToken != null) ||
                            (ResultEx.Result.ExtendedLifeTimeToken && ResultEx.RefreshToken != null))
                        {
                            ResultEx = await this.RefreshAccessTokenAsync(ResultEx).ConfigureAwait(false);
                            if (ResultEx != null && ResultEx.Exception == null)
                            {
                                this.tokenCache.StoreToCache(ResultEx, this.Authenticator.Authority, this.Resource,
                                    this.ClientKey.ClientId, this.TokenSubjectType, this.CallState);
                            }
                        }
                    }
                }

                if (ResultEx == null || ResultEx.Exception != null)
                {
                    if (PlatformPlugin.BrokerHelper.CanInvokeBroker)
                    {
                        ResultEx = await PlatformPlugin.BrokerHelper.AcquireTokenUsingBroker(brokerParameters).ConfigureAwait(false);
                    }
                    else
                    {
                        await this.PreTokenRequest().ConfigureAwait(false);
                        // check if broker app installation is required for authentication.
                        if (this.BrokerInvocationRequired())
                        {
                            ResultEx = await PlatformPlugin.BrokerHelper.AcquireTokenUsingBroker(brokerParameters).ConfigureAwait(false);
                        }
                        else
                        {
                            ResultEx = await this.SendTokenRequestAsync().ConfigureAwait(false);
                        }
                    }
                    //broker token acquisition failed
                    if (ResultEx != null && ResultEx.Exception != null)
                    {
                        throw ResultEx.Exception;
                    }
                    this.PostTokenRequest(ResultEx);
                    if (this.StoreToCache)
                    {
                        if (!notifiedBeforeAccessCache)
                        {
                            this.NotifyBeforeAccessCache();
                            notifiedBeforeAccessCache = true;
                        }
                        this.tokenCache.StoreToCache(ResultEx, this.Authenticator.Authority, this.Resource,
                            this.ClientKey.ClientId, this.TokenSubjectType, this.CallState);
                    }
                }
                await this.PostRunAsync(ResultEx.Result).ConfigureAwait(false);
                return ResultEx.Result;
            }
            catch (Exception ex)
            {
                PlatformPlugin.Logger.Error(this.CallState, ex);
                if (client != null && client.Resiliency && extendedLifetimeResultEx != null)
                {
                    PlatformPlugin.Logger.Information(this.CallState,
                        "Refreshing AT failed either due to one of these :- Internal Server Error,Gateway Timeout and Service Unavailable.Hence returning back stale AT");
                    return extendedLifetimeResultEx.Result;
                }
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

        protected virtual void UpdateBrokerParameters(IDictionary<string, string> parameters)
        {
        }

        protected virtual bool BrokerInvocationRequired()
        {
            return false;
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
            await this.Authenticator.UpdateFromTemplateAsync(this.CallState).ConfigureAwait(false);
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
            return await this.SendHttpMessageAsync(requestParameters).ConfigureAwait(false);
        }

        protected async Task<AuthenticationResultEx> SendTokenRequestByRefreshTokenAsync(string refreshToken)
        {
            var requestParameters = new DictionaryRequestParameters(this.Resource, this.ClientKey);
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.RefreshToken;
            requestParameters[OAuthParameter.RefreshToken] = refreshToken;
            requestParameters[OAuthParameter.Scope] = OAuthValue.ScopeOpenId;

            AuthenticationResultEx result = await this.SendHttpMessageAsync(requestParameters).ConfigureAwait(false);

            if (result.RefreshToken == null)
            {
                result.RefreshToken = refreshToken;
                PlatformPlugin.Logger.Verbose(this.CallState,
                    "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
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
                    newResultEx = await this.SendTokenRequestByRefreshTokenAsync(result.RefreshToken).ConfigureAwait(false);
                    this.Authenticator.UpdateTenantId(result.Result.TenantId);

                    if (newResultEx.Result.IdToken == null)
                    {
                        // If Id token is not returned by token endpoint when refresh token is redeemed, we should copy tenant and user information from the cached token.
                        newResultEx.Result.UpdateTenantAndUserInfo(result.Result.TenantId, result.Result.IdToken,
                            result.Result.UserInfo);
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
                            serviceException);
                    }
                    newResultEx = new AuthenticationResultEx { Exception = ex };
                }
            }

            return newResultEx;
        }

        private async Task<AuthenticationResultEx> SendHttpMessageAsync(IRequestParameters requestParameters)
        {
            client = new AdalHttpClient(this.Authenticator.TokenUri, this.CallState)
            { Client = { BodyParameters = requestParameters } };
            TokenResponse tokenResponse = await client.GetResponseAsync<TokenResponse>().ConfigureAwait(false);
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

                PlatformPlugin.Logger.Information(this.CallState,
                    string.Format(CultureInfo.CurrentCulture,
                        "=== Token Acquisition finished successfully. An access token was retuned:\n\tAccess Token Hash: {0}\n\tExpiration Time: {1}\n\tUser Hash: {2}\n\t",
                        accessTokenHash,
                        result.ExpiresOn,
                        result.UserInfo != null
                            ? PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.UserInfo.UniqueId)
                            : "null"));
            }
        }

        private void ValidateAuthorityType()
        {
            if (!this.SupportADFS && this.Authenticator.AuthorityType == AuthorityType.ADFS)
            {
                throw new AdalException(AdalError.InvalidAuthorityType,
                    string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.InvalidAuthorityTypeTemplate,
                        this.Authenticator.Authority));
            }
        }
    }
}

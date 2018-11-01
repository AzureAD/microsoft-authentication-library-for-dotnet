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
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.ClientCreds;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Instance;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Flows
{
    internal abstract class AcquireTokenHandlerBase
    {
        protected const string NullResource = "null_resource_as_optional";
        protected static readonly Task CompletedTask = Task.FromResult(false);
        private readonly TokenCache tokenCache;
        internal readonly IDictionary<string, string> brokerParameters;
        protected CacheQueryData CacheQueryData = new CacheQueryData();
        protected readonly BrokerHelper brokerHelper = new BrokerHelper();
        private AdalHttpClient client = null;
        internal readonly RequestContext RequestContext;

        protected AcquireTokenHandlerBase(RequestData requestData)
        {
            this.Authenticator = requestData.Authenticator;
            RequestContext = CreateCallState(null, this.Authenticator.CorrelationId);
            brokerHelper.RequestContext = RequestContext;

            RequestContext.Logger.Info(string.Format(CultureInfo.CurrentCulture,
                "ADAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}' is running...",
                PlatformProxyFactory.GetPlatformProxy().GetProductName(), AdalIdHelper.GetAdalVersion(),
                AssemblyUtils.GetAssemblyFileVersionAttribute(), AssemblyUtils.GetAssemblyInformationalVersion()));

            {
                string msg = string.Format(CultureInfo.CurrentCulture,
                    "=== Token Acquisition started: \n\tCacheType: {0}\n\tAuthentication Target: {1}\n\t",
                    tokenCache != null
                        ? tokenCache.GetType().FullName +
                          string.Format(CultureInfo.CurrentCulture, " ({0} items)", tokenCache.tokenCacheDictionary.Count)
                        : "null",
                    requestData.SubjectType);
                if (InstanceDiscovery.IsWhitelisted(requestData.Authenticator.GetAuthorityHost()))
                {
                    msg += string.Format(CultureInfo.CurrentCulture,
                        ", Authority Host: {0}",
                        requestData.Authenticator.GetAuthorityHost());
                }

                var piiMsg = string.Format(CultureInfo.CurrentCulture,
                    "=== Token Acquisition started:\n\tAuthority: {0}\n\tResource: {1}\n\tClientId: {2}\n\tCacheType: {3}\n\tAuthentication Target: {4}\n\t",
                    requestData.Authenticator.Authority, requestData.Resource, requestData.ClientKey.ClientId,
                    (tokenCache != null)
                        ? tokenCache.GetType().FullName +
                          string.Format(CultureInfo.CurrentCulture, " ({0} items)", tokenCache.tokenCacheDictionary.Count)
                        : "null",
                    requestData.SubjectType);
                RequestContext.Logger.InfoPii(piiMsg, msg);
            }

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
            brokerParameters[BrokerParameter.Authority] = requestData.Authenticator.Authority;
            brokerParameters[BrokerParameter.Resource] = requestData.Resource;
            brokerParameters[BrokerParameter.ClientId] = requestData.ClientKey.ClientId;
            brokerParameters[BrokerParameter.CorrelationId] = RequestContext.Logger.CorrelationId.ToString();
            brokerParameters[BrokerParameter.ClientVersion] = AdalIdHelper.GetAdalVersion();
            this.ResultEx = null;

            CacheQueryData.ExtendedLifeTimeEnabled = requestData.ExtendedLifeTimeEnabled;
        }

        protected bool SupportADFS { get; set; }

        protected Authenticator Authenticator { get; set; }

        protected string Resource { get; set; }

        protected ClientKey ClientKey { get; private set; }

        protected AdalResultWrapper ResultEx { get; set; }

        protected TokenSubjectType TokenSubjectType { get; private set; }

        protected string UniqueId { get; set; }

        protected string DisplayableId { get; set; }

        protected UserIdentifierType UserIdentifierType { get; set; }

        protected bool LoadFromCache { get; set; }

        protected bool StoreToCache { get; set; }

        public async Task<AuthenticationResult> RunAsync()
        {
            bool notifiedBeforeAccessCache = false;
            AdalResultWrapper extendedLifetimeResultEx = null;

            try
            {
                await this.PreRunAsync().ConfigureAwait(false);

                if (this.LoadFromCache)
                {
                    RequestContext.Logger.Verbose("Loading from cache.");

                    CacheQueryData.Authority = Authenticator.Authority;
                    CacheQueryData.Resource = this.Resource;
                    CacheQueryData.ClientId = this.ClientKey.ClientId;
                    CacheQueryData.SubjectType = this.TokenSubjectType;
                    CacheQueryData.UniqueId = this.UniqueId;
                    CacheQueryData.DisplayableId = this.DisplayableId;

                    this.NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;
                    ResultEx = await this.tokenCache.LoadFromCacheAsync(CacheQueryData, RequestContext).ConfigureAwait(false);
                    extendedLifetimeResultEx = ResultEx;

                    if (ResultEx?.Result != null &&
                        ((ResultEx.Result.AccessToken == null && ResultEx.RefreshToken != null) ||
                         (ResultEx.Result.ExtendedLifeTimeToken && ResultEx.RefreshToken != null)))
                    {
                        ResultEx = await this.RefreshAccessTokenAsync(ResultEx).ConfigureAwait(false);
                        if (ResultEx != null && ResultEx.Exception == null)
                        {
                            notifiedBeforeAccessCache = await StoreResultExToCacheAsync(notifiedBeforeAccessCache).ConfigureAwait(false);
                        }
                    }
                }

                if (ResultEx == null || ResultEx.Exception != null)
                {
                    if (brokerHelper.CanInvokeBroker)
                    {
                        ResultEx = await brokerHelper.AcquireTokenUsingBrokerAsync(brokerParameters).ConfigureAwait(false);
                    }
                    else
                    {
                        await this.PreTokenRequestAsync().ConfigureAwait(false);
                        // check if broker app installation is required for authentication.
                        await CheckAndAcquireTokenUsingBrokerAsync().ConfigureAwait(false);
                    }

                    //broker token acquisition failed
                    if (ResultEx != null && ResultEx.Exception != null)
                    {
                        throw ResultEx.Exception;
                    }

                    await this.PostTokenRequestAsync(ResultEx).ConfigureAwait(false);
                    notifiedBeforeAccessCache = await StoreResultExToCacheAsync(notifiedBeforeAccessCache).ConfigureAwait(false);
                }

                await this.PostRunAsync(ResultEx.Result).ConfigureAwait(false);
                return new AuthenticationResult(ResultEx.Result);
            }
            catch (Exception ex)
            {
                RequestContext.Logger.ErrorPii(ex);
                if (client != null && client.Resiliency && extendedLifetimeResultEx != null)
                {
                    RequestContext.Logger.Info("Refreshing access token failed due to one of these reasons:- Internal Server Error, Gateway Timeout and Service Unavailable. " +
                                       "Hence returning back stale access token");

                    return new AuthenticationResult(extendedLifetimeResultEx.Result);
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

        private async Task<bool> StoreResultExToCacheAsync(bool notifiedBeforeAccessCache)
        {
            if (this.StoreToCache)
            {
                if (!notifiedBeforeAccessCache)
                {
                    this.NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;
                }

                await this.tokenCache.StoreToCacheAsync(ResultEx, this.Authenticator.Authority, this.Resource,
                    this.ClientKey.ClientId, this.TokenSubjectType, RequestContext).ConfigureAwait(false);
            }
            return notifiedBeforeAccessCache;
        }

        private async Task CheckAndAcquireTokenUsingBrokerAsync()
        {
            if (this.BrokerInvocationRequired())
            {
                ResultEx = await brokerHelper.AcquireTokenUsingBrokerAsync(brokerParameters).ConfigureAwait(false);
            }
            else
            {
                ResultEx = await this.SendTokenRequestAsync().ConfigureAwait(false);
            }
        }

        protected virtual void UpdateBrokerParameters(IDictionary<string, string> parameters)
        {
        }

        protected virtual bool BrokerInvocationRequired()
        {
            return false;
        }

        public static RequestContext CreateCallState(string clientId, Guid correlationId)
        {
            correlationId = (correlationId != Guid.Empty) ? correlationId : Guid.NewGuid();
            return new RequestContext(clientId, new AdalLogger(correlationId));
        }

        protected virtual Task PostRunAsync(AdalResult result)
        {
            LogReturnedToken(result);
            return CompletedTask;
        }

        protected virtual async Task PreRunAsync()
        {
            await this.Authenticator.UpdateFromTemplateAsync(RequestContext).ConfigureAwait(false);
            this.ValidateAuthorityType();
        }

        protected internal /* internal for test only */ virtual Task PreTokenRequestAsync()
        {
            return CompletedTask;
        }
        
        protected async Task UpdateAuthorityAsync(string updatedAuthority)
        {
            if(!Authenticator.Authority.Equals(updatedAuthority, StringComparison.OrdinalIgnoreCase))
            {
                await Authenticator.UpdateAuthorityAsync(updatedAuthority, RequestContext).ConfigureAwait(false);
                this.ValidateAuthorityType();
            }
        }

        protected virtual async Task PostTokenRequestAsync(AdalResultWrapper resultEx)
        {
            // if broker returned Authority update Authentiator
            if(!string.IsNullOrEmpty(resultEx.Result.Authority))
            {
                await UpdateAuthorityAsync(resultEx.Result.Authority).ConfigureAwait(false);
            }

            this.Authenticator.UpdateTenantId(resultEx.Result.TenantId);

            resultEx.Result.Authority = Authenticator.Authority;
        }

        protected abstract void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters);

        protected internal /* interal for test only */ virtual async Task<AdalResultWrapper> SendTokenRequestAsync()
        {
            var requestParameters = new DictionaryRequestParameters(this.Resource, this.ClientKey)
            {
                { OAuth2Parameter.ClientInfo, "1" }
            };
            this.AddAditionalRequestParameters(requestParameters);
            return await this.SendHttpMessageAsync(requestParameters).ConfigureAwait(false);
        }

        protected async Task<AdalResultWrapper> SendTokenRequestByRefreshTokenAsync(string refreshToken)
        {
            var requestParameters = new DictionaryRequestParameters(this.Resource, this.ClientKey);
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.RefreshToken;
            requestParameters[OAuthParameter.RefreshToken] = refreshToken;
            requestParameters[OAuthParameter.Scope] = OAuthValue.ScopeOpenId;
            requestParameters[OAuth2Parameter.ClientInfo] = "1";

            AdalResultWrapper result = await this.SendHttpMessageAsync(requestParameters).ConfigureAwait(false);

            if (result.RefreshToken == null)
            {
                result.RefreshToken = refreshToken;
                RequestContext.Logger.Verbose("Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
            }

            return result;
        }

        private async Task<AdalResultWrapper> RefreshAccessTokenAsync(AdalResultWrapper result)
        {
            AdalResultWrapper newResultEx = null;

            if (this.Resource != null)
            {
                RequestContext.Logger.Verbose("Refreshing access token...");

                try
                {
                    newResultEx = await this.SendTokenRequestByRefreshTokenAsync(result.RefreshToken)
                        .ConfigureAwait(false);
                    this.Authenticator.UpdateTenantId(result.Result.TenantId);

                    newResultEx.Result.Authority = Authenticator.Authority;

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
                    newResultEx = new AdalResultWrapper {Exception = ex};
                }
            }

            return newResultEx;
        }

        private async Task<AdalResultWrapper> SendHttpMessageAsync(IRequestParameters requestParameters)
        {
            client = new AdalHttpClient(this.Authenticator.TokenUri, RequestContext)
                {Client = {BodyParameters = requestParameters}};
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

        private void LogReturnedToken(AdalResult result)
        {
            if (result.AccessToken != null)
            {
                var accessTokenHash = PlatformProxyFactory
                                      .GetPlatformProxy()
                                      .CryptographyManager
                                      .CreateSha256Hash(result.AccessToken);

                {
                    var msg = string.Format(CultureInfo.CurrentCulture,
                        "=== Token Acquisition finished successfully. An access token was returned: Expiration Time: {0}",
                        result.ExpiresOn);

                    var piiMsg = msg + string.Format(CultureInfo.CurrentCulture, "Access Token Hash: {0}\n\t User id: {1}",
                                     accessTokenHash,
                                     result.UserInfo != null
                                         ? result.UserInfo.UniqueId
                                         : "null");
                    RequestContext.Logger.InfoPii(piiMsg, msg);
                }
            }
        }

        protected void ValidateAuthorityType()
        {
            if (!SupportADFS && Authenticator.AuthorityType == Instance.AuthorityType.ADFS)
            {
                throw new AdalException(AdalError.InvalidAuthorityType,
                    string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.InvalidAuthorityTypeTemplate,
                        Authenticator.Authority));
            }
        }
    }
}

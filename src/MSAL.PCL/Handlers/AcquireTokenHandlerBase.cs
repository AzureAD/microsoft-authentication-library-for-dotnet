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
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Handlers
{
    internal abstract class AcquireTokenHandlerBase
    {
        protected readonly static Task CompletedTask = Task.FromResult(false);
        internal readonly TokenCache tokenCache;
        protected readonly bool restrictToSingleUser;
        protected readonly IDictionary<string, string> brokerParameters;

        protected AcquireTokenHandlerBase(Authenticator authenticator, TokenCache tokenCache, string[] scope, ClientKey clientKey, string policy, bool restrictToSingleUser)
        {
            this.Authenticator = authenticator;
            this.CallState = CreateCallState(this.Authenticator.CorrelationId);
            PlatformPlugin.Logger.Information(this.CallState,
                string.Format("=== Token Acquisition started:\n\tAuthority: {0}\n\tScope: {1}\n\tClientId: {2}\n\tCacheType: {3}",
                authenticator.Authority, scope.CreateSingleStringFromArray(), clientKey.ClientId,
                (tokenCache != null) ? tokenCache.GetType().FullName + string.Format(" ({0} items)", tokenCache.Count) : "null"));

            this.tokenCache = tokenCache;

            if (MsalStringHelper.IsNullOrEmpty(scope))
            {
                throw new ArgumentNullException("scope");
            }
            
            this.Scope = scope.CreateSetFromArray();
            ValidateScopeInput(this.Scope);

            this.ClientKey = clientKey;
            this.Policy = policy;

            this.LoadFromCache = (tokenCache != null);
            this.StoreToCache = (tokenCache != null);
            this.SupportADFS = false;

            this.brokerParameters = new Dictionary<string, string>();
            brokerParameters["authority"] = authenticator.Authority;
            brokerParameters["scope"] = this.Scope.CreateSingleStringFromSet();
            brokerParameters["client_id"] = clientKey.ClientId;
            brokerParameters["correlation_id"] = this.CallState.CorrelationId.ToString();
            brokerParameters["client_version"] = MsalIdHelper.GetMsalVersion();
            this.restrictToSingleUser = restrictToSingleUser;
            
            if (this.tokenCache != null && (restrictToSingleUser && this.tokenCache.GetUniqueIdsFromCache(this.ClientKey.ClientId).Count() > 1))
            {
                throw new ArgumentException(
                    "Cache cannot have entries for more than 1 unique id when RestrictToSingleUser is set to TRUE.");
            }

            //pull any user from the cache as they will all have the same uniqueId
            if (this.tokenCache != null && restrictToSingleUser)
            {
                this.User = this.tokenCache.ReadItems(this.ClientKey.ClientId).First().User;
            }
        }

        internal CallState CallState { get; set; }

        protected bool SupportADFS { get; set; }

        protected Authenticator Authenticator { get; private set; }

        protected HashSet<string> Scope { get; set; }

        protected ClientKey ClientKey { get; private set; }

        protected User User { get; set; }

        protected string Policy { get; set; }

        protected bool LoadFromCache { get; set; }
        
        protected bool StoreToCache { get; set; }

        protected HashSet<string> GetDecoratedScope(HashSet<string> inputScope)
        {
            HashSet<string> set = new HashSet<string>(inputScope.ToArray());
            set.UnionWith(OAuthValue.ReservedScopes.CreateSetFromArray());
            return set;
        }

        protected void ValidateScopeInput(HashSet<string> scopesToValidate)
        {
            //check if scope or additional scope contains client ID.
            if (scopesToValidate.Intersect(OAuthValue.ReservedScopes.CreateSetFromArray()).Any())
            {
                throw new ArgumentException(string.Format("API does not accept '{0}' value as user-provided scopes", OAuthValue.ReservedScopes.CreateSingleStringFromArray()));
            }
        }

        public async Task<AuthenticationResult> RunAsync()
        {
            bool notifiedBeforeAccessCache = false;

            try
            {
                await this.PreRunAsync().ConfigureAwait(false);

                AuthenticationResultEx resultEx = null;
                if (this.LoadFromCache)
                {
                    this.NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;

                    resultEx = this.tokenCache.LoadFromCache(this.Authenticator.Authority, this.Scope, this.ClientKey.ClientId, this.User, this.Policy, this.CallState);
                    if (resultEx != null && resultEx.Result.AccessToken == null && resultEx.RefreshToken != null)
                    {
                        resultEx = await this.RefreshAccessTokenAsync(resultEx).ConfigureAwait(false);
                        if (resultEx != null)
                        {
                            this.tokenCache.StoreToCache(resultEx, this.Authenticator.Authority, this.ClientKey.ClientId, this.Policy, this.restrictToSingleUser, this.CallState);
                        }
                    }
                }

                if (resultEx == null)
                {
                    if (PlatformPlugin.BrokerHelper.CanInvokeBroker)
                    {
                        resultEx = await PlatformPlugin.BrokerHelper.AcquireTokenUsingBroker(brokerParameters).ConfigureAwait(false);
                    }
                    else
                    {
                        await this.PreTokenRequest().ConfigureAwait(false);
                        
                        // check if broker app installation is required for authentication.
                        if (this.BrokerInvocationRequired())
                        {
                            resultEx = await PlatformPlugin.BrokerHelper.AcquireTokenUsingBroker(brokerParameters).ConfigureAwait(false);
                        }
                        else
                        {
                            resultEx = await this.SendTokenRequestAsync().ConfigureAwait(false);
                        }
                    }

                    //broker token acquisition failed
                    if (resultEx != null && resultEx.Exception != null)
                    {
                        throw resultEx.Exception;
                    }

                    this.PostTokenRequest(resultEx);
                    if (this.StoreToCache)
                    {
                        if (!notifiedBeforeAccessCache)
                        {
                            this.NotifyBeforeAccessCache();
                            notifiedBeforeAccessCache = true;
                        }

                        this.tokenCache.StoreToCache(resultEx, this.Authenticator.Authority, this.ClientKey.ClientId, this.Policy, this.restrictToSingleUser, this.CallState);
                    }
                }

                await this.PostRunAsync(resultEx.Result).ConfigureAwait(false);

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

            //add client id, token cache and authority to User object
            if (result.User != null)
            {
                result.User.TokenCache = this.tokenCache;
                result.User.ClientId = this.ClientKey.ClientId;
                result.User.Authority = this.Authenticator.Authority;
            }

            return CompletedTask;
        }

        internal virtual async Task PreRunAsync()
        {
            await this.Authenticator.UpdateFromTemplateAsync(this.CallState).ConfigureAwait(false);
        }

        internal virtual Task PreTokenRequest()
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
            var requestParameters = new DictionaryRequestParameters(this.GetDecoratedScope(this.Scope), this.ClientKey);
            this.AddAditionalRequestParameters(requestParameters);
            return await this.SendHttpMessageAsync(requestParameters).ConfigureAwait(false);
        }

        internal async Task<AuthenticationResultEx> SendTokenRequestByRefreshTokenAsync(string refreshToken)
        {
            var requestParameters = new DictionaryRequestParameters(this.GetDecoratedScope(this.Scope), this.ClientKey);
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.RefreshToken;
            requestParameters[OAuthParameter.RefreshToken] = refreshToken;

            AuthenticationResultEx result = await this.SendHttpMessageAsync(requestParameters).ConfigureAwait(false);

            if (result.RefreshToken == null)
            {
                result.RefreshToken = refreshToken;
                PlatformPlugin.Logger.Verbose(this.CallState, "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
            }

            return result;
        }

        internal async Task<AuthenticationResultEx> RefreshAccessTokenAsync(AuthenticationResultEx result)
        {
            AuthenticationResultEx newResultEx = null;

            if (this.Scope != null)
            {
                PlatformPlugin.Logger.Verbose(this.CallState, "Refreshing access token...");

                try
                {
                    newResultEx = await this.SendTokenRequestByRefreshTokenAsync(result.RefreshToken).ConfigureAwait(false);
                    this.Authenticator.UpdateTenantId(result.Result.TenantId);

                    if (newResultEx.Result.IdToken == null)
                    {
                        // If Id token is not returned by token endpoint when refresh token is redeemed, we should copy tenant and user information from the cached token.
                        newResultEx.Result.UpdateTenantAndUser(result.Result.TenantId, result.Result.IdToken, result.Result.User);
                    }
                }
                catch (MsalException ex)
                {
                    MsalServiceException serviceException = ex as MsalServiceException;
                    if (serviceException != null && serviceException.ErrorCode == "invalid_request")
                    {
                        throw new MsalServiceException(
                            MsalError.FailedToRefreshToken,
                            MsalErrorMessage.FailedToRefreshToken + ". " + serviceException.Message,
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
            string endpoint = this.Authenticator.TokenUri;
            endpoint = AddPolicyParameter(endpoint);

            var client = new MsalHttpClient(endpoint, this.CallState) { Client = { BodyParameters = requestParameters } };
            TokenResponse tokenResponse = await client.GetResponseAsync<TokenResponse>(ClientMetricsEndpointType.Token).ConfigureAwait(false);

            return tokenResponse.GetResult();
        }

        internal void NotifyBeforeAccessCache()
        {
            this.tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs
            {
                TokenCache = this.tokenCache,
                Scope = this.Scope,
                ClientId = this.ClientKey.ClientId
            });
        }

        internal void NotifyAfterAccessCache()
        {
            this.tokenCache.OnAfterAccess(new TokenCacheNotificationArgs
            {
                TokenCache = this.tokenCache,
                Scope = this.Scope,
                ClientId = this.ClientKey.ClientId
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
                    result.User != null ? PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.User.UniqueId) : "null"));
            }
        }

        internal string AddPolicyParameter(string endpoint)
        {
            if (!string.IsNullOrWhiteSpace(this.Policy))
            {
                string delimiter = (endpoint.IndexOf('?') > 0) ? "&" : "?";
                endpoint += string.Concat(delimiter, string.Format("p={0}", this.Policy));
            }

            return endpoint;
        }

        internal User MapIdentifierToUser(string identifier)
        {
            string displayableId = null;
            string uniqueId = null;

            if (!string.IsNullOrEmpty(identifier))
            {
                if (identifier.Contains("@"))
                {
                    displayableId = identifier;
                }
                else
                {
                    uniqueId = identifier;
                }
            }

            User user = new User();
            if (this.tokenCache != null)
            {
                bool notifiedBeforeAccessCache = false;
                try
                {
                    this.NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;

                    AuthenticationResultEx resultEx = this.tokenCache.LoadFromCache(this.Authenticator.Authority,
                        this.Scope,
                        this.ClientKey.ClientId, user,
                        this.Policy, this.CallState);
                }
                finally
                {
                    if (notifiedBeforeAccessCache)
                    {
                        this.NotifyAfterAccessCache();
                    }

                }
            }

            return user;
        }

    }
}

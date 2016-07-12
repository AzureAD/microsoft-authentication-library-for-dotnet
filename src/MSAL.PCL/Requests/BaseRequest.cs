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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Requests
{
    internal abstract class BaseRequest
    {
        protected readonly static Task CompletedTask = Task.FromResult(false);
        internal readonly TokenCache tokenCache;
        protected readonly bool restrictToSingleUser;


        protected BaseRequest(RequestData requestData)
        {
            this.Authenticator = requestData.Authenticator;
            this.CallState = CreateCallState(this.Authenticator.CorrelationId);

            PlatformPlugin.Logger.Information(this.CallState,
                string.Format(CultureInfo.InvariantCulture,"=== Token Acquisition started:\n\tAuthority: {0}\n\tScope: {1}\n\tClientId: {2}\n\tCacheType: {3}",
                Authenticator.Authority, requestData.Scope.AsSingleString(), requestData.ClientKey.ClientId,
                (tokenCache != null) ? tokenCache.GetType().FullName + string.Format(CultureInfo.InvariantCulture," ({0} items)", tokenCache.Count) : "null"));

            this.tokenCache = requestData.TokenCache;
            this.ClientKey = requestData.ClientKey;
            this.Policy = requestData.Policy;
            this.restrictToSingleUser = requestData.RestrictToSingleUser;

            if (MsalStringHelper.IsNullOrEmpty(requestData.Scope))
            {
                throw new ArgumentNullException("scope");
            }
            
            this.Scope = requestData.Scope.CreateSetFromArray();
            ValidateScopeInput(this.Scope);


            this.LoadFromCache = (tokenCache != null);
            this.StoreToCache = (tokenCache != null);
            this.SupportADFS = false;
            
            if (this.tokenCache != null && (restrictToSingleUser && this.tokenCache.GetUniqueIdsFromCache(this.ClientKey.ClientId).Count() > 1))
            {
                throw new ArgumentException(
                    "Cache cannot have entries for more than 1 unique id when RestrictToSingleUser is set to TRUE.");
            }
        }

        internal CallState CallState { get; set; }

        protected bool SupportADFS { get; set; }

        protected Authenticator Authenticator { get; private set; }

        protected HashSet<string> Scope { get; set; }

        protected ClientKey ClientKey { get; private set; }

        protected User User { get; set; }

        protected string Policy { get; set; }

        protected AuthenticationResultEx ResultEx { get; set; }

        protected bool LoadFromCache { get; set; }

        protected bool ForceRefresh { get; set; }

        protected bool StoreToCache { get; set; }

        protected virtual HashSet<string> GetDecoratedScope(HashSet<string> inputScope)
        {
            HashSet<string> set = new HashSet<string>(inputScope.ToArray());
            set.UnionWith(OAuthValue.ReservedScopes.CreateSetFromArray());
            set.Remove(this.ClientKey.ClientId);

            //special case b2c scenarios to not send email and profile as scopes for BUILD 
            if (!string.IsNullOrEmpty(this.Policy))
            {
                set.Remove("email");
                set.Remove("profile");
            }

            return set;
        }

        protected void ValidateScopeInput(HashSet<string> scopesToValidate)
        {
            //check if scope or additional scope contains client ID.
            if (scopesToValidate.Intersect(OAuthValue.ReservedScopes.CreateSetFromArray()).Any())
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,"API does not accept '{0}' value as user-provided scopes", OAuthValue.ReservedScopes.AsSingleString()));
            }

            if (scopesToValidate.Contains(this.ClientKey.ClientId))
            {
                if (scopesToValidate.Count > 1)
                {
                    throw new ArgumentException("Client Id can only be provided as a single scope");
                }
            }
        }

        public async Task<AuthenticationResult> RunAsync()
        {
            bool notifiedBeforeAccessCache = false;

            try
            {
                await this.PreRunAsync().ConfigureAwait(false);

                if (this.LoadFromCache)
                {
                    this.NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;

                    ResultEx = this.tokenCache.LoadFromCache(this.Authenticator.Authority, this.Scope,
                        this.ClientKey.ClientId, this.User, this.Policy, this.CallState);
                    this.ValidateResult();
                    if (ResultEx != null && (ResultEx.Result.Token == null || ForceRefresh) && ResultEx.RefreshToken != null)

                    {
                        ResultEx = await this.RefreshAccessTokenAsync(ResultEx).ConfigureAwait(false);
                        if (ResultEx != null && ResultEx.Exception == null)
                        {
                            this.tokenCache.StoreToCache(ResultEx, this.Authenticator.Authority, this.ClientKey.ClientId, this.Policy, this.restrictToSingleUser, this.CallState);
                        }
                    }
                }

                if (ResultEx == null || ResultEx.Exception!=null)
                {
                    await this.PreTokenRequest().ConfigureAwait(false);    
                    ResultEx = await this.SendTokenRequestAsync().ConfigureAwait(false);

                    if (ResultEx.Exception != null)
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

                        this.tokenCache.StoreToCache(ResultEx, this.Authenticator.Authority, this.ClientKey.ClientId, this.Policy, this.restrictToSingleUser, this.CallState);
                    }
                }

                await this.PostRunAsync(ResultEx.Result).ConfigureAwait(false);

                return ResultEx.Result;
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

        protected virtual void ValidateResult()
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

            //pull any user from the cache as they will all have the same uniqueId
            if (this.User == null && this.tokenCache != null && restrictToSingleUser)
            {
                this.User = this.tokenCache.ReadItems(this.ClientKey.ClientId).First().User;
            }
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
                PlatformPlugin.Logger.Information(this.CallState, "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
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

                    newResultEx = new AuthenticationResultEx { Exception = ex };
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

            AuthenticationResultEx resultEx = tokenResponse.GetResultEx();
            
            if (resultEx.Result.ScopeSet == null || resultEx.Result.ScopeSet.Count == 0)
            {
                resultEx.Result.ScopeSet = this.Scope;
                PlatformPlugin.Logger.Information(this.CallState, "Scope was missing from the token response, so using developer provided scopes in the result");
            }

            return resultEx;
        }

        internal void NotifyBeforeAccessCache()
        {
            this.tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs
            {
                TokenCache = this.tokenCache,
                Scope = this.Scope.ToArray(),
                ClientId = this.ClientKey.ClientId,
                User = this.User,
                Policy = this.Policy
            });
        }

        internal void NotifyAfterAccessCache()
        {
            this.tokenCache.OnAfterAccess(new TokenCacheNotificationArgs
            {
                TokenCache = this.tokenCache,
                Scope = this.Scope.ToArray(),
                ClientId = this.ClientKey.ClientId,
                User = this.User,
                Policy = this.Policy
            });
        }

        private void LogReturnedToken(AuthenticationResult result)
        {
            if (result.Token != null)
            {
                string accessTokenHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.Token);

                PlatformPlugin.Logger.Information(this.CallState, string.Format(CultureInfo.InvariantCulture,"=== Token Acquisition finished successfully. An access token was retuned:\n\tAccess Token Hash: {0}\n\tExpiration Time: {1}\n\tUser Hash: {2}\n\t",
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
                endpoint += string.Concat(delimiter, string.Format(CultureInfo.InvariantCulture,"p={0}", this.Policy));
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

            if (this.tokenCache != null)
            {
                bool notifiedBeforeAccessCache = false;
                try
                {

                    User user = new User()
                    {
                        UniqueId = uniqueId,
                        DisplayableId = displayableId
                    };

                    this.NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;

                    AuthenticationResultEx resultEx = this.tokenCache.LoadFromCache(this.Authenticator.Authority,
                        this.Scope,
                        this.ClientKey.ClientId, user,
                        this.Policy, this.CallState);
                    if (resultEx != null)
                    {
                        return resultEx.Result.User;
                    }
                }
                finally
                {
                    if (notifiedBeforeAccessCache)
                    {
                        this.NotifyAfterAccessCache();
                    }

                }
            }

            return null;
        }

    }
}

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
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal abstract class BaseRequest
    {
        protected static readonly Task CompletedTask = Task.FromResult(false);
        internal readonly AuthenticationRequestParameters AuthenticationRequestParameters;
        internal readonly Authority Authority;
        internal readonly TokenCache TokenCache;
        protected TokenResponse Response;
        protected AccessTokenCacheItem AccessTokenItem;

        internal RequestContext RequestContext { get; set; }

        protected bool SupportADFS { get; set; }

        protected bool LoadFromCache { get; set; }

        protected bool ForceRefresh { get; set; }

        protected bool StoreToCache { get; set; }

        protected BaseRequest(AuthenticationRequestParameters authenticationRequestParameters)
        {
            this.Authority = authenticationRequestParameters.Authority;
            this.RequestContext = authenticationRequestParameters.RequestContext;
            this.TokenCache = authenticationRequestParameters.TokenCache;

            PlatformPlugin.Logger.Information(this.RequestContext,
                string.Format(CultureInfo.InvariantCulture,
                    "=== Token Acquisition started:\n\tAuthority: {0}\n\tScope: {1}\n\tClientId: {2}\n\tCacheType: {3}",
                    Authority.CanonicalAuthority, authenticationRequestParameters.Scope.AsSingleString(),
                    authenticationRequestParameters.ClientId,
                    (TokenCache != null)
                        ? TokenCache.GetType().FullName
                        : null));

            this.AuthenticationRequestParameters = authenticationRequestParameters;

            if (authenticationRequestParameters.Scope == null || authenticationRequestParameters.Scope.Count == 0)
            {
                throw new ArgumentNullException("scope");
            }

            ValidateScopeInput(authenticationRequestParameters.Scope);

            this.LoadFromCache = (TokenCache != null);
            this.StoreToCache = (TokenCache != null);
            this.SupportADFS = true;
        }

        protected virtual SortedSet<string> GetDecoratedScope(SortedSet<string> inputScope)
        {
            SortedSet<string> set = new SortedSet<string>(inputScope.ToArray());
            set.UnionWith(OAuth2Value.ReservedScopes.CreateSetFromArray());
            set.Remove(AuthenticationRequestParameters.ClientId);
            return set;
        }

        protected void ValidateScopeInput(SortedSet<string> scopesToValidate)
        {
            //check if scope or additional scope contains client ID.
            if (scopesToValidate.Intersect(OAuth2Value.ReservedScopes.CreateSetFromArray()).Any())
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "API does not accept '{0}' value as user-provided scopes",
                    OAuth2Value.ReservedScopes.AsSingleString()));
            }

            if (scopesToValidate.Contains(AuthenticationRequestParameters.ClientId))
            {
                if (scopesToValidate.Count > 1)
                {
                    throw new ArgumentException("Client Id can only be provided as a single scope");
                }
            }
        }

        public async Task<AuthenticationResult> RunAsync()
        {
            AuthenticationResult result = null;
            try
            {
                //authority endpoints resolution and validation 
                await this.PreRunAsync().ConfigureAwait(false);
                await this.PreTokenRequest().ConfigureAwait(false);
                await this.SendTokenRequestAsync().ConfigureAwait(false);
                //save to cache if no access token item found
                //this means that no cached item was found
                if (AccessTokenItem == null)
                {
                    AccessTokenItem = SaveTokenResponseToCache();
                }

                result = PostTokenRequest(AccessTokenItem);
                await this.PostRunAsync(result).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                PlatformPlugin.Logger.Error(this.RequestContext, ex);
                throw;
            }
        }

        internal virtual async Task PreRunAsync()
        {
            await this.Authority.ResolveEndpointsAsync(AuthenticationRequestParameters.LoginHint, this.RequestContext).ConfigureAwait(false);
        }

        private AccessTokenCacheItem SaveTokenResponseToCache()
        {
            if (StoreToCache)
            {
                this.TokenCache.SaveAccessAndRefreshToken(AuthenticationRequestParameters, Response);
            }

            return new AccessTokenCacheItem(this.Authority.CanonicalAuthority,
                AuthenticationRequestParameters.ClientId, Response);
        }

        protected virtual bool BrokerInvocationRequired()
        {
            return false;
        }

        protected virtual Task PostRunAsync(AuthenticationResult result)
        {
            LogReturnedToken(result);
            return CompletedTask;
        }

        internal virtual Task PreTokenRequest()
        {
            return CompletedTask;
        }

        protected virtual AuthenticationResult PostTokenRequest(AccessTokenCacheItem item)
        {
            AuthenticationResult result = new AuthenticationResult(item);
            //add client id, token cache and authority to User object
            if (result.User != null)
            {
                result.User.TokenCache = this.TokenCache;
                result.User.ClientId = AuthenticationRequestParameters.ClientId;
                result.User.Authority = this.Authority.CanonicalAuthority;
            }

            return result;
        }

        protected abstract void SetAdditionalRequestParameters(OAuth2Client client);

        protected virtual async Task SendTokenRequestAsync()
        {
            OAuth2Client client = new OAuth2Client();
            client.AddBodyParameter(OAuth2Parameter.ClientId, AuthenticationRequestParameters.ClientId);
            foreach (var entry in AuthenticationRequestParameters.ToParameters())
            {
                client.AddBodyParameter(entry.Key, entry.Value);
            }

            client.AddBodyParameter(OAuth2Parameter.Scope,
                this.GetDecoratedScope(AuthenticationRequestParameters.Scope).AsSingleString());
            this.SetAdditionalRequestParameters(client);
            await this.SendHttpMessageAsync(client).ConfigureAwait(false);
        }

        private async Task SendHttpMessageAsync(OAuth2Client client)
        {
            Response =
                await client.GetToken(new Uri(this.Authority.TokenEndpoint), this.RequestContext).ConfigureAwait(false);

            if (string.IsNullOrEmpty(Response.Scope))
            {
                Response.Scope = AuthenticationRequestParameters.Scope.AsSingleString();
                PlatformPlugin.Logger.Information(this.RequestContext,
                    "Scope was missing from the token response, so using developer provided scopes in the result");
            }
        }

        private void LogReturnedToken(AuthenticationResult result)
        {
            if (result.AccessToken != null)
            {
                string accessTokenHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.AccessToken);

                PlatformPlugin.Logger.Information(this.RequestContext,
                    string.Format(CultureInfo.InvariantCulture,
                        "=== Token Acquisition finished successfully. An access token was retuned:\n\tAccess Token Hash: {0}\n\tExpiration Time: {1}\n\tUser Hash: {2}\n\t",
                        accessTokenHash,
                        result.ExpiresOn,
                        result.User != null
                            ? PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.User.UniqueId)
                            : "null"));
            }
        }
    }
}
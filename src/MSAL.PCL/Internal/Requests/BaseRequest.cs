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
        protected Exception Exception;
        protected TokenResponse Response;

        internal CallState CallState { get; set; }

        protected bool SupportADFS { get; set; }

        protected User User { get; set; }

        protected bool LoadFromCache { get; set; }

        protected bool ForceRefresh { get; set; }

        protected bool StoreToCache { get; set; }

        protected BaseRequest(AuthenticationRequestParameters authenticationRequestParameters)
        {
            this.Authority = authenticationRequestParameters.Authority;
            this.CallState = authenticationRequestParameters.CallState;
            this.TokenCache = authenticationRequestParameters.TokenCache;

            PlatformPlugin.Logger.Information(this.CallState,
                string.Format(CultureInfo.InvariantCulture,
                    "=== Token Acquisition started:\n\tAuthority: {0}\n\tScope: {1}\n\tClientId: {2}\n\tCacheType: {3}",
                    Authority.CanonicalAuthority, authenticationRequestParameters.Scope.AsSingleString(),
                    authenticationRequestParameters.ClientKey.ClientId,
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
            this.SupportADFS = false;
        }

        protected virtual SortedSet<string> GetDecoratedScope(SortedSet<string> inputScope)
        {
            SortedSet<string> set = new SortedSet<string>(inputScope.ToArray());
            set.UnionWith(OAuth2Value.ReservedScopes.CreateSetFromArray());
            set.Remove(AuthenticationRequestParameters.ClientKey.ClientId);

            //special case b2c scenarios to not send email and profile as scopes for BUILD 
            if (!string.IsNullOrEmpty(AuthenticationRequestParameters.Policy))
            {
                set.Remove("email");
                set.Remove("profile");
            }

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

            if (scopesToValidate.Contains(AuthenticationRequestParameters.ClientKey.ClientId))
            {
                if (scopesToValidate.Count > 1)
                {
                    throw new ArgumentException("Client Id can only be provided as a single scope");
                }
            }
        }

        public async Task<AuthenticationResult> RunAsync()
        {
            TokenCacheItem accessTokenItem = null;
            AuthenticationResult result = null;
            try
            {
                await this.PreRunAsync().ConfigureAwait(false);
                if (this.LoadFromCache)
                {
                    //look for access token first because
                    //force refresh is not 
                    if (!ForceRefresh)
                    {
                        accessTokenItem = TokenCache.FindAccessToken(AuthenticationRequestParameters,
                            User);
                    }

                    // no matching access token in the cache
                    if (accessTokenItem == null)
                    {
                        RefreshTokenCacheItem refreshTokenItem =
                            TokenCache.FindRefreshToken(AuthenticationRequestParameters,
                                User);

                        if (refreshTokenItem != null)
                        {
                            await this.RefreshAccessTokenAsync(refreshTokenItem).ConfigureAwait(false);
                            if (Response != null && Exception == null && StoreToCache)
                            {
                                accessTokenItem = SaveTokenResponseToCache();
                            }
                        }
                    }
                }

                //silent request did not succeed
                if (Response == null || Exception != null)
                {
                    await this.PreTokenRequest().ConfigureAwait(false);
                    await this.SendTokenRequestAsync().ConfigureAwait(false);

                    if (Exception != null)
                    {
                        throw Exception;
                    }

                    SaveTokenResponseToCache();
                }

                result = PostTokenRequest(accessTokenItem);
                await this.PostRunAsync(result).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                PlatformPlugin.Logger.Error(this.CallState, ex);
                throw;
            }
        }

        private TokenCacheItem SaveTokenResponseToCache()
        {
            if (StoreToCache)
            {
                this.TokenCache.SaveAccessToken(this.Authority.CanonicalAuthority,
                    AuthenticationRequestParameters.ClientKey.ClientId,
                    AuthenticationRequestParameters.Policy, Response);

                this.TokenCache.SaveRefreshToken(AuthenticationRequestParameters.ClientKey.ClientId,
                    AuthenticationRequestParameters.Policy, Response);
            }

            return new TokenCacheItem(this.Authority.CanonicalAuthority,
                AuthenticationRequestParameters.ClientKey.ClientId,
                AuthenticationRequestParameters.Policy, Response);
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

        internal virtual async Task PreRunAsync()
        {
            await this.Authority.UpdateFromTemplateAsync(this.CallState).ConfigureAwait(false);
        }

        internal virtual Task PreTokenRequest()
        {
            return CompletedTask;
        }

        protected virtual AuthenticationResult PostTokenRequest(TokenCacheItem item)
        {
            AuthenticationResult result = new AuthenticationResult(item);
            //add client id, token cache and authority to User object
            if (result.User != null)
            {
                result.User.TokenCache = this.TokenCache;
                result.User.ClientId = AuthenticationRequestParameters.ClientKey.ClientId;
                result.User.Authority = this.Authority.CanonicalAuthority;
            }

            return result;
        }

        protected abstract void SetAdditionalRequestParameters(OAuth2Client client);

        protected virtual async Task SendTokenRequestAsync()
        {
            OAuth2Client client = new OAuth2Client();
            foreach (var entry in AuthenticationRequestParameters.ClientKey.ToParameters())
            {
                client.AddBodyParameter(entry.Key, entry.Value);
            }

            client.AddBodyParameter(OAuth2Parameter.Scope,
                this.GetDecoratedScope(AuthenticationRequestParameters.Scope).AsSingleString());
            this.SetAdditionalRequestParameters(client);
            await this.SendHttpMessageAsync(client).ConfigureAwait(false);
        }

        internal async Task SendTokenRequestByRefreshTokenAsync(string refreshToken)
        {
            OAuth2Client client = new OAuth2Client();
            foreach (var entry in AuthenticationRequestParameters.ClientKey.ToParameters())
            {
                client.AddBodyParameter(entry.Key, entry.Value);
            }

            client.AddBodyParameter(OAuth2Parameter.Scope,
                this.GetDecoratedScope(AuthenticationRequestParameters.Scope).AsSingleString());
            client.AddBodyParameter(OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken);
            client.AddBodyParameter(OAuth2Parameter.RefreshToken, refreshToken);

            Response = await this.SendHttpMessageAsync(client).ConfigureAwait(false);

            if (Response.RefreshToken == null)
            {
                Response.RefreshToken = refreshToken;
                PlatformPlugin.Logger.Information(this.CallState,
                    "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
            }
        }

        internal async Task RefreshAccessTokenAsync(RefreshTokenCacheItem item)
        {
            if (AuthenticationRequestParameters.Scope != null)
            {
                PlatformPlugin.Logger.Verbose(this.CallState, "Refreshing access token...");

                try
                {
                    await this.SendTokenRequestByRefreshTokenAsync(item.RefreshToken).ConfigureAwait(false);

                    if (Response.IdToken == null)
                    {
                        // If Id token is not returned by token endpoint when refresh token is redeemed, 
                        // we should copy tenant and user information from the cached token.
                        Response.IdToken = item.RawIdToken;
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
                            serviceException.InnerException);
                    }

                    Exception = ex;
                }
            }
        }

        private async Task<TokenResponse> SendHttpMessageAsync(OAuth2Client client)
        {
            if (!string.IsNullOrWhiteSpace(AuthenticationRequestParameters.Policy))
            {
                client.AddQueryParameter("p", AuthenticationRequestParameters.Policy);
            }

            TokenResponse tokenResponse =
                await client.GetToken(new Uri(this.Authority.TokenEndpoint), this.CallState).ConfigureAwait(false);

            if (string.IsNullOrEmpty(tokenResponse.Scope))
            {
                tokenResponse.Scope = AuthenticationRequestParameters.Scope.AsSingleString();
                PlatformPlugin.Logger.Information(this.CallState,
                    "Scope was missing from the token response, so using developer provided scopes in the result");
            }

            return tokenResponse;
        }

        private void LogReturnedToken(AuthenticationResult result)
        {
            if (result.Token != null)
            {
                string accessTokenHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.Token);

                PlatformPlugin.Logger.Information(this.CallState,
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
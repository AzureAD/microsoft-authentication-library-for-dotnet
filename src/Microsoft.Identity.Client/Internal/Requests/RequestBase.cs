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
    internal abstract class RequestBase
    {
        protected static readonly Task CompletedTask = Task.FromResult(false);
        internal readonly AuthenticationRequestParameters AuthenticationRequestParameters;
        internal readonly TokenCache TokenCache;
        protected TokenResponse Response;
        protected AccessTokenCacheItem AccessTokenItem;

        protected bool SupportADFS { get; set; }

        protected bool LoadFromCache { get; set; }

        protected bool ForceRefresh { get; set; }

        protected bool StoreToCache { get; set; }

        protected RequestBase(AuthenticationRequestParameters authenticationRequestParameters)
        {
            TokenCache = authenticationRequestParameters.TokenCache;

            authenticationRequestParameters.RequestContext.Logger.Info(string.Format(CultureInfo.InvariantCulture,
                "=== Token Acquisition ({4}) started:\n\tAuthority: {0}\n\tScope: {1}\n\tClientId: {2}\n\tCache Provided: {3}",
                CryptographyHelper.CreateBase64UrlEncodedSha256Hash(AuthenticationRequestParameters?.Authority
                    ?.CanonicalAuthority), authenticationRequestParameters.Scope.AsSingleString(),
                authenticationRequestParameters.ClientId,
                TokenCache != null, this.GetType().Name));
            authenticationRequestParameters.RequestContext.Logger.InfoPii(string.Format(CultureInfo.InvariantCulture,
                "=== Token Acquisition ({4}) started:\n\tAuthority: {0}\n\tScope: {1}\n\tClientId: {2}\n\tCache Provided: {3}",
                AuthenticationRequestParameters?.Authority?.CanonicalAuthority,
                authenticationRequestParameters.Scope.AsSingleString(),
                authenticationRequestParameters.ClientId,
                TokenCache != null, this.GetType().Name));

            AuthenticationRequestParameters = authenticationRequestParameters;
            if (authenticationRequestParameters.Scope == null || authenticationRequestParameters.Scope.Count == 0)
            {
                throw new ArgumentNullException(nameof(authenticationRequestParameters.Scope));
            }

            ValidateScopeInput(authenticationRequestParameters.Scope);
            LoadFromCache = (TokenCache != null);
            StoreToCache = (TokenCache != null);
            SupportADFS = false;

            AuthenticationRequestParameters.LogState();
            Telemetry.GetInstance().ClientId = AuthenticationRequestParameters.ClientId;
        }

        protected virtual SortedSet<string> GetDecoratedScope(SortedSet<string> inputScope)
        {
            SortedSet<string> set = new SortedSet<string>(inputScope.ToArray());
            set.UnionWith(OAuth2Value.ReservedScopes.CreateSetFromEnumerable());
            return set;
        }

        protected void ValidateScopeInput(SortedSet<string> scopesToValidate)
        {
            //check if scope or additional scope contains client ID.
            if (scopesToValidate.Intersect(OAuth2Value.ReservedScopes.CreateSetFromEnumerable()).Any())
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "API does not accept '{0}' value as user-provided scopes",
                    OAuth2Value.ReservedScopes.AsSingleString()));
            }

            if (scopesToValidate.Contains(AuthenticationRequestParameters.ClientId))
            {
                throw new ArgumentException("API does not accept client id as a user-provided scope");
            }
        }

        public async Task<AuthenticationResult> RunAsync()
        {
            AuthenticationResult result = null;
            //this method is the common entrance for all token requests, so it is a good place to put the generic Telemetry logic here
            AuthenticationRequestParameters.RequestContext.TelemetryRequestId = Telemetry.GetInstance().GenerateNewRequestId();
            try
            {
                //authority endpoints resolution and validation
                await PreTokenRequest().ConfigureAwait(false);
                await SendTokenRequestAsync().ConfigureAwait(false);
                result = PostTokenRequest();
                await PostRunAsync(result).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Error(ex);
                throw;
            }
            finally
            {
                Telemetry.GetInstance().Flush(AuthenticationRequestParameters.RequestContext.TelemetryRequestId);
            }
        }

        private AccessTokenCacheItem SaveTokenResponseToCache()
        {
            // developer passed in user object.
            if (AuthenticationRequestParameters.ClientInfo != null)
            {
                ClientInfo fromServer = ClientInfo.CreateFromJson(Response.ClientInfo);
                if (!fromServer.UniqueIdentifier.Equals(AuthenticationRequestParameters.ClientInfo.UniqueIdentifier) ||
                    !fromServer.UniqueTenantIdentifier.Equals(AuthenticationRequestParameters.ClientInfo
                        .UniqueTenantIdentifier))
                {
                    AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(String.Format(
                        CultureInfo.InvariantCulture,
                        "Returned user identifiers (uid:{0} utid:{1}) does not meatch the sent user identifier (uid:{2} utid:{3})",
                        fromServer.UniqueIdentifier, fromServer.UniqueTenantIdentifier,
                        AuthenticationRequestParameters.ClientInfo.UniqueIdentifier,
                        AuthenticationRequestParameters.ClientInfo.UniqueTenantIdentifier));
                    throw new MsalServiceException("user_mismatch", "Returned user identifier does not match the sent user identifier");
                }
            }

            IdToken idToken = IdToken.Parse(Response.IdToken);
            AuthenticationRequestParameters.TenantUpdatedCanonicalAuthority = Authority.UpdateTenantId(
                AuthenticationRequestParameters.Authority.CanonicalAuthority, idToken?.TenantId);

            if (StoreToCache)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info("Saving Token Response to cache..");
                return TokenCache.SaveAccessAndRefreshToken(AuthenticationRequestParameters, Response);
            }

            return new AccessTokenCacheItem(AuthenticationRequestParameters.TenantUpdatedCanonicalAuthority,
                AuthenticationRequestParameters.ClientId, Response);
        }

        protected virtual Task PostRunAsync(AuthenticationResult result)
        {
            LogReturnedToken(result);
            return CompletedTask;
        }

        internal virtual async Task PreTokenRequest()
        {
            await ResolveAuthorityEndpoints().ConfigureAwait(false);
        }


        internal async Task ResolveAuthorityEndpoints()
        {
            await AuthenticationRequestParameters.Authority
                .ResolveEndpointsAsync(AuthenticationRequestParameters.LoginHint,
                    AuthenticationRequestParameters.RequestContext)
                .ConfigureAwait(false);
        }


        protected virtual AuthenticationResult PostTokenRequest()
        {
            //save to cache if no access token item found
            //this means that no cached item was found
            if (AccessTokenItem == null)
            {
                AccessTokenItem = SaveTokenResponseToCache();
            }

            return new AuthenticationResult(AccessTokenItem);
        }

        protected abstract void SetAdditionalRequestParameters(OAuth2Client client);

        protected virtual async Task SendTokenRequestAsync()
        {
            OAuth2Client client = new OAuth2Client();
            client.AddBodyParameter(OAuth2Parameter.ClientId, AuthenticationRequestParameters.ClientId);
            client.AddBodyParameter(OAuth2Parameter.ClientInfo, "1");
            foreach (var entry in AuthenticationRequestParameters.ToParameters())
            {
                client.AddBodyParameter(entry.Key, entry.Value);
            }

            client.AddBodyParameter(OAuth2Parameter.Scope,
                GetDecoratedScope(AuthenticationRequestParameters.Scope).AsSingleString());
            SetAdditionalRequestParameters(client);
            await SendHttpMessageAsync(client).ConfigureAwait(false);
        }

        private async Task SendHttpMessageAsync(OAuth2Client client)
        {
            UriBuilder builder = new UriBuilder(AuthenticationRequestParameters.Authority.TokenEndpoint);
            builder.AppendQueryParameters("slice=testslice&uid=true");
            Response =
                await client
                    .GetToken(builder.Uri,
                        AuthenticationRequestParameters.RequestContext)
                    .ConfigureAwait(false);

            if (string.IsNullOrEmpty(Response.Scope))
            {
                Response.Scope = AuthenticationRequestParameters.Scope.AsSingleString();
                AuthenticationRequestParameters.RequestContext.Logger.Info(
                    "ScopeSet was missing from the token response, so using developer provided scopes in the result");
            }
        }

        private void LogReturnedToken(AuthenticationResult result)
        {
            if (result.AccessToken != null)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info(string.Format(CultureInfo.InvariantCulture,
                    "=== Token Acquisition finished successfully. An access token was retuned with Expiration Time: {0} ===",
                    result.ExpiresOn));
            }
        }
    }
}
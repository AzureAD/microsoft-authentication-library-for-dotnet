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
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class TokenCacheManager
    {
        public delegate Task<AuthenticationResult> RefreshAccessTokenAsync(AuthenticationResult result, string resource, string clientId, CallState callState);

        // We do not want to return near expiry tokens, this is why we use this hard coded setting to refresh tokens which are close to expiration.
        private const int ExpirationMarginInMinutes = 5;

        private readonly RefreshAccessTokenAsync refreshAccessTokenAsync;

        public TokenCacheManager(string authority, IDictionary<TokenCacheKey, string> tokenCacheStore, RefreshAccessTokenAsync refreshAccessTokenAsync)
        {
            this.Authority = authority;
            this.TokenCacheStore = tokenCacheStore;
            this.refreshAccessTokenAsync = refreshAccessTokenAsync;
        }

        public string Authority { get; set; }

        public IDictionary<TokenCacheKey, string> TokenCacheStore { get; private set; }

        public void StoreToCache(AuthenticationResult result, string resource, string clientId = null)
        {
            if (this.TokenCacheStore == null)
            {
                return;
            }

            string userId = result.UserInfo == null ? null : result.UserInfo.UserId;

            this.RemoveFromCache(resource, clientId, userId);
            TokenCacheKey tokenCacheKey = this.CreateTokenCacheKey(result, resource, clientId);
            this.StoreToCache(tokenCacheKey, result);
            this.UpdateCachedMRRTRefreshTokens(clientId, result);
        }

        public async Task<AuthenticationResult> LoadFromCacheAndRefreshIfNeededAsync(string resource, CallState callState, string clientId = null, string userId = null)
        {
            if (this.TokenCacheStore == null)
            {
                return null;
            }

            AuthenticationResult result = null;

            KeyValuePair<TokenCacheKey, string>? kvp = this.LoadSingleEntryFromCache(resource, clientId, userId);

            if (kvp.HasValue)
            {
                TokenCacheKey cacheKey = kvp.Value.Key;
                string tokenValue = kvp.Value.Value;

                result = TokenCacheEncoding.DecodeCacheValue(tokenValue);
                bool tokenMarginallyExpired = (cacheKey.ExpiresOn <= DateTime.UtcNow + TimeSpan.FromMinutes(ExpirationMarginInMinutes));
                if (cacheKey.Resource != resource && cacheKey.IsMultipleResourceRefreshToken)
                {
                    result.AccessToken = null;
                }
                else if (tokenMarginallyExpired && result.RefreshToken == null)
                {
                    this.TokenCacheStore.Remove(cacheKey);
                    result = null;
                }

                if (result != null && ((result.AccessToken == null || tokenMarginallyExpired) && result.RefreshToken != null))
                {
                    AuthenticationResult refreshedResult = await this.refreshAccessTokenAsync(result, resource, clientId, callState);
                    if (refreshedResult != null)
                    {
                        this.StoreToCache(refreshedResult, resource, clientId);
                        this.UpdateCachedMRRTRefreshTokens(clientId, refreshedResult);
                    }

                    result = refreshedResult;
                }
            }

            if (result != null)
            {
                Logger.Verbose(callState, "A matching token was found in the cache");
            }

            return result;
        }

        private TokenCacheKey CreateTokenCacheKey(AuthenticationResult result, string resource = null, string clientId = null)
        {
            TokenCacheKey tokenCacheKey = new TokenCacheKey(result) { Authority = this.Authority };

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                tokenCacheKey.ClientId = clientId;
            }

            if (!string.IsNullOrWhiteSpace(resource))
            {
                tokenCacheKey.Resource = resource;
            }

            return tokenCacheKey;
        }

        private void UpdateCachedMRRTRefreshTokens(string clientId, AuthenticationResult result)
        {
            if (result != null && !string.IsNullOrWhiteSpace(clientId) && result.UserInfo != null && !string.IsNullOrWhiteSpace(result.UserInfo.UserId))
            {
                List<KeyValuePair<TokenCacheKey, string>> mrrtEntries =
                    this.QueryCache(clientId, result.UserInfo.UserId).Where(p => p.Key.IsMultipleResourceRefreshToken).ToList();

                foreach (KeyValuePair<TokenCacheKey, string> entry in mrrtEntries)
                {
                    AuthenticationResult cachedResult = TokenCacheEncoding.DecodeCacheValue(entry.Value);
                    cachedResult.RefreshToken = result.RefreshToken;
                    this.StoreToCache(entry.Key, cachedResult);
                }
            }
        }

        private void StoreToCache(TokenCacheKey key, AuthenticationResult result)
        {
            if (this.TokenCacheStore == null)
            {
                return;
            }

            this.TokenCacheStore.Remove(key);
            this.TokenCacheStore.Add(key, TokenCacheEncoding.EncodeCacheValue(result));
        }

        private void RemoveFromCache(string resource, string clientId = null, string userId = null)
        {
            if (this.TokenCacheStore == null)
            {
                return;
            }

            IEnumerable<KeyValuePair<TokenCacheKey, string>> cacheValues = this.QueryCache(clientId, userId, resource);

            List<TokenCacheKey> keysToRemove = cacheValues.Select(cacheValue => cacheValue.Key).ToList();

            foreach (TokenCacheKey tokenCacheKey in keysToRemove)
            {
                this.TokenCacheStore.Remove(tokenCacheKey);
            }
        }

        /// <summary>
        /// Queries all values in the cache that meet the passed in values, plus the 
        /// authority value that this AuthorizationContext was created with.  In every case passing
        /// null results in a wildcard evaluation.
        /// </summary>
        private List<KeyValuePair<TokenCacheKey, string>> QueryCache(string clientId, string userId, string resource = null)
        {
            return
                this.TokenCacheStore.Where(
                    p =>
                        p.Key.Authority == this.Authority
                        && (string.IsNullOrWhiteSpace(resource) || (string.Compare(p.Key.Resource, resource, StringComparison.OrdinalIgnoreCase) == 0))
                        && (string.IsNullOrWhiteSpace(clientId) || (string.Compare(p.Key.ClientId, clientId, StringComparison.OrdinalIgnoreCase) == 0))
                        && (string.IsNullOrWhiteSpace(userId) || (string.Compare(p.Key.UserId, userId, StringComparison.OrdinalIgnoreCase) == 0))).ToList();
        }

        private KeyValuePair<TokenCacheKey, string>? LoadSingleEntryFromCache(string resource, string clientId, string userId)
        {
            KeyValuePair<TokenCacheKey, string>? returnValue = null;

            // First identify all potential tokens.
            List<KeyValuePair<TokenCacheKey, string>> cacheValues = this.QueryCache(clientId, userId);

            List<KeyValuePair<TokenCacheKey, string>> resourceSpecificCacheValues =
                cacheValues.Where(p => string.Compare(p.Key.Resource, resource, StringComparison.OrdinalIgnoreCase) == 0).ToList();

            int resourceValuesCount = resourceSpecificCacheValues.Count();
            if (resourceValuesCount == 1)
            {
                returnValue = resourceSpecificCacheValues.First();
            }
            else if (resourceValuesCount == 0)
            {
                // There are no resource specific tokens.  Choose any of the MRRT tokens if there are any.
                List<KeyValuePair<TokenCacheKey, string>> mrrtCachValues =
                    cacheValues.Where(p => p.Key.IsMultipleResourceRefreshToken).ToList();

                if (mrrtCachValues.Any())
                {
                    returnValue = mrrtCachValues.First();
                }
            }
            else
            {
                // There is more than one resource specific token.  It is 
                // ambiguous which one to return so throw.
                throw new AdalException(AdalError.MultipleTokensMatched);
            }

            return returnValue;
        }
    }
}

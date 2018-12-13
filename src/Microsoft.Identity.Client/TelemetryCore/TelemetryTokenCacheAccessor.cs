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
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal class TelemetryTokenCacheAccessor : ITokenCacheAccessor
    {
        private readonly ITokenCacheAccessor _tokenCacheAccessor;

        // TelemetryManager will get set with proper Telemetry internal object when the TokenCache/TokenCacheAccessor
        // is attached to ClientApplicationBase.
        internal ITelemetryManager TelemetryManager { get; set; } = new TelemetryManager(null);

        public TelemetryTokenCacheAccessor(ITokenCacheAccessor tokenCacheAccessor)
        {
            _tokenCacheAccessor = tokenCacheAccessor;
        }

#if iOS
        public void SetKeychainSecurityGroup(string keychainSecurityGroup)
        {
            _tokenCacheAccessor.SetKeychainSecurityGroup(keychainSecurityGroup);
        }
#endif

        // The content of this class has to be placed outside of its base class TokenCacheAccessor,
        // otherwise we would have to modify multiple implementations of TokenCacheAccessor on different platforms.
        public void SaveAccessToken(MsalAccessTokenCacheItem item, RequestContext requestContext)
        {
            using (TelemetryManager.CreateTelemetryHelper(requestContext.TelemetryRequestId, requestContext.ClientId,
                new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.AT }))
            {
                SaveAccessToken(item);
            }
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item, RequestContext requestContext)
        {
            using (TelemetryManager.CreateTelemetryHelper(requestContext.TelemetryRequestId, requestContext.ClientId,
                new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.RT }))
            {
                SaveRefreshToken(item);
            }
        }

        public void SaveIdToken(MsalIdTokenCacheItem item, RequestContext requestContext)
        {
            using (TelemetryManager.CreateTelemetryHelper(requestContext.TelemetryRequestId, requestContext.ClientId,
                new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.ID }))
            {
                SaveIdToken(item);
            }
        }

        public void SaveAccount(MsalAccountCacheItem item, RequestContext requestContext)
        {
            using (TelemetryManager.CreateTelemetryHelper(requestContext.TelemetryRequestId, requestContext.ClientId,
                new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.ACCOUNT }))
            {
                SaveAccount(item);
            }
        }

        public void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey, RequestContext requestContext)
        {
            using (TelemetryManager.CreateTelemetryHelper(requestContext.TelemetryRequestId, requestContext.ClientId,
                new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.AT }))
            {
                DeleteAccessToken(cacheKey);
            }
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey, RequestContext requestContext)
        {
            using (TelemetryManager.CreateTelemetryHelper(requestContext.TelemetryRequestId, requestContext.ClientId,
                new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.RT }))
            {
                DeleteRefreshToken(cacheKey);
            }
        }

        public void DeleteIdToken(MsalIdTokenCacheKey cacheKey, RequestContext requestContext)
        {
            using (TelemetryManager.CreateTelemetryHelper(requestContext.TelemetryRequestId, requestContext.ClientId,
                new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.ID }))
            {
                DeleteIdToken(cacheKey);
            }
        }

        public void DeleteAccount(MsalAccountCacheKey cacheKey, RequestContext requestContext)
        {
            using (TelemetryManager.CreateTelemetryHelper(requestContext.TelemetryRequestId, requestContext.ClientId,
                new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.ACCOUNT }))
            {
                DeleteAccount(cacheKey);
            }
        }

        /// <inheritdoc />
        public int RefreshTokenCount => _tokenCacheAccessor.RefreshTokenCount;

        /// <inheritdoc />
        public int AccessTokenCount => _tokenCacheAccessor.AccessTokenCount;

        /// <inheritdoc />
        public int AccountCount => _tokenCacheAccessor.AccountCount;

        /// <inheritdoc />
        public int IdTokenCount => _tokenCacheAccessor.IdTokenCount;

        /// <inheritdoc />
        public void ClearRefreshTokens()
        {
            _tokenCacheAccessor.ClearRefreshTokens();
        }

        /// <inheritdoc />
        public void ClearAccessTokens()
        {
            _tokenCacheAccessor.ClearAccessTokens();
        }

        /// <inheritdoc />
        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            _tokenCacheAccessor.SaveAccessToken(item);
        }

        /// <inheritdoc />
        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            _tokenCacheAccessor.SaveRefreshToken(item);
        }

        /// <inheritdoc />
        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            _tokenCacheAccessor.SaveIdToken(item);
        }

        /// <inheritdoc />
        public void SaveAccount(MsalAccountCacheItem item)
        {
            _tokenCacheAccessor.SaveAccount(item);
        }

        /// <inheritdoc />
        public string GetAccessToken(MsalAccessTokenCacheKey accessTokenKey)
        {
            return _tokenCacheAccessor.GetAccessToken(accessTokenKey);
        }

        /// <inheritdoc />
        public string GetRefreshToken(MsalRefreshTokenCacheKey refreshTokenKey)
        {
            return _tokenCacheAccessor.GetRefreshToken(refreshTokenKey);
        }

        /// <inheritdoc />
        public string GetIdToken(MsalIdTokenCacheKey idTokenKey)
        {
            return _tokenCacheAccessor.GetIdToken(idTokenKey);
        }

        /// <inheritdoc />
        public string GetAccount(MsalAccountCacheKey accountKey)
        {
            return _tokenCacheAccessor.GetAccount(accountKey);
        }

        /// <inheritdoc />
        public void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey)
        {
            _tokenCacheAccessor.DeleteAccessToken(cacheKey);
        }

        /// <inheritdoc />
        public void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey)
        {
            _tokenCacheAccessor.DeleteRefreshToken(cacheKey);
        }

        /// <inheritdoc />
        public void DeleteIdToken(MsalIdTokenCacheKey cacheKey)
        {
            _tokenCacheAccessor.DeleteIdToken(cacheKey);
        }

        /// <inheritdoc />
        public void DeleteAccount(MsalAccountCacheKey cacheKey)
        {
            _tokenCacheAccessor.DeleteAccount(cacheKey);
        }

        /// <inheritdoc />
        public ICollection<string> GetAllAccessTokensAsString()
        {
            return _tokenCacheAccessor.GetAllAccessTokensAsString();
        }

        /// <inheritdoc />
        public ICollection<string> GetAllRefreshTokensAsString()
        {
            return _tokenCacheAccessor.GetAllRefreshTokensAsString();
        }

        /// <inheritdoc />
        public ICollection<string> GetAllIdTokensAsString()
        {
            return _tokenCacheAccessor.GetAllIdTokensAsString();
        }

        /// <inheritdoc />
        public ICollection<string> GetAllAccountsAsString()
        {
            return _tokenCacheAccessor.GetAllAccountsAsString();
        }

        /// <inheritdoc />
        public void Clear()
        {
            _tokenCacheAccessor.Clear();
        }
    }
}

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
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.OAuth2;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    public sealed class TokenCache
    {
        private const int DefaultExpirationBufferInMinutes = 5;
        internal readonly TokenCacheAccessor TokenCacheAccessor = new TokenCacheAccessor();

        /// <summary>
        /// Notification for certain token cache interactions during token acquisition.
        /// </summary>
        /// <param name="args">Arguments related to the cache item impacted</param>
        public delegate void TokenCacheNotification(TokenCacheNotificationArgs args);
        
        internal readonly object lockObject = new object();
        private volatile bool hasStateChanged;
        private readonly string _clientId;

        public TokenCache(string clientId)
        {
            _clientId = clientId;
        }

        /// <summary>
        /// Notification method called before any library method accesses the cache.
        /// </summary>
        internal TokenCacheNotification BeforeAccess { get; set; }

        /// <summary>
        /// Notification method called before any library method writes to the cache. This notification can be used to reload
        /// the cache state from a row in database and lock that row. That database row can then be unlocked in
        /// <see cref="AfterAccess" /> notification.
        /// </summary>
        internal TokenCacheNotification BeforeWrite { get; set; }

        /// <summary>
        /// Notification method called after any library method accesses the cache.
        /// </summary>
        internal TokenCacheNotification AfterAccess { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating whether cache state has changed. MSAL methods set this flag after any change.
        /// Caller application should reset
        /// the flag after serializing and persisting the state of the cache.
        /// </summary>
        public bool HasStateChanged
        {
            get { return this.hasStateChanged; }

            set { this.hasStateChanged = value; }
        }

        /// <summary>
        /// Clears the cache by deleting all the items. Note that if the cache is the default shared cache, clearing it would
        /// impact all the instances of <see cref="PublicClientApplication" /> which share that cache.
        /// </summary>
        internal void Clear()
        {
            lock (lockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = _clientId,
                    User = null
                };

                OnBeforeAccess(args);
                OnBeforeWrite(args);
                //_tokenCacheAccessor.DeleteAll(_clientId);
                OnAfterAccess(args);
            }
        }

        internal void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            AfterAccess?.Invoke(args);
        }

        internal void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            BeforeAccess?.Invoke(args);
        }

        internal void OnBeforeWrite(TokenCacheNotificationArgs args)
        {
            BeforeWrite?.Invoke(args);
        }

        internal int TokenCount
        {
            get
            {
                lock (lockObject)
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = _clientId
                    };

                    OnBeforeAccess(args);
                    IList<TokenCacheItem> tokenCacheItems = TokenCacheAccessor.GetAllAccessTokens();
                    OnAfterAccess(args);
                    return tokenCacheItems.Count;
                }
            }
        }
        
        internal int RefreshTokenCount
        {
            get
            {
                lock (lockObject)
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = _clientId
                    };

                    OnBeforeAccess(args);
                    IList<RefreshTokenCacheItem> tokenCacheItems = TokenCacheAccessor.GetAllRefreshTokens();
                    OnAfterAccess(args);
                    return tokenCacheItems.Count;
                }
            }
        }

        internal TokenCacheItem SaveAccessToken(string authority, string clientId, string policy, TokenResponse response)
        {
            lock (lockObject)
            {
                // create the access token cache item
                TokenCacheItem tokenCacheItem = new TokenCacheItem(authority, clientId, policy, response);
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = _clientId,
                    User = tokenCacheItem.User
                };

                OnBeforeAccess(args);
                OnBeforeWrite(args);
                TokenCacheAccessor.SaveAccessToken(tokenCacheItem);
                OnAfterAccess(args);

                return tokenCacheItem;
            }
        }

        internal void SaveRefreshToken(string clientId, string policy, TokenResponse response)
        {
            lock (lockObject)
            {
                // if server returns the refresh token back, save it in the cache.
                if (response.RefreshToken != null)
                {
                    // create the refresh token cache item
                    RefreshTokenCacheItem refreshTokenCacheItem = new RefreshTokenCacheItem(null, clientId, policy,
                        response);
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = _clientId,
                        User = refreshTokenCacheItem.User
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);
                    TokenCacheAccessor.SaveRefreshToken(refreshTokenCacheItem);
                    OnAfterAccess(args);
                }
            }
        }

        internal TokenCacheItem FindAccessToken(AuthenticationRequestParameters requestParam)
        {
            lock (lockObject)
            {
                TokenCacheKey key = new TokenCacheKey(requestParam.Authority.CanonicalAuthority,
                    requestParam.Scope, _clientId, requestParam.User, requestParam.Policy);
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = _clientId,
                    User = requestParam.User
                };

                OnBeforeAccess(args);
                IList<TokenCacheItem> tokenCacheItems = TokenCacheAccessor.GetTokens(key);

                OnAfterAccess(args);

                if (tokenCacheItems.Count == 0)
                {
                    // TODO: log access token not found
                    return null;
                }

                // TODO: If user is not provided for silent request, and there is only one item found in the cache. Should we return it?
                if (tokenCacheItems.Count > 1)
                {
                    // TODO: log there are multiple access tokens found, don't know which one to use.
                    return null;
                }

                // Access token lookup needs to be a strict match. In the JSON response from token endpoint, server only returns the scope
                // the developer requires the token for. We store the token separately for considerations i.e. MFA.
                TokenCacheItem tokenCacheItem = tokenCacheItems[0];

                if (requestParam.UserAssertion != null &&
                    !tokenCacheItem.UserAssertionHash.Equals(requestParam.UserAssertion.AssertionHash))
                {
                    return null;
                }

                if (tokenCacheItem.ExpiresOn > DateTime.UtcNow + TimeSpan.FromMinutes(DefaultExpirationBufferInMinutes))
                {
                    return tokenCacheItem;
                }

                //TODO: log the access token found is expired.
                return null;
            }
        }

        internal RefreshTokenCacheItem FindRefreshToken(AuthenticationRequestParameters requestParam)
        {
            lock (lockObject)
            {
                TokenCacheKey key = new TokenCacheKey(null, null, requestParam.ClientKey.ClientId, null, null,
                    requestParam.User?.HomeObjectId,
                    requestParam.Policy);
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = _clientId,
                    User = requestParam.User
                };

                OnBeforeAccess(args);
                IList<RefreshTokenCacheItem> refreshTokenCacheItems = TokenCacheAccessor.GetRefreshTokens(key);
                OnAfterAccess(args);
                if (refreshTokenCacheItems.Count == 0)
                {
                    // TODO: no RT returned
                    return null;
                }

                // User info already provided, if there are multiple items found will throw since we don't what
                // is the one we should use.
                if (refreshTokenCacheItems.Count > 1)
                {
                    throw new MsalException(MsalError.MultipleTokensMatched);
                }

                return refreshTokenCacheItems[0];
            }
        }

        internal void DeleteRefreshToken(RefreshTokenCacheItem rtItem)
        {
            lock (lockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = _clientId,
                    User = rtItem.User
                };

                OnBeforeAccess(args);
                OnBeforeWrite(args);
                TokenCacheAccessor.DeleteRefreshToken(rtItem);
                OnAfterAccess(args);
            }
        }

        internal ICollection<User> GetUsers(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException("empty or null clientId");
            }

            lock (lockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = _clientId,
                    User = null
                };

                OnBeforeAccess(args);
                IList<RefreshTokenCacheItem> allRefreshTokens =
                    TokenCacheAccessor.GetAllRefreshTokensForGivenClientId(clientId);
                OnAfterAccess(args);

                IDictionary<string, User> allUsers = new Dictionary<string, User>();
                foreach (RefreshTokenCacheItem item in allRefreshTokens)
                {
                    User user = new User(item.User);
                    user.ClientId = item.ClientId;
                    user.TokenCache = this;
                    allUsers[item.HomeObjectId] = user;
                }

                return allUsers.Values;
            }
        }

        internal ICollection<RefreshTokenCacheItem> GetAllRefreshTokens()
        {
            lock (lockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = _clientId,
                    User = null
                };

                OnBeforeAccess(args);
                IList<RefreshTokenCacheItem> allRefreshTokens =
                    TokenCacheAccessor.GetAllRefreshTokens();
                OnAfterAccess(args);

                return new ReadOnlyCollection<RefreshTokenCacheItem>(allRefreshTokens);
            }
        }

        internal ICollection<TokenCacheItem> GetAllTokens()
        {
            lock (lockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = _clientId,
                    User = null
                };

                OnBeforeAccess(args);
                IList<TokenCacheItem> allTokens =
                    TokenCacheAccessor.GetAllAccessTokens();
                OnAfterAccess(args);

                return new ReadOnlyCollection<TokenCacheItem>(allTokens);
            }
        }


        internal void SignOut(User user)
        {
            lock (lockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = _clientId,
                    User = null
                };

                OnBeforeAccess(args);
                OnBeforeWrite(args);
                IList<RefreshTokenCacheItem> allRefreshTokens =
                    TokenCacheAccessor.GetAllRefreshTokensForGivenClientId(user.ClientId)
                        .Where(item => item.HomeObjectId.Equals(user.HomeObjectId))
                        .ToList();
                foreach (var rtItem in allRefreshTokens)
                {
                    TokenCacheAccessor.DeleteRefreshToken(rtItem);
                }

                IList<TokenCacheItem> allAccessTokens =
                    TokenCacheAccessor.GetAllAccessTokens()
                        .Where(
                            item => item.ClientId.Equals(user.ClientId) && item.HomeObjectId.Equals(user.HomeObjectId))
                        .ToList();

                foreach (var atItem in allAccessTokens)
                {
                    TokenCacheAccessor.DeleteToken(atItem);
                }

                OnAfterAccess(args);
                
            }
        }
    }
}
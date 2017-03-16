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

using Android.App;
using Android.Content;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Client
{
    internal class TokenCacheAccessor
    {
        private const string AccessTokenSharedPreferenceName = "com.microsoft.identity.client.token";
        private const string RefreshTokenSharedPreferenceName = "com.microsoft.identity.client.refreshToken";
        private readonly ISharedPreferences _accessTokenSharedPreference;
        private readonly ISharedPreferences _refreshTokenSharedPreference;
        private RequestContext _requestContext;

        public TokenCacheAccessor()
        {
            _accessTokenSharedPreference = Application.Context.GetSharedPreferences(AccessTokenSharedPreferenceName,
                    FileCreationMode.Private);
            _refreshTokenSharedPreference = Application.Context.GetSharedPreferences(RefreshTokenSharedPreferenceName,
                    FileCreationMode.Private);

            if (_accessTokenSharedPreference == null || _refreshTokenSharedPreference == null)
            {
                throw new MsalException("Fail to create SharedPreference");
            }
        }

        public TokenCacheAccessor(RequestContext requestContext) : this()
        {
            _requestContext = requestContext;
        }

        public void SaveAccessToken(AccessTokenCacheItem accessTokenItem)
        {
            ISharedPreferencesEditor editor = _accessTokenSharedPreference.Edit();
            editor.PutString(accessTokenItem.GetTokenCacheKey().ToString(), JsonHelper.SerializeToJson(accessTokenItem));
            editor.Apply();
        }

        public void SaveRefreshToken(RefreshTokenCacheItem refreshTokenItem)
        {
            ISharedPreferencesEditor editor = _accessTokenSharedPreference.Edit();
            editor.PutString(refreshTokenItem.GetTokenCacheKey().ToString(), JsonHelper.SerializeToJson(refreshTokenItem));
            editor.Apply();
        }

        public ICollection<RefreshTokenCacheItem> GetRefreshTokens(TokenCacheKey tokenCacheKey)
        {
            ICollection<string> allRefreshTokens = this.GetAllRefreshTokensAsString();
            IList<RefreshTokenCacheItem> matchedRefreshTokens = new List<RefreshTokenCacheItem>();
            foreach (string refreshTokenValue in allRefreshTokens)
            {
                RefreshTokenCacheItem refreshTokenCacheItem =
                    JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(refreshTokenValue);

                if (tokenCacheKey.Equals(refreshTokenCacheItem.GetTokenCacheKey()))
                {
                    matchedRefreshTokens.Add(refreshTokenCacheItem);
                }
            }

            return matchedRefreshTokens;
        }

        public void DeleteAccessToken(AccessTokenCacheItem accessToken‪Item)
        {
            Delete(accessToken‪Item.GetTokenCacheKey().ToString(), _refreshTokenSharedPreference.Edit());
        }

        public void DeleteRefreshToken(RefreshTokenCacheItem refreshToken‪Item)
        {
            Delete(refreshToken‪Item.GetTokenCacheKey().ToString(), _refreshTokenSharedPreference.Edit());
        }

        private void Delete(string key, ISharedPreferencesEditor editor)
        {
            editor.Remove(key);
            editor.Apply();
        }

        public ICollection<string> GetAllAccessTokensAsString()
        {
            return _accessTokenSharedPreference.All.Values as ICollection<string>;
        }

        public ICollection<AccessTokenCacheItem> GetAllAccessTokens(string clientId)
        {
            ICollection<string> allTokensAsString = this.GetAllAccessTokensAsString();
            IList<AccessTokenCacheItem> returnList = new List<AccessTokenCacheItem>();
            foreach (var token in allTokensAsString)
            {
                returnList.Add(JsonHelper.DeserializeFromJson<AccessTokenCacheItem>(token));
            }

            return returnList.Where(t => t.ClientId.Equals(clientId)).ToList();
        }

        public ICollection<string> GetAllRefreshTokensAsString()
        {
            return _refreshTokenSharedPreference.All.Values as ICollection<string>;
        }

        public ICollection<RefreshTokenCacheItem> GetAllRefreshTokens(string clientId)
        {
            ICollection<string> allTokensAsString = GetAllRefreshTokensAsString();
            IList<RefreshTokenCacheItem> returnList = new List<RefreshTokenCacheItem>();
            foreach (var token in allTokensAsString)
            {
                returnList.Add(JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(token));
            }

            return returnList.Where(t => t.ClientId.Equals(clientId)).ToList();
        }
    }
}

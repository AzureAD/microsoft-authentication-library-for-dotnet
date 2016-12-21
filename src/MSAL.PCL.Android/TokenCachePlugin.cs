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

using System.Collections.Generic;
using Android.App;
using Android.Content;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.Interfaces;

namespace Microsoft.Identity.Client
{
    internal class TokenCachePlugin : ITokenCachePlugin
    {
        private const string AccessTokenSharedPreferenceName = "com.microsoft.identity.client.token";
        private const string RefreshTokenSharedPreferenceName = "com.microsoft.identity.client.refreshToken";
        private readonly ISharedPreferences _accessTokenSharedPreference;
        private readonly ISharedPreferences _refreshTokenSharedPreference;

        public TokenCachePlugin()
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

        public ICollection<string> AllAccessAndIdTokens()
        {
            return _accessTokenSharedPreference.All.Values as ICollection<string>;
        }

        public ICollection<string> AllRefreshTokens()
        {
            return _refreshTokenSharedPreference.All.Values as ICollection<string>;
        }

        public void SaveToken(TokenCacheItem tokenItem)
        {
            TokenCacheKey key = TokenCacheKey.ExtractKeyForAT(tokenItem);
            ISharedPreferencesEditor editor = _accessTokenSharedPreference.Edit();
            editor.PutString(key.ToString(), JsonHelper.SerializeToJson(tokenItem));
            editor.Apply();
        }

        public void SaveRefreshToken(RefreshTokenCacheItem refreshTokenItem)
        {
            TokenCacheKey key = TokenCacheKey.ExtractKeyForRT(refreshTokenItem);
            ISharedPreferencesEditor editor = _accessTokenSharedPreference.Edit();
            editor.PutString(key.ToString(), JsonHelper.SerializeToJson(refreshTokenItem));
            editor.Apply();
        }

        public void DeleteToken(TokenCacheKey key)
        {
            Delete(key.ToString(), _accessTokenSharedPreference.Edit());
        }

        public void DeleteRefreshToken(TokenCacheKey key)
        {
            Delete(key.ToString(), _refreshTokenSharedPreference.Edit());
        }

        public void DeleteAll(string clientId)
        {
            throw new System.NotImplementedException();
        }

        private void Delete(string key, ISharedPreferencesEditor editor)
        {
            editor.Remove(key);
            editor.Apply();
        }

        public void BeforeAccess(TokenCacheNotificationArgs args)
        {
        }

        public void AfterAccess(TokenCacheNotificationArgs args)
        {
        }
    }
}
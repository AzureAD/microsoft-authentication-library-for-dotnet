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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Core.Cache;
using System.Collections.ObjectModel;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Core
{
    internal class TokenCacheAccessor : ITokenCacheAccessor
    {
        private const string AccessTokenSharedPreferenceName = "com.microsoft.identity.client.accessToken";
        private const string RefreshTokenSharedPreferenceName = "com.microsoft.identity.client.refreshToken";
        private const string IdTokenSharedPreferenceName = "com.microsoft.identity.client.idToken";
        private const string AccountSharedPreferenceName = "com.microsoft.identity.client.Account";

        private readonly ISharedPreferences _accessTokenSharedPreference;
        private readonly ISharedPreferences _refreshTokenSharedPreference;
        private readonly ISharedPreferences _idTokenSharedPreference;
        private readonly ISharedPreferences _accountSharedPreference;

        private RequestContext _requestContext;

        public TokenCacheAccessor()
        {
            _accessTokenSharedPreference = Application.Context.GetSharedPreferences(AccessTokenSharedPreferenceName,
                    FileCreationMode.Private);
            _refreshTokenSharedPreference = Application.Context.GetSharedPreferences(RefreshTokenSharedPreferenceName,
                    FileCreationMode.Private);
            _idTokenSharedPreference = Application.Context.GetSharedPreferences(IdTokenSharedPreferenceName,
                    FileCreationMode.Private);
            _accountSharedPreference = Application.Context.GetSharedPreferences(AccountSharedPreferenceName,
                FileCreationMode.Private);

            if (_accessTokenSharedPreference == null || _refreshTokenSharedPreference == null
                || _idTokenSharedPreference == null || _accountSharedPreference == null)
            {
                throw new MsalException("Fail to create SharedPreference");
            }
        }

        public TokenCacheAccessor(RequestContext requestContext) : this()
        {
            _requestContext = requestContext;
        }

        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            ISharedPreferencesEditor editor = _accessTokenSharedPreference.Edit();
            editor.PutString(item.GetKey().ToString(), JsonHelper.SerializeToJson(item));
            editor.Apply();
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            ISharedPreferencesEditor editor = _refreshTokenSharedPreference.Edit();
            editor.PutString(item.GetKey().ToString(), JsonHelper.SerializeToJson(item));
            editor.Apply();
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            ISharedPreferencesEditor editor = _idTokenSharedPreference.Edit();
            editor.PutString(item.GetKey().ToString(), JsonHelper.SerializeToJson(item));
            editor.Apply();
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            ISharedPreferencesEditor editor = _accountSharedPreference.Edit();
            editor.PutString(item.GetKey().ToString(), JsonHelper.SerializeToJson(item));
            editor.Apply();
        }

        public void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey)
        {
            Delete(cacheKey.ToString(), _accessTokenSharedPreference.Edit());
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey)
        {
            Delete(cacheKey.ToString(), _refreshTokenSharedPreference.Edit());
        }

        public void DeleteIdToken(MsalIdTokenCacheKey cacheKey)
        {
            Delete(cacheKey.ToString(), _idTokenSharedPreference.Edit());
        }

        public void DeleteAccount(MsalAccountCacheKey cacheKey)
        {
            Delete(cacheKey.ToString(), _accountSharedPreference.Edit());
        }

        private void Delete(string key, ISharedPreferencesEditor editor)
        {
            editor.Remove(key);
            editor.Apply();
        }

        private void DeleteAll(ISharedPreferences sharedPreferences)
        {
            var editor = sharedPreferences.Edit();

            editor.Clear();
            editor.Apply();
        }

        public ICollection<string> GetAllAccessTokensAsString()
        {
            return _accessTokenSharedPreference.All.Values.Cast<string>().ToList();
        }

        public ICollection<string> GetAllRefreshTokensAsString()
        {
            return _refreshTokenSharedPreference.All.Values.Cast<string>().ToList();
        }

        public ICollection<string> GetAllIdTokensAsString()
        {
            return _idTokenSharedPreference.All.Values.Cast<string>().ToList();
        }

        public ICollection<string> GetAllAccountsAsString()
        {
            return _accountSharedPreference.All.Values.Cast<string>().ToList();
        }
        /*
        public ICollection<string> GetAllAccessTokenKeys()
        {
            return _accessTokenSharedPreference.All.Keys.ToList();
        }

        public ICollection<string> GetAllRefreshTokenKeys()
        {
            return _refreshTokenSharedPreference.All.Keys.ToList();
        }

        public ICollection<string> GetAllIdTokenKeys()
        {
            return _idTokenSharedPreference.All.Keys.ToList();
        }

        public ICollection<string> GetAllAccountKeys()
        {
            return _accountSharedPreference.All.Keys.ToList();
        }
        */
        public void Clear()
        {
            DeleteAll(_accessTokenSharedPreference);
            DeleteAll(_refreshTokenSharedPreference);
            DeleteAll(_idTokenSharedPreference);
            DeleteAll(_accessTokenSharedPreference);
        }

        public string GetAccessToken(MsalAccessTokenCacheKey accessTokenKey)
        {
            return _accessTokenSharedPreference.GetString(accessTokenKey.ToString(), null);
        }

        public string GetRefreshToken(MsalRefreshTokenCacheKey refreshTokenKey)
        {
            return _refreshTokenSharedPreference.GetString(refreshTokenKey.ToString(), null);
        }

        public string GetIdToken(MsalIdTokenCacheKey idTokenKey)
        {
            return _idTokenSharedPreference.GetString(idTokenKey.ToString(), null);
        }

        public string GetAccount(MsalAccountCacheKey accountKey)
        {
            return _accountSharedPreference.GetString(accountKey.ToString(), null);
        }

        public void SetSecurityGroup(string securityGroup)
        {
            throw new System.NotImplementedException();
        }
    }
}

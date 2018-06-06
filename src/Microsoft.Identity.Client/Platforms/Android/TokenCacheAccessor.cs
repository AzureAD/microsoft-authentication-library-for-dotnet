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
using System;

namespace Microsoft.Identity.Client
{
    internal class TokenCacheAccessor : ITokenCacheAccessor
    {
        private const string AccessTokenSharedPreferenceName = "com.microsoft.identity.client.accessToken";
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

        public void SaveAccessToken(string cacheKey, string item, RequestContext RequestContext)
        {
            _requestContext = RequestContext;
            ISharedPreferencesEditor editor = _accessTokenSharedPreference.Edit();
            editor.PutString(cacheKey, item);
            editor.Apply();
        }

        public void SaveRefreshToken(string cacheKey, string item, RequestContext RequestContext)
        {
            _requestContext = RequestContext;
            ISharedPreferencesEditor editor = _refreshTokenSharedPreference.Edit();
            editor.PutString(cacheKey, item);
            editor.Apply();
        }

        public string GetRefreshToken(string refreshTokenKey, RequestContext RequestContext)
        {
            _requestContext = RequestContext;
            return _refreshTokenSharedPreference.GetString(refreshTokenKey, null);
        }

        public void DeleteAccessToken(string cacheKey, RequestContext RequestContext)
        {
            _requestContext = RequestContext;
            Delete(cacheKey, _accessTokenSharedPreference.Edit());
        }

        public void DeleteRefreshToken(string cacheKey, RequestContext RequestContext)
        {
            _requestContext = RequestContext;
            Delete(cacheKey, _refreshTokenSharedPreference.Edit());
        }

        private void Delete(string key, ISharedPreferencesEditor editor)
        {
            editor.Remove(key);
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

        public ICollection<string> GetAllAccessTokenKeys()
        {
            return _accessTokenSharedPreference.All.Keys.ToList();
        }

        public ICollection<string> GetAllRefreshTokenKeys()
        {
            return _refreshTokenSharedPreference.All.Keys.ToList();
        }

        public void Clear()
        {
            foreach (var key in GetAllAccessTokenKeys())
            {
                DeleteAccessToken(key, _requestContext);
            }

            foreach (var key in GetAllRefreshTokenKeys())
            {
                DeleteRefreshToken(key, _requestContext);
            }
        }
    }
}

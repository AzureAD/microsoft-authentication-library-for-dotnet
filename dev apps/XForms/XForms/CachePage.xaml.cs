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

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal.Cache;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CachePage : ContentPage
    {
        public CachePage()
        {
            InitializeComponent();
        }

        private void RefreshCacheView()
        {
            var tokenCache = App.MsalPublicClient.UserTokenCache;
            accessTokenCacheItems.ItemsSource = tokenCache.GetAllAccessTokensForClient(new RequestContext(Guid.Empty));

            refreshTokenCacheItems.ItemsSource = tokenCache.GetAllRefreshTokensForClient(new RequestContext(Guid.Empty));
        }

        protected override void OnAppearing()
        {
            RefreshCacheView();
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            var tokenCache = App.MsalPublicClient.UserTokenCache;
            var users = tokenCache.GetUsers(new Uri(App.Authority).Host, new RequestContext(Guid.Empty));
            foreach (var user in users)
            {
                tokenCache.Remove(user, new RequestContext(Guid.Empty));
            }

            RefreshCacheView();
        }

        private static long GetCurrentTimestamp()
        {
            return (long) (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public void OnExpire(object sender, EventArgs e)
        {
            var mi = ((MenuItem) sender);
            var accessTokenCacheItem = (AccessTokenCacheItem) mi.CommandParameter;
            var tokenCache = App.MsalPublicClient.UserTokenCache;

            // set access token as expired
            accessTokenCacheItem.ExpiresOnUnixTimestamp = GetCurrentTimestamp();

            // update entry in the cache
            tokenCache.AddAccessTokenCacheItem(accessTokenCacheItem);

            RefreshCacheView();
        }

        public void OnAtDelete(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var accessTokenCacheItem = (AccessTokenCacheItem)mi.CommandParameter;

            var tokenCache = App.MsalPublicClient.UserTokenCache;
            tokenCache.DeleteAccessToken(accessTokenCacheItem);

            RefreshCacheView();
        }

        public void OnInvalidate(object sender, EventArgs e)
        {
            var mi = ((MenuItem) sender);
            var refreshTokenCacheItem = (RefreshTokenCacheItem) mi.CommandParameter;
            var tokenCache = App.MsalPublicClient.UserTokenCache;

            // invalidate refresh token
            refreshTokenCacheItem.RefreshToken = "InvalidValue";

            // update entry in the cache
            tokenCache.AddRefreshTokenCacheItem(refreshTokenCacheItem);

            RefreshCacheView();
        }
    }
}


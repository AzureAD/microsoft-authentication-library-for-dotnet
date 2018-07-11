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
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
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

            var requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));

            IDictionary<string, MsalAccessTokenCacheItem> accessTokens = new Dictionary<string, MsalAccessTokenCacheItem>();
            foreach (var accessItemStr in tokenCache.GetAllAccessTokenCacheItems(requestContext))
            {
                MsalAccessTokenCacheItem accessItem = JsonHelper.DeserializeFromJson<MsalAccessTokenCacheItem>(accessItemStr);
                accessTokens.Add(accessItem.GetKey().ToString(), accessItem);
            }
            accessTokenCacheItems.ItemsSource = accessTokens;

            IDictionary<string, MsalRefreshTokenCacheItem> refreshTokens = new Dictionary<string, MsalRefreshTokenCacheItem>();
            foreach (var refreshItemStr in tokenCache.GetAllRefreshTokenCacheItems(requestContext))
            {
                MsalRefreshTokenCacheItem refreshItem = JsonHelper.DeserializeFromJson<MsalRefreshTokenCacheItem>(refreshItemStr);
                refreshTokens.Add(refreshItem.GetKey().ToString(), refreshItem);
            }
            refreshTokenCacheItems.ItemsSource = refreshTokens;

            IDictionary<string, MsalIdTokenCacheItem> idTokens = new Dictionary<string, MsalIdTokenCacheItem>();
            foreach (var idItemStr in tokenCache.GetAllIdTokenCacheItems(requestContext))
            {
                MsalIdTokenCacheItem idItem = JsonHelper.DeserializeFromJson<MsalIdTokenCacheItem>(idItemStr);
                idTokens.Add(idItem.GetKey().ToString(), idItem);
            }
            idTokenCacheItems.ItemsSource = idTokens;

            IDictionary<string, MsalAccountCacheItem> accounts = new Dictionary<string, MsalAccountCacheItem>();
            foreach (var accountStr in tokenCache.GetAllAccountCacheItems(requestContext))
            {
                MsalAccountCacheItem accountItem = JsonHelper.DeserializeFromJson<MsalAccountCacheItem>(accountStr);
                accounts.Add(accountItem.GetKey().ToString(), accountItem);
            }
            accountsCacheItems.ItemsSource = accounts;
        }

        protected override void OnAppearing()
        {
            RefreshCacheView();
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            var tokenCache = App.MsalPublicClient.UserTokenCache;
            var users = tokenCache.GetUsers(new Uri(App.Authority).Host, new RequestContext(new MsalLogger(Guid.NewGuid(), null)));
            foreach (var user in users)
            {
                tokenCache.Remove(user, new RequestContext(new MsalLogger(Guid.NewGuid(), null)));
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
            var accessTokenCacheItem = (MsalAccessTokenCacheItem) mi.CommandParameter;
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
            var accessTokenCacheItem = (MsalAccessTokenCacheItem)mi.CommandParameter;

            var tokenCache = App.MsalPublicClient.UserTokenCache;
            // todo pass idToken instead of null
            var requestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null));

            tokenCache.DeleteAccessToken(accessTokenCacheItem, null, requestContext);

            RefreshCacheView();
        }

        public void OnInvalidate(object sender, EventArgs e)
        {
            var mi = ((MenuItem) sender);
            var refreshTokenCacheItem = (MsalRefreshTokenCacheItem) mi.CommandParameter;
            var tokenCache = App.MsalPublicClient.UserTokenCache;

            // invalidate refresh token
            refreshTokenCacheItem.Secret = "InvalidValue";

            // update entry in the cache
            tokenCache.AddRefreshTokenCacheItem(refreshTokenCacheItem);

            RefreshCacheView();
        }

        public async Task ShowAccessTokenDetails(object sender, EventArgs e)
        {
            var mi = (MenuItem) sender;
            var accessTokenCacheItem = (MsalAccessTokenCacheItem) mi.CommandParameter;

            // pass idtoken instead of null
            await Navigation.PushAsync(new AccessTokenCacheItemDetails(accessTokenCacheItem, null));
        }

        public async Task ShowRefreshTokenDetails(object sender, EventArgs e)
        {
            var mi = (MenuItem)sender;
            var refreshTokenCacheItem = (MsalRefreshTokenCacheItem)mi.CommandParameter;

            await Navigation.PushAsync(new RefreshTokenCacheItemDetails(refreshTokenCacheItem));
        }

        public async Task ShowIdTokenDetails(object sender, EventArgs e)
        {
            var mi = (MenuItem)sender;
            var idTokenCacheItem = (MsalIdTokenCacheItem)mi.CommandParameter;

            // pass idtoken instead of null
            await Navigation.PushAsync(new IdTokenCacheItemDetails(idTokenCacheItem));
        }

        public async Task ShowAccountDetails(object sender, EventArgs e)
        {
            var mi = (MenuItem)sender;
            var accountCacheItem = (MsalAccountCacheItem)mi.CommandParameter;

            // pass idtoken instead of null
            await Navigation.PushAsync(new AccountCacheItemDetails(accountCacheItem));
        }
    }
}


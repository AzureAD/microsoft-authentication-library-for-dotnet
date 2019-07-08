// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinDev
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CachePage : ContentPage
    {
        public CachePage()
        {
            InitializeComponent();
        }

        private async Task RefreshCacheViewAsync()
        {
            var tokenCache = App.MsalPublicClient.UserTokenCacheInternal;

            IDictionary<string, MsalAccessTokenCacheItem> accessTokens = new Dictionary<string, MsalAccessTokenCacheItem>();
            foreach (var accessItem in (await tokenCache.GetAllAccessTokensAsync(true).ConfigureAwait(false)))
            {
                accessTokens.Add(accessItem.GetKey().ToString(), accessItem);
            }
            accessTokenCacheItems.ItemsSource = accessTokens;

            IDictionary<string, MsalRefreshTokenCacheItem> refreshTokens = new Dictionary<string, MsalRefreshTokenCacheItem>();
            foreach (var refreshItem in (await tokenCache.GetAllRefreshTokensAsync(true).ConfigureAwait(false)))
            {
                refreshTokens.Add(refreshItem.GetKey().ToString(), refreshItem);
            }
            refreshTokenCacheItems.ItemsSource = refreshTokens;

            IDictionary<string, MsalIdTokenCacheItem> idTokens = new Dictionary<string, MsalIdTokenCacheItem>();
            foreach (var idItem in (await tokenCache.GetAllIdTokensAsync(true).ConfigureAwait(false)))
            {
                idTokens.Add(idItem.GetKey().ToString(), idItem);
            }
            idTokenCacheItems.ItemsSource = idTokens;

            IDictionary<string, MsalAccountCacheItem> accounts = new Dictionary<string, MsalAccountCacheItem>();
            foreach (var accountItem in (await tokenCache.GetAllAccountsAsync().ConfigureAwait(false)))
            {
                accounts.Add(accountItem.GetKey().ToString(), accountItem);
            }
            accountsCacheItems.ItemsSource = accounts;
        }

        protected override async void OnAppearing()
        {
            await RefreshCacheViewAsync().ConfigureAwait(false);
        }

        private async void OnClearClickedAsync(object sender, EventArgs e)
        {
            foreach (var user in await App.MsalPublicClient.GetAccountsAsync().ConfigureAwait(false))
            {
                await App.MsalPublicClient.RemoveAsync(user).ConfigureAwait(false);
            }

            await RefreshCacheViewAsync().ConfigureAwait(false);
        }

        private static string GetCurrentTimestamp()
        {
            return ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        public void OnExpire(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var accessTokenCacheItem = (MsalAccessTokenCacheItem)mi.CommandParameter;
            var tokenCache = App.MsalPublicClient.UserTokenCacheInternal;

            // set access token as expired
            accessTokenCacheItem.ExpiresOnUnixTimestamp = GetCurrentTimestamp();

            // update entry in the cache
            tokenCache.AddAccessTokenCacheItem(accessTokenCacheItem);

            RefreshCacheViewAsync().ConfigureAwait(true);
        }

        public void OnAtDelete(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var accessTokenCacheItem = (MsalAccessTokenCacheItem)mi.CommandParameter;

            var tokenCache = App.MsalPublicClient.UserTokenCacheInternal;
            tokenCache.DeleteAccessToken(accessTokenCacheItem);

            RefreshCacheViewAsync().ConfigureAwait(true);
        }

        public void OnInvalidate(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var refreshTokenCacheItem = (MsalRefreshTokenCacheItem)mi.CommandParameter;
            var tokenCache = App.MsalPublicClient.UserTokenCacheInternal;

            // invalidate refresh token
            refreshTokenCacheItem.Secret = "InvalidValue";

            // update entry in the cache
            tokenCache.AddRefreshTokenCacheItem(refreshTokenCacheItem);

            RefreshCacheViewAsync().ConfigureAwait(true);
        }

        public async void ShowAccessTokenDetailsAsync(object sender, EventArgs e)
        {
            var mi = (MenuItem)sender;
            var accessTokenCacheItem = (MsalAccessTokenCacheItem)mi.CommandParameter;

            // pass idtoken instead of null
            await Navigation.PushAsync(new AccessTokenCacheItemDetails(accessTokenCacheItem, null)).ConfigureAwait(false);
        }

        public async void ShowRefreshTokenDetailsAsync(object sender, EventArgs e)
        {
            var mi = (MenuItem)sender;
            var refreshTokenCacheItem = (MsalRefreshTokenCacheItem)mi.CommandParameter;

            await Navigation.PushAsync(new RefreshTokenCacheItemDetails(refreshTokenCacheItem)).ConfigureAwait(false);
        }

        public async void ShowIdTokenDetailsAsync(object sender, EventArgs e)
        {
            var mi = (MenuItem)sender;
            var idTokenCacheItem = (MsalIdTokenCacheItem)mi.CommandParameter;

            // pass idtoken instead of null
            await Navigation.PushAsync(new IdTokenCacheItemDetails(idTokenCacheItem)).ConfigureAwait(false);
        }

        public async void ShowAccountDetailsAsync(object sender, EventArgs e)
        {
            var mi = (MenuItem)sender;
            var accountCacheItem = (MsalAccountCacheItem)mi.CommandParameter;

            // pass idtoken instead of null
            await Navigation.PushAsync(new AccountCacheItemDetails(accountCacheItem)).ConfigureAwait(false);
        }
    }
}


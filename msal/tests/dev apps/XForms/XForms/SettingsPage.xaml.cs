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
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            RefreshView();
        }

        private void RefreshView()
        {
            authority.Text = App.Authority;
            clientIdEntry.Text = App.ClientId;

            numOfAtItems.Text = App.MsalPublicClient.UserTokenCache.tokenCacheAccessor.GetAllAccessTokensAsString()
                .Count.ToString(CultureInfo.InvariantCulture);
            numOfRtItems.Text = App.MsalPublicClient.UserTokenCache.tokenCacheAccessor.GetAllRefreshTokensAsString()
                .Count.ToString(CultureInfo.InvariantCulture);
            numOfIdItems.Text = App.MsalPublicClient.UserTokenCache.tokenCacheAccessor.GetAllIdTokensAsString()
                .Count.ToString(CultureInfo.InvariantCulture);
            numOfAccountItems.Text = App.MsalPublicClient.UserTokenCache.tokenCacheAccessor.GetAllAccountsAsString()
                .Count.ToString(CultureInfo.InvariantCulture);

            validateAuthority.IsToggled = App.ValidateAuthority;
            RedirectUriLabel.Text = App.MsalPublicClient.RedirectUri;
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            App.Authority = authority.Text;
            App.ClientId = clientIdEntry.Text;
            App.InitPublicClient();
        }

        private void OnClearAllCache(object sender, EventArgs e)
        {
            App.MsalPublicClient.UserTokenCache.Clear();
            RefreshView();
        }

        private void OnClearAdalCache(object sender, EventArgs e)
        {
            App.MsalPublicClient.UserTokenCache.ClearAdalCache();
            RefreshView();
        }

        private void OnClearMsalCache(object sender, EventArgs e)
        {
            App.MsalPublicClient.UserTokenCache.ClearMsalCache();
            RefreshView();
        }

        private void OnValidateAuthorityToggled(object sender, ToggledEventArgs args)
        {
            App.MsalPublicClient.ValidateAuthority = args.Value;
            App.ValidateAuthority = args.Value;
        }

        private void OnB2cSwitchToggled(object sender, ToggledEventArgs args)
        {
            if (b2cSwitch.IsToggled)
            {
                App.Authority = App.B2cAuthority;
                App.Scopes = App.B2cScopes;
                App.ClientId = App.B2cClientId;
            }
            else
            {
                App.Authority = App.DefaultAuthority;
                App.Scopes = App.DefaultScopes;
                App.ClientId = App.DefaultClientId;
            }
            App.InitPublicClient();
            RefreshView();
        }
    }
}

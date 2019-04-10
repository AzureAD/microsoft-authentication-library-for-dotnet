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
using System.Linq;
using Microsoft.Identity.Client;
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

            numOfAtItems.Text = App.MsalPublicClient.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count().ToString(CultureInfo.InvariantCulture);
            numOfRtItems.Text = App.MsalPublicClient.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count().ToString(CultureInfo.InvariantCulture);
            numOfIdItems.Text = App.MsalPublicClient.UserTokenCacheInternal.Accessor.GetAllIdTokens().Count().ToString(CultureInfo.InvariantCulture);
            numOfAccountItems.Text = App.MsalPublicClient.UserTokenCacheInternal.Accessor.GetAllAccounts().Count().ToString(CultureInfo.InvariantCulture);

            validateAuthoritySwitch.IsToggled = App.ValidateAuthority;
            RedirectUriLabel.Text = App.MsalPublicClient.AppConfig.RedirectUri;
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            App.Authority = authority.Text;
            App.ClientId = clientIdEntry.Text;
            App.InitPublicClient(null, App.ClientId);
        }

        private void OnClearAllCache(object sender, EventArgs e)
        {
            App.MsalPublicClient.UserTokenCacheInternal.Clear();
            RefreshView();
        }

        private void OnClearAdalCache(object sender, EventArgs e)
        {
            App.MsalPublicClient.UserTokenCacheInternal.ClearAdalCache();
            RefreshView();
        }

        private void OnClearMsalCache(object sender, EventArgs e)
        {
            App.MsalPublicClient.UserTokenCacheInternal.ClearMsalCache();
            RefreshView();
        }

        private void OnValidateAuthorityToggled(object sender, ToggledEventArgs args)
        {
            App.ValidateAuthority = args.Value;
            App.InitPublicClient(null, App.ClientId);
        }

        private void OnPickerSelectedIndexChanged(object sender, EventArgs args)
        {
            var selectedB2CAuthority = (Picker)sender;
            int selectedIndex = selectedB2CAuthority.SelectedIndex;

            switch (selectedIndex)
            {
            case 0:
                App.UseB2CAuthorityHost = true;
                App.InitPublicClient(App.B2CMicrosoftLoginAuthorityHost, App.B2cClientId);               
                RefreshView();
                break;
            case 1:
                App.UseB2CAuthorityHost = true;
                App.InitPublicClient(App.B2CAuthorityHost, App.B2cClientId);
                RefreshView();
                break;
            }
        }

        private void OnPolicyPickerSelectedIndexChanged(object sender, EventArgs args)
        {
            var selectedB2CPolicy = (Picker)sender;
            int selectedIndex = selectedB2CPolicy.SelectedIndex;

            switch (selectedIndex)
            {
            case 0:
                App.B2CPolicy = App.B2CSiSuPolicy;
                break;
            case 1:
                App.B2CPolicy = App.B2CEditProfilePolicy;
                break;
            }
        }

        private void OnAcquireTokenWithBrokerToggled(object sender, ToggledEventArgs args)
        {
            App.UseBroker = args.Value;
            App.InitPublicClient(null, App.ClientId);
        }
    }
}

//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

using TestApp.PCL;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Test.ADAL.Common;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace AdalUniversalTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private TokenBroker tokenBroker;

        public MainPage()
        {
            this.InitializeComponent();
            tokenBroker = new TokenBroker();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            AuthenticationContext context = new AuthenticationContext("https://login.windows.net/common", false);

            var result = await context.AcquireTokenAsync("https://api.office.com/discovery/", "af124e86-4e96-495a-b70a-90f90ab96707", new Uri("ms-onedrive://com.microsoft.skydrive"),
                new PlatformParameters(PromptBehavior.Auto, false), new UserIdentifier("kpanwar@microsoft.com", UserIdentifierType.OptionalDisplayableId));

            result = await context.AcquireTokenSilentAsync("https://encana-my.sharepoint.com/",
                "af124e86-4e96-495a-b70a-90f90ab96707",
                new UserIdentifier("kpanwar@microsoft.com", UserIdentifierType.OptionalDisplayableId));
            
            this.AccessToken.Text = result.AccessToken;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            string token = await tokenBroker.GetTokenWithUsernamePasswordAsync();
            this.AccessToken.Text = token;
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            string token = await tokenBroker.GetTokenInteractiveWithMsAppAsync(new PlatformParameters(PromptBehavior.Auto, false));
            this.AccessToken.Text = token;
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            string token = await tokenBroker.GetTokenWithClientCredentialAsync();
            this.AccessToken.Text = token;
        }
    }
}

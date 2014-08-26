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

using System.Threading.Tasks;

using Windows.ApplicationModel.Activation;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641
using Test.ADAL.Common;

namespace Test.ADAL.WinPhone.Dashboard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IWebAuthenticationContinuable
    {
        private AuthenticationContext context;

        private Sts sts;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.sts = StsFactory.CreateSts(StsType.AAD);
        }

        public async void ContinueWebAuthentication(WebAuthenticationBrokerContinuationEventArgs args)
        {
            await this.context.ContinueAcquireTokenAsync(args);
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;

            this.context = await AuthenticationContext.CreateAsync(sts.Authority);

            var result = await this.context.AcquireTokenSilentAsync(sts.ValidResource, sts.ValidClientId, sts.ValidUserId);
            if (result.Status == AuthenticationStatus.Success)
            {
                this.DisplayToken(result);
            }
            else
            {
                this.context.AcquireTokenAndContinue(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, sts.ValidUserId, this.DisplayToken);
            }
        }

        private void DisplayToken(AuthenticationResult result)
        {
            if (!string.IsNullOrEmpty(result.AccessToken))
            {
                this.AccessToken.Text = result.AccessToken;
            }
            else
            {
                this.AccessToken.Text = result.ErrorDescription;
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            this.context = await AuthenticationContext.CreateAsync(sts.Authority);
            this.context.TokenCache.Clear();
        }

        private async void SsoButton_Click(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;

            this.context = await AuthenticationContext.CreateAsync(sts.Authority);

            var result = await this.context.AcquireTokenSilentAsync(sts.ValidResource, sts.ValidClientId, sts.ValidUserId);
            if (result.Status == AuthenticationStatus.Success)
            {
                this.DisplayToken(result);
            }
            else
            {
                this.context.AcquireTokenAndContinue(sts.ValidResource, sts.ValidClientId, null, sts.ValidUserId, this.DisplayToken);
            }
        }
    }
}

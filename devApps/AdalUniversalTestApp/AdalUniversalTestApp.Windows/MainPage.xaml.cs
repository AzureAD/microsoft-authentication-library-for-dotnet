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
using Microsoft.IdentityModel.Clients.ActiveDirectory;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace AdalUniversalTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void AccessTokenButton_Click(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            
            try
            {
                AuthenticationResult result =
                    await
                        ctx.AcquireTokenAsync("https://graph.windows.net", "<CLIENT-ID>",
                            new Uri("<REDIRECT-URI>"), new PlatformParameters(PromptBehavior.SelectAccount, false)).ConfigureAwait(false);

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    AccessToken.Text = "Signed in User - " + result.UserInfo.DisplayableId + "\nAccessToken: \n" + result.AccessToken;
                });
            }
            catch (Exception exc)
            {
                this.AccessToken.Text = exc.Message;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
        }
    }
}

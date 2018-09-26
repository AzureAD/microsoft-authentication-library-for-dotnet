//------------------------------------------------------------------------------
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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UAPTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string ClientId = "cd01dc27-9d3c-4812-beda-8229d5d4a8d5";

        private const string ReturnUri = "https://MyDirectorySearcherApp";

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
                AuthenticationResult result = await ctx.AcquireTokenAsync("https://graph.windows.net",
                    ClientId, new Uri(ReturnUri),
                    new PlatformParameters(PromptBehavior.Auto, false));

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        AccessToken.Text = "Signed in User - " + result.UserInfo.DisplayableId + "\nAccessToken: \n" + result.AccessToken;
                    });
            }
            catch (Exception exc)
            {
                await ShowError(exc);
            }
        }

        private async void ClearCache(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            try
            {
                ctx.TokenCache.Clear();
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    AccessToken.Text = "Cache was cleared";
                });
            }
            catch (Exception exc)
            {
                await ShowError(exc);
            }
        }

        private async void ShowCacheCount(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    AccessToken.Text = "Token Cache count is " + ctx.TokenCache.Count;
                });
            }
            catch (Exception exc)
            {
                await ShowError(exc);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void Button_Click_2(object sender, RoutedEventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            this.AccessToken.Text = string.Empty;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void AcquireTokenClientCred_Click(object sender, RoutedEventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            this.AccessToken.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");

            try
            {
                AuthenticationResult result = await ctx.AcquireTokenAsync(
                    "https://graph.windows.net",
                    ClientId,
                    new UserCredential()); // can add a

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        AccessToken.Text = "Signed in User - " + result.UserInfo.DisplayableId + "\nAccessToken: \n" + result.AccessToken;
                    });
            }
            catch (Exception exc)
            {
                await ShowError(exc);
            }
        }

        private async void AccessTokenSilentButton_Click(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            AuthenticationContext context = new AuthenticationContext("https://login.microsoftonline.com/common");
            try
            {
                AuthenticationResult authResult = await context.AcquireTokenSilentAsync("https://graph.microsoft.com", ClientId).ConfigureAwait(false);

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                       () =>
                       {
                           AccessToken.Text = "Acquire Token Silent:\nSigned in User - " + authResult.UserInfo.DisplayableId + "\nAccessToken: \n" + authResult.AccessToken;
                       });
            }
            catch (Exception exc)
            {
                await ShowError(exc);
            }
        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private async void AcquireTokenIWA_Click(object sender, RoutedEventArgs e) // make sure to use a client id that is configured, such as the one from the .net sample
        {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

            this.AccessToken.Text = string.Empty;
            AuthenticationContext context = new AuthenticationContext("https://login.microsoftonline.com/common");
            try
            {
                AuthenticationResult authResult = await context.AcquireTokenAsync(
                    "https://graph.microsoft.com",
                    ClientId, new UserCredential())
                        .ConfigureAwait(false);

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                       () =>
                       {
                           AccessToken.Text = "Acquire Token Silent:\nSigned in User - " + authResult.UserInfo.DisplayableId + "\nAccessToken: \n" + authResult.AccessToken;
                       });
            }
            catch (Exception exc)
            {
                await ShowError(exc);
            }
        }

        private async System.Threading.Tasks.Task ShowError(Exception exc)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                  () =>
                  {
                      this.AccessToken.Text = "Auth failed: " + exc.Message;
                  });
        }
    }
}

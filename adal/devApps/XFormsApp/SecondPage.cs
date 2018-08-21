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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Text;
using Xamarin.Forms;

namespace XFormsApp
{
    public class SecondPage : ContentPage
    {
        private readonly StringBuilder _logs = new StringBuilder();

        public const string ClientId = "d3590ed6-52b3-4102-aeff-aad2292ab01c";
        public const string ClientIdBroker = "<ClientIdBroker>";
        public const string AndroidBrokerRedirectURI = "msauth://com.microsoft.xformsdroid.adal/mJaAVvdXtcXy369xPWv2C7mV674=";
        public const string IOSBrokerRedirectURI = "adaliosapp://com.yourcompany.xformsapp";
        public const string User = "<User>";
        static string RedirectURI = "urn:ietf:wg:oauth:2.0:oob";

        public string DrainLogs()
        {
            string output = _logs.ToString();
            _logs.Clear();
            return output;
        }

        private readonly Label result;
        private readonly Label testResult;

        public IPlatformParameters Parameters { get; set; }

        public IPlatformParameters BrokerParameters { get; set; }

        public SecondPage()
        {
            var acquireTokenButton = new Button
            {
                Text = "Acquire Token",
                AutomationId = "acquireToken"
            };

            var acquireTokenSilentButton = new Button
            {
                Text = "Acquire Token Silent",
                AutomationId = "acquireTokenSilent"
            };

            var clearAllCacheButton = new Button
            {
                Text = "Clear All Cache",
                AutomationId = "clearCache"
            };

            var acquireTokenWithBrokerButton = new Button
            {
                Text = "Acquire Token With Broker",
                AutomationId = "acquireTokenBroker"
            };

            var acquireTokenSilentWithBrokerButton = new Button
            {
                Text = "Acquire Token Silent With Broker",
                AutomationId = "acquireTokenSilentWithBroker"
            };

            testResult = new Label()
            {
                Text = "Success:",
                VerticalOptions = LayoutOptions.FillAndExpand,
                AutomationId = "testResult"
            };

            result = new Label()
            {
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            var scrollView = new ScrollView()
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Content = new StackLayout()
                {
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    Children =
                    {
                        testResult,
                        result
                    }
                }
            };

            acquireTokenButton.Clicked += AcquireTokenButton_Clicked;
            acquireTokenSilentButton.Clicked += AcquireTokenSilentButton_Clicked;
            clearAllCacheButton.Clicked += ClearAllCacheButton_Clicked;
            acquireTokenWithBrokerButton.Clicked += AcquireTokenWithBrokerButton_Clicked;
            acquireTokenSilentWithBrokerButton.Clicked += AcquireTokenSilentWithBrokerButton_Clicked;

            Thickness padding;

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    padding = new Thickness(0, 40, 0, 0);
                    break;
                case Device.UWP:
                    padding = new Thickness(0, 20, 0, 0);
                    break;
                default:
                    padding = new Thickness(0, 0, 0, 0);
                    break;
            }

            Content = new StackLayout
            {
                Padding = padding,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = {
                    acquireTokenButton,
                    acquireTokenSilentButton,
                    clearAllCacheButton,
                    acquireTokenWithBrokerButton,
                    acquireTokenSilentWithBrokerButton,
                    scrollView
                }
            };

            void LogCallback(LogLevel level, string message, bool containsPii)
            {
                _logs.AppendLine(message);
            }

            LoggerCallbackHandler.LogCallback = LogCallback;
        }

        private async void AcquireTokenSilentButton_Clicked(object sender, EventArgs e)
        {
            this.result.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            string output = string.Empty;
            try
            {
                AuthenticationResult result = await ctx.AcquireTokenSilentAsync("https://graph.microsoft.com", ClientId).ConfigureAwait(false);
                output = "Signed in User - " + result.UserInfo.DisplayableId;
            }
            catch (Exception exc)
            {
                output = exc.Message;
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    this.result.Text += "Result : " + output;

                    this.result.Text += "Logs : " + DrainLogs();
                });
            }
        }

        async void AcquireTokenButton_Clicked(object sender, EventArgs e)
        {
            this.result.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            string output = string.Empty;
            string accessToken = String.Empty;
            this.testResult.Text = "Success:";
            try
            {
                AuthenticationResult result =
                    await
                        ctx.AcquireTokenAsync("https://graph.microsoft.com", ClientId,
                            new Uri(RedirectURI), Parameters).ConfigureAwait(false);
                output = "Signed in User - " + result.UserInfo.DisplayableId;
                accessToken = result.AccessToken;
            }
            catch (Exception exc)
            {
                output = exc.Message;
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    this.testResult.Text = string.IsNullOrWhiteSpace(accessToken) ? "Success: False" : "Success: True";
                    this.result.Text += "Result : " + output;

                    this.result.Text += "Logs : " + DrainLogs();
                });
            }
        }

        private void AcquireTokenWithBrokerButton_Clicked(object sender, EventArgs e)
        {
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    AcquireTokenWithBroker();
                    break;
                case Device.Android:
                    AcquireTokenWithBroker();
                    break;
                default:
                    this.result.Text = "UWP does not support broker. Use iOS or Android.";
                    break;
            }
        }

        private void AcquireTokenSilentWithBrokerButton_Clicked(object sender, EventArgs e)
        {
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    AcquireTokenSilentWithBroker();
                    break;
                case Device.Android:
                    AcquireTokenSilentWithBroker();
                    break;
                default:
                    this.result.Text = "UWP does not support broker. Use iOS or Android.";
                    break;
            }
        }

        private async void AcquireTokenWithBroker()
        {
            this.result.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            string output = string.Empty;
            string accessToken = String.Empty;
            this.testResult.Text = "Success:";

            try
            {
                AuthenticationResult result =
                    await
                        ctx.AcquireTokenAsync("https://graph.microsoft.com", ClientIdBroker,
                            new Uri(AndroidBrokerRedirectURI),
                            BrokerParameters).ConfigureAwait(false);
                output = "Signed in User - " + result.UserInfo.DisplayableId;
                accessToken = result.AccessToken;
            }
            catch (Exception exc)
            {
                output = exc.Message;
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    this.testResult.Text = string.IsNullOrWhiteSpace(accessToken) ? "Success: False" : "Success: True";
                    this.result.Text += "Result : " + output;

                    this.result.Text += "Logs : " + DrainLogs();
                });
            }
        }

        private async void AcquireTokenSilentWithBroker()
        {
            this.result.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            string output = string.Empty;
            try
            {
                AuthenticationResult result = await ctx.AcquireTokenSilentAsync("https://graph.microsoft.com", ClientIdBroker,
                    new UserIdentifier(User, UserIdentifierType.OptionalDisplayableId), BrokerParameters).ConfigureAwait(false);
                output = "Signed in User - " + result.UserInfo.DisplayableId;
            }
            catch (Exception exc)
            {
                output = exc.Message;
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    this.result.Text += "Result : " + output;

                    this.result.Text += "Logs : " + DrainLogs();
                });
            }
        }

        private string DeterminePlatformForRedirectUri()
        {
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    RedirectURI = IOSBrokerRedirectURI;
                    break;
                case Device.Android:
                    RedirectURI = AndroidBrokerRedirectURI;
                    break;
                default:
                    throw new NotImplementedException();
            }
            return RedirectURI;
        }

        void ClearAllCacheButton_Clicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                this.result.Text = "Cache items before clear: " + TokenCache.DefaultShared.Count + Environment.NewLine;
                TokenCache.DefaultShared.Clear();
                this.result.Text += "Cache items after clear: " + TokenCache.DefaultShared.Count + Environment.NewLine;
            });
        }
    }
}
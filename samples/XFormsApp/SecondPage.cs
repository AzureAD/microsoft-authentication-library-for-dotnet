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
using TestApp.PCL;
using Xamarin.Forms;

namespace XFormsApp
{

    class AdalCallback : IAdalLogCallback
    {
        StringBuilder logs = new StringBuilder();
        
        public void Log(LogLevel level, string message)
        {
            logs.AppendLine(message);
        }

        public string DrainLogs()
        {
            string output = logs.ToString();
            logs.Clear();
            return output;
        }
    }

    public class SecondPage : ContentPage
    {
        private TokenBroker tokenBroker;
        private Label result;
        private Label logLabel;
        private AdalCallback callback = new AdalCallback();
        public SecondPage()
        {
            this.tokenBroker = new TokenBroker();

            var acquireTokenButton = new Button
            {
                Text = "Acquire Token"
            };

            var acquireTokenSilentButton = new Button
            {
                Text = "Acquire Token Silent"
            };

            var clearButton = new Button
            {
                Text = "Clear Cache"
            };

            result = new Label { };
            logLabel = new Label
            {
            };

            acquireTokenButton.Clicked += browseButton_Clicked;
            acquireTokenSilentButton.Clicked += acquireTokenSilentButton_Clicked;
            clearButton.Clicked += clearButton_Clicked;

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children = {
                    acquireTokenButton,
                    acquireTokenSilentButton,
                    clearButton,
                    result,
                    logLabel
				}
            };

            LoggerCallbackHandler.Callback = callback;
        }

        private async void acquireTokenSilentButton_Clicked(object sender, EventArgs e)
        {
            this.result.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            string output = string.Empty;
            try
            {
                AuthenticationResult result = await ctx.AcquireTokenSilentAsync("https://graph.windows.net", "de49ddaf-c7f8-4a06-8463-3c6ae124fe52").ConfigureAwait(false);
                output = "Signed in User - " + result.UserInfo.DisplayableId;
            }
            catch (Exception exc)
            {
                output = exc.Message;
            } finally
            {
                Device.BeginInvokeOnMainThread(() => {
                    this.logLabel.Text = callback.DrainLogs();
                    this.result.Text = output;
                });
            }

        }

        public IPlatformParameters Parameters { get; set; }

        async void browseButton_Clicked(object sender, EventArgs e)
        {
            this.result.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            string output = string.Empty;
            try
            {
                AuthenticationResult result =
                    await
                        ctx.AcquireTokenAsync("https://graph.windows.net", "de49ddaf-c7f8-4a06-8463-3c6ae124fe52",
                            new Uri("adaliosapp://com.yourcompany.xformsapp"),
                            Parameters).ConfigureAwait(false);
                output = "Signed in User - " + result.UserInfo.DisplayableId;
            }
            catch (Exception exc)
            {
                output = exc.Message;
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() => {
                                                         this.logLabel.Text = callback.DrainLogs();
                    this.result.Text = output;
                });
            }

        }

        void clearButton_Clicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() => {
                this.result.Text = "Cache items before clear: " + TokenCache.DefaultShared.Count + Environment.NewLine;
                tokenBroker.ClearTokenCache();
                this.result.Text += "Cache items after clear: " + TokenCache.DefaultShared.Count + Environment.NewLine;
            });
        }
    }
}

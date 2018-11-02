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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace XFormsApp
{
    public class SecondPage : ContentPage
    {
        private readonly StringBuilder _logs = new StringBuilder();

        public string User = "<User>";
        private string Tenant = "<Tenant>";
        public const string AndroidBrokerRedirectURI = "msauth://com.microsoft.xformsdroid.adal/mJaAVvdXtcXy369xPWv2C7mV674=";
        public const string IOSBrokerRedirectURI = "adaliosapp://com.yourcompany.xformsapp";
        static string RedirectURI = "urn:ietf:wg:oauth:2.0:oob";

        public string DrainLogs()
        {
            string output = _logs.ToString();
            _logs.Clear();
            return output;
        }

        private readonly Label result;
        private readonly Label testResult;
        private Picker clientIdPicker;
        private Picker resourcePicker;
        private Picker promptBehaviorPicker;
        private Entry clientIdInput;
        private Entry resourceInput;
        private Entry promptBehaviorInput;

        private string ClientId { get; set; } = AppConstants.UiAutomationTestClientId;
        private string Resource { get; set; } = AppConstants.MSGraph;
        
        public IPlatformParameters BrokerParameters { get; set; }

        public string PlatformParameters { get; set; }

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

            var clientIdInputLabel = new Label
            {
                Text = "CientId:"
            };

            var resourceInputLabel = new Label
            {
                Text = "Resource:"
            };

            var promptBehaviorLabel = new Label
            {
                Text = "Prompt Behavior:"
            };

            clientIdPicker = new Picker
            {
                Title = "Pick an application",
                ItemsSource = new List<string>(AppConstants.LabelToApplicationUriMap.Keys),
                AutomationId = "clientIdPicker"
            };

            resourcePicker = new Picker
            {
                Title = "Pick a resource",
                ItemsSource = new List<string>(AppConstants.LabelToResourceUriMap.Keys),
                AutomationId = "resourcePicker"
            };

            promptBehaviorPicker = new Picker
            {
                Title = "Select a prompt behavior",
                ItemsSource = new List<string>(AppConstants.PromptBehaviorList),
                AutomationId = "promptBehaviorPicker"
            };

            clientIdInput = new Entry
            {
                Text = AppConstants.UiAutomationTestClientId,
                AutomationId = "clientIdEntry"
            };

            resourceInput = new Entry
            {
                Text = AppConstants.MSGraph,
                AutomationId = "resourceEntry"
            };

            promptBehaviorInput = new Entry
            {
                Text = "auto",
                AutomationId = "promptBehaviorEntry"
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
            clientIdPicker.SelectedIndexChanged += UpdateClientId;
            resourcePicker.SelectedIndexChanged += UpdateResourceId;
            clientIdInput.TextChanged += UpdateClientIdFromInput;
            resourceInput.TextChanged += UpdateResourceFromInput;
            promptBehaviorPicker.SelectedIndexChanged += UpdatePromptBehavior;
            promptBehaviorInput.TextChanged += UpdatePromptBehaviorFromInput;

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
                    clientIdInputLabel,
                    clientIdPicker,
                    clientIdInput,
                    resourceInputLabel,
                    resourcePicker,
                    resourceInput,
                    promptBehaviorLabel,
                    promptBehaviorPicker,
                    promptBehaviorInput,
                    scrollView
                }
            };

            void LogCallback(LogLevel level, string message, bool containsPii)
            {
                _logs.AppendLine(message);
            }

            LoggerCallbackHandler.LogCallback = LogCallback;
        }

        private async void AcquireTokenButton_Clicked(object sender, EventArgs e)
        {
            this.result.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            string output = string.Empty;
            string accessToken = string.Empty;
            this.testResult.Text = "Result:";

            try
            {
                var factory = DependencyService.Get<IPlatformParametersFactory>();
                IPlatformParameters platformParameters = factory.GetPlatformParameters(PlatformParameters);

                AuthenticationResult result =
                    await
                        ctx.AcquireTokenAsync(Resource, ClientId, new Uri(RedirectURI), platformParameters).ConfigureAwait(false);
                output = "Signed in User - " + result.UserInfo.DisplayableId;
                accessToken = result.AccessToken;
                User = result.UserInfo.DisplayableId;
                Tenant = result.TenantId;
            }
            catch (Exception exc)
            {
                output = exc.Message;
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    this.testResult.Text = string.IsNullOrWhiteSpace(accessToken) ? "Result: Failure" : "Result: Success";
                    this.result.Text += "Result : " + output;
                    this.result.Text += "Logs : " + DrainLogs();
                });
            }
        }

        private async void AcquireTokenSilentButton_Clicked(object sender, EventArgs e)
        {
            this.result.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/" + Tenant);
            string output = string.Empty;
            string accessToken = string.Empty;
            this.testResult.Text = "Result:";
            try
            {
                AuthenticationResult result = await ctx.AcquireTokenSilentAsync(Resource, ClientId,
                                              new UserIdentifier(User, UserIdentifierType.OptionalDisplayableId)).ConfigureAwait(false);
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
                    this.testResult.Text = string.IsNullOrWhiteSpace(accessToken) ? "Result: Failure" : "Result: Success";
                    this.result.Text += "Result : " + output;
                    this.result.Text += "Logs : " + DrainLogs();
                });
            }
        }

        private async void AcquireTokenWithBroker()
        {
            this.result.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            string output = string.Empty;
            string accessToken = String.Empty;
            this.testResult.Text = "Result:";

            try
            {
                AuthenticationResult result =
                    await
                        ctx.AcquireTokenAsync(Resource, ClientId,
                            new Uri(AndroidBrokerRedirectURI),
                            BrokerParameters).ConfigureAwait(false);
                output = "Signed in User - " + result.UserInfo.DisplayableId;
                accessToken = result.AccessToken;
                User = result.UserInfo.DisplayableId;
            }
            catch (Exception exc)
            {
                output = exc.Message;
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    this.testResult.Text = string.IsNullOrWhiteSpace(accessToken) ? "Result: Failure" : "Result: Success";
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
            string accessToken = String.Empty;
            this.testResult.Text = "Result:";
            try
            {
                AuthenticationResult result = await ctx.AcquireTokenSilentAsync(Resource, ClientId,
                    new UserIdentifier(User, UserIdentifierType.OptionalDisplayableId), BrokerParameters).ConfigureAwait(false);
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
                    this.testResult.Text = string.IsNullOrWhiteSpace(accessToken) ? "Result: Failure" : "Result: Success";
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
                case Device.Android:
                    AcquireTokenWithBroker();
                    break;
                case Device.UWP:
                    this.result.Text = "UWP does not support broker. Use iOS or Android.";
                    break;
                default:
                    throw new NotImplementedException();
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
                case Device.UWP:
                    this.result.Text = "UWP does not support broker. Use iOS or Android.";
                    break;
                default:
                    throw new NotImplementedException();
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

        void UpdateClientId(object sender, EventArgs e)
        {
            ClientId = clientIdInput.Text = AppConstants.LabelToApplicationUriMap.Where(x => x.Key == (string)clientIdPicker.SelectedItem).FirstOrDefault().Value;
        }

        void UpdateResourceId(object sender, EventArgs e)
        {
            Resource = resourceInput.Text = AppConstants.LabelToResourceUriMap.Where(x => x.Key == (string)resourcePicker.SelectedItem).FirstOrDefault().Value;
        }

        void UpdatePromptBehavior(object sender, EventArgs e)
        {
            PlatformParameters = promptBehaviorInput.Text = AppConstants.PromptBehaviorList.Where(x => x == (string)promptBehaviorPicker.SelectedItem).FirstOrDefault();
        }

        void UpdateClientIdFromInput(object sender, EventArgs e)
        {
            ClientId = clientIdInput.Text;
        }

        private void UpdateResourceFromInput(object sender, TextChangedEventArgs e)
        {
            Resource = resourceInput.Text;
        }

        private void UpdatePromptBehaviorFromInput(object sender, TextChangedEventArgs e)
        {
            PlatformParameters = promptBehaviorInput.Text;
        }
    }
}
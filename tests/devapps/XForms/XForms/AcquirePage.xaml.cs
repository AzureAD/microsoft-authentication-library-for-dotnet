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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AcquirePage : ContentPage
    {
        private const string UserNotSelected = "not selected";
        private const string EmptyResult = "Result:";
        private const string SuccessfulResult = "Result: Success";

        public AcquirePage()
        {
            InitializeComponent();
            InitUIBehaviorPicker();
        }

        protected override void OnAppearing()
        {
            RefreshUsers();
            ScopesEntry.Text = string.Join("", App.Scopes);
        }

        private void RefreshUsers()
        {
            var userIds = App.MsalPublicClient.GetAccountsAsync().Result.
                Select(x => x.Username).ToList();

            userIds.Add(UserNotSelected);
            usersPicker.ItemsSource = userIds;
            usersPicker.SelectedIndex = userIds.Count - 1; // go to UserNotSelected
        }

        private void InitUIBehaviorPicker()
        {
            var options = new List<string>
            {
                Prompt.SelectAccount.PromptValue,
                Prompt.ForceLogin.PromptValue,
                Prompt.Consent.PromptValue,
                Prompt.NoPrompt.PromptValue
            };

            UIBehaviorPicker.ItemsSource = options;
            UIBehaviorPicker.SelectedItem = Prompt.ForceLogin.PromptValue;
        }

        private Prompt GetUIBehavior()
        {
            var selectedUIBehavior = UIBehaviorPicker.SelectedItem as string;

            if (Prompt.ForceLogin.PromptValue.Equals(selectedUIBehavior, StringComparison.OrdinalIgnoreCase))
            {
                return Prompt.ForceLogin;
            }

            if (Prompt.Consent.PromptValue.Equals(selectedUIBehavior, StringComparison.OrdinalIgnoreCase))
            {
                return Prompt.Consent;
            }

            if (Prompt.NoPrompt.PromptValue.Equals(selectedUIBehavior, StringComparison.OrdinalIgnoreCase))
            {
                return Prompt.NoPrompt;
            }

            return Prompt.SelectAccount;
        }

        private string GetExtraQueryParams()
        {
            return ExtraQueryParametersEntry.Text.Trim();
        }

        private string ToString(IAccount user)
        {
            var sb = new StringBuilder();

            sb.AppendLine("user.DisplayableId : " + user.Username);
            //sb.AppendLine("user.IdentityProvider : " + user.IdentityProvider);
            sb.AppendLine("user.Environment : " + user.Environment);

            return sb.ToString();
        }

        private string ToString(AuthenticationResult result)
        {
            var sb = new StringBuilder();

            sb.AppendLine("AccessToken : " + result.AccessToken);
            sb.AppendLine("IdToken : " + result.IdToken);
            sb.AppendLine("ExpiresOn : " + result.ExpiresOn);
            sb.AppendLine("TenantId : " + result.TenantId);
            sb.AppendLine("Scope : " + string.Join(",", result.Scopes));
            sb.AppendLine("User :");
            sb.Append(ToString(result.Account));

            return sb.ToString();
        }

        private IAccount getUserByDisplayableId(string str)
        {
            return string.IsNullOrWhiteSpace(str) ? null :
                App.MsalPublicClient.GetAccountsAsync().Result.FirstOrDefault(user => user.Username.Equals(str, StringComparison.OrdinalIgnoreCase));
        }

        private string[] GetScopes()
        {
            return ScopesEntry.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private string GetSelectedUserId()
        {
            if (usersPicker.SelectedIndex == -1)
            {
                return null;
            }

            var selectedUserId = usersPicker.SelectedItem as string;
            return UserNotSelected.Equals(selectedUserId, StringComparison.OrdinalIgnoreCase) ? null : selectedUserId;
        }

        private async void OnAcquireSilentlyClickedAsync(object sender, EventArgs e)
        {
            acquireResponseLabel.Text = "Starting silent token acquisition";
            await Task.Delay(700).ConfigureAwait(true);

            try
            {
                var selectedUser = GetSelectedUserId();
                if (selectedUser == null)
                {
                    acquireResponseLabel.Text = "User was not selected";
                    return;
                }

                var authority = PassAuthoritySwitch.IsToggled ? App.Authority : null;

                var res = await App.MsalPublicClient.AcquireTokenSilentAsync(GetScopes(),
                    getUserByDisplayableId(selectedUser), authority, ForceRefreshSwitch.IsToggled).ConfigureAwait(true);

                var resText = ToString(res);

                if (resText.Contains("AccessToken"))
                {
                    acquireResponseTitleLabel.Text = SuccessfulResult;
                }

                acquireResponseLabel.Text = resText;
            }
            catch (Exception exception)
            {
                CreateExceptionMessage(exception);
            }
        }

        private async void OnAcquireClickedAsync(object sender, EventArgs e)
        {
            try
            {
                acquireResponseTitleLabel.Text = EmptyResult;
                AuthenticationResult res;
                if (LoginHintSwitch.IsToggled)
                {
                    var loginHint = LoginHintEntry.Text.Trim();
                    res =
                        await App.MsalPublicClient.AcquireTokenAsync(
                            GetScopes(),
                            loginHint,
                            GetUIBehavior(),
                            GetExtraQueryParams(),
                            App.UIParent).ConfigureAwait(true);
                }
                else
                {
                    var user = getUserByDisplayableId(GetSelectedUserId());
                    res = await App.MsalPublicClient.AcquireTokenAsync(
                        GetScopes(),
                        user,
                        GetUIBehavior(),
                        GetExtraQueryParams(),
                        App.UIParent).ConfigureAwait(true);
                }

                var resText = ToString(res);

                if (resText.Contains("AccessToken"))
                {
                    acquireResponseTitleLabel.Text = SuccessfulResult;
                }

                acquireResponseLabel.Text = resText;
                RefreshUsers();
            }
            catch (Exception exception)
            {
                CreateExceptionMessage(exception);
            }
        }

        private async void OnAcquireByDeviceCodeClickedAsync(object sender, EventArgs e)
        {
            try
            {
                acquireResponseTitleLabel.Text = EmptyResult;
                AuthenticationResult res =
                        await App.MsalPublicClient.AcquireTokenWithDeviceCodeAsync(
                            GetScopes(),
                            GetExtraQueryParams(),
                            dcr =>
                            {
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    acquireResponseLabel.Text = dcr.Message;
                                });
                                return Task.FromResult(0);
                            }).ConfigureAwait(true);

                var resText = ToString(res);

                if (resText.Contains("AccessToken"))
                {
                    acquireResponseTitleLabel.Text = SuccessfulResult;
                }

                acquireResponseLabel.Text = resText;
                RefreshUsers();
            }
            catch (Exception exception)
            {
                CreateExceptionMessage(exception);
            }
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            acquireResponseLabel.Text = "";
            acquireResponseTitleLabel.Text = EmptyResult;
        }

        private async Task OnClearCacheClickedAsync(object sender, EventArgs e)
        {
            var tokenCache = App.MsalPublicClient.UserTokenCacheInternal;
            var users = tokenCache.GetAccounts(App.Authority);
            foreach (var user in users)
            {
                await App.MsalPublicClient.RemoveAsync(user).ConfigureAwait(true);
            }

            acquireResponseLabel.Text = "";
            acquireResponseTitleLabel.Text = EmptyResult;
        }

        private void CreateExceptionMessage(Exception exception)
        {
            if (exception is MsalException msalException)
            {
                acquireResponseLabel.Text = string.Format(CultureInfo.InvariantCulture, "MsalException -\nError Code: {0}\nMessage: {1}",
                    msalException.ErrorCode, msalException.Message);
            }
            else
            {
                acquireResponseLabel.Text = "Exception - " + exception.Message;
            }

            System.Console.WriteLine(exception.Message);
        }
    }
}


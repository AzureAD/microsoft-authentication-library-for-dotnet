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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AcquirePage : ContentPage
    {
        private const string UserNotSelected = "not selected";

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
            var userIds = App.MsalPublicClient.Users.Select(x => x.DisplayableId)
                .ToList();

            userIds.Add(UserNotSelected);
            usersPicker.ItemsSource = userIds;
            usersPicker.SelectedIndex = 0;
        }

        private void InitUIBehaviorPicker()
        {
            var options = new List<string>
            {
                UIBehavior.SelectAccount.PromptValue,
                UIBehavior.ForceLogin.PromptValue,
                UIBehavior.Consent.PromptValue
            };

            UIBehaviorPicker.ItemsSource = options;
            UIBehaviorPicker.SelectedItem = UIBehavior.SelectAccount.PromptValue;
        }

        private UIBehavior GetUIBehavior()
        {
            var selectedUIBehavior = UIBehaviorPicker.SelectedItem as string;

            if (UIBehavior.ForceLogin.PromptValue.Equals(selectedUIBehavior))
                return UIBehavior.ForceLogin;
            if (UIBehavior.Consent.PromptValue.Equals(selectedUIBehavior))
                return UIBehavior.Consent;

            return UIBehavior.SelectAccount;
        }

        private string GetExtraQueryParams()
        {
            return ExtraQueryParametersEntry.Text.Trim();
        }

        private string ToString(IUser user)
        {
            var sb = new StringBuilder();

            sb.AppendLine("user.DisplayableId : " + user.DisplayableId);
            //sb.AppendLine("user.IdentityProvider : " + user.IdentityProvider);
            sb.AppendLine("user.Name : " + user.Name);

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
            sb.Append(ToString(result.User));

            return sb.ToString();
        }

        private IUser getUserByDisplayableId(string str)
        {
            return string.IsNullOrWhiteSpace(str) ? null : App.MsalPublicClient.Users.FirstOrDefault(user => user.DisplayableId.Equals(str));
        }

        private string[] GetScopes()
        {
            return ScopesEntry.Text.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
        }

        private string GetSelectedUserId()
        {
            if (usersPicker.SelectedIndex == -1) return null;

            var selectedUserId = usersPicker.SelectedItem as string;
            return UserNotSelected.Equals(selectedUserId) ? null : selectedUserId;
        }

        private async void OnAcquireSilentlyClicked(object sender, EventArgs e)
        {
            acquireResponseLabel.Text = "Starting silent token acquisition";
            await Task.Delay(700);

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
                    getUserByDisplayableId(selectedUser), authority, ForceRefreshSwitch.IsToggled);

                acquireResponseLabel.Text = ToString(res);
            }
            catch (MsalException exception)
            {
                acquireResponseLabel.Text = String.Format("MsalException -\nError Code: {0}\nMessage: {1}", exception.ErrorCode, exception.Message);
            }
            catch (Exception exception)
            {
                acquireResponseLabel.Text = "Exception - " + exception.Message;
            }
        }

        private async void OnAcquireClicked(object sender, EventArgs e)
        {
            try
            {
                AuthenticationResult res;
                if (LoginHintSwitch.IsToggled)
                {
                    var loginHint = LoginHintEntry.Text.Trim();
                    res =
                        await App.MsalPublicClient.AcquireTokenAsync(GetScopes(), loginHint, GetUIBehavior(),
                            GetExtraQueryParams(),
                            App.UIParent);
                }
                else
                {
                    var user = getUserByDisplayableId(GetSelectedUserId());
                    res = await App.MsalPublicClient.AcquireTokenAsync(GetScopes(), user, GetUIBehavior(),
                        GetExtraQueryParams(), App.UIParent);
                }

                acquireResponseLabel.Text = ToString(res);
                RefreshUsers();
            }
            catch (MsalException exception)
            {
                acquireResponseLabel.Text = String.Format("MsalException -\nError Code: {0}\nMessage: {1}", exception.ErrorCode, exception.Message);
            }
            catch (Exception exception)
            {
                acquireResponseLabel.Text = "Exception - " + exception.Message;
            }
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            acquireResponseLabel.Text = "";
        }
    }
}


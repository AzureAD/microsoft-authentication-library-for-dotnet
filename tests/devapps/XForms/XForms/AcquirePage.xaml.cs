// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
using System.Threading;
using System.Security;

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

        private Prompt GetPrompt()
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

        private static string GetAccountDescription(IAccount user)
        {
            var sb = new StringBuilder();

            sb.AppendLine("user.DisplayableId : " + user.Username);
            //sb.AppendLine("user.IdentityProvider : " + user.IdentityProvider);
            sb.AppendLine("user.Environment : " + user.Environment);

            return sb.ToString();
        }

        private static string GetResultDescription(AuthenticationResult result)
        {
            var sb = new StringBuilder();

            sb.AppendLine("AccessToken : " + result.AccessToken);
            sb.AppendLine("IdToken : " + result.IdToken);
            sb.AppendLine("ExpiresOn : " + result.ExpiresOn);
            sb.AppendLine("TenantId : " + result.TenantId);
            sb.AppendLine("Scope : " + string.Join(",", result.Scopes));
            sb.AppendLine("User :");
            sb.Append(GetAccountDescription(result.Account));

            return sb.ToString();
        }

        private IAccount GetSelectedAccount()
        {
            string accountId = GetSelectedUsername();
            return string.IsNullOrWhiteSpace(accountId) ? null :
                App.MsalPublicClient
                .GetAccountsAsync().Result
                .FirstOrDefault(user => user.Username.Equals(accountId, StringComparison.OrdinalIgnoreCase));
        }

        private string[] GetScopes()
        {
            return ScopesEntry.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private string GetSelectedUsername()
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
                var selectedUser = GetSelectedUsername();
                if (selectedUser == null)
                {
                    acquireResponseLabel.Text = "User was not selected";
                    return;
                }

                var authority = PassAuthoritySwitch.IsToggled ? App.Authority : null;

                var builder = App.MsalPublicClient.AcquireTokenSilent(GetScopes(), GetSelectedAccount());
                if (PassAuthoritySwitch.IsToggled)
                {
                    builder = builder.WithAuthority(App.Authority);
                }

                var res = await builder
                    .WithForceRefresh(ForceRefreshSwitch.IsToggled)
                    .ExecuteAsync()
                    .ConfigureAwait(true);

                var resText = GetResultDescription(res);

                if (res.AccessToken != null)
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

                var request = App.MsalPublicClient.AcquireTokenInteractive(GetScopes())
                    .WithPrompt(GetPrompt())
                    .WithParentActivityOrWindow(App.AndroidActivity)
                    .WithUseEmbeddedWebView(true)
                    .WithExtraQueryParameters(GetExtraQueryParams());

                var result = await request.ExecuteAsync().ConfigureAwait(true);

                var resText = GetResultDescription(result);

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
                AuthenticationResult res = await App.MsalPublicClient
                    .AcquireTokenWithDeviceCode(
                        GetScopes(),
                        dcr =>
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                acquireResponseLabel.Text = dcr.Message;
                            });
                            return Task.FromResult(0);
                        })
                        .WithExtraQueryParameters(GetExtraQueryParams())
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(true);

                var resText = GetResultDescription(res);

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

        private async void OnAcquireByROPCClickedAsync(object sender, EventArgs e)
        {
            try
            {
                acquireResponseTitleLabel.Text = EmptyResult;

                var request = App.MsalPublicClient.AcquireTokenByUsernamePassword(
                    App.Scopes,
                    UserName.Text.Trim(),
                    ConvertToSecureString(Password.Text.Trim()));

                var result = await request.ExecuteAsync().ConfigureAwait(true);
                var resText = GetResultDescription(result);

                if (resText.Contains("AccessToken"))
                {
                    acquireResponseTitleLabel.Text = SuccessfulResult;
                }

                acquireResponseLabel.Text = resText;
                RefreshUsers();
            }
            catch(Exception exception)
            {
                CreateExceptionMessage(exception);
            }
        }

        private SecureString ConvertToSecureString(string password)
        {
            if (password.Length > 0)
            {
                SecureString securePassword = new SecureString();
                password.ToCharArray().ToList().ForEach(p => securePassword.AppendChar(p));
                securePassword.MakeReadOnly();
                return securePassword;
            }
            return null;
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            acquireResponseLabel.Text = "";
            acquireResponseTitleLabel.Text = EmptyResult;
        }

        private async Task OnClearCacheClickedAsync(object sender, EventArgs e)
        {
            var accounts = await App.MsalPublicClient.GetAccountsAsync().ConfigureAwait(true);

            foreach (var account in accounts)
            {
                await App.MsalPublicClient.RemoveAsync(account).ConfigureAwait(true);
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

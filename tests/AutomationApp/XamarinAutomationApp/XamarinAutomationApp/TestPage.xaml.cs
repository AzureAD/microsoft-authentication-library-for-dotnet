// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinAutomationApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TestPage : ContentPage
    {
        private const string EmptyResult = "Result:";
        private const string SuccessfulResult = "Result: Success";
        private static string s_currentUser = "";
        private bool _isB2CTest = false;

        public TestPage()
        {
            InitializeComponent();
            _isB2CTest = false;
        }

        private void OnPickerSelectedIndexChanged(object sender, EventArgs args)
        {
            var selectedTest = (Picker)sender;
            int selectedIndex = selectedTest.SelectedIndex;

            switch (selectedIndex)
            {
                case 0: // AT Interactive
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 1: // AT Silent
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenSilentAsync().ConfigureAwait(false);
                    break;
                case 2: // Prompt Behavior Consent with Select Account
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.SelectAccount).ConfigureAwait(false);
                    break;
                case 3: // ADFSv3 Federated
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 4: // ADFSv3 NonFederated
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 5: // ADFSv4 Federated
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 6: //ADFSv4 NonFederated
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 7: // ADFSv2019 Federated
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 8: //ADFSv2019 NonFederated
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 9: // B2C Facebook b2clogin.com
                    _isB2CTest = true;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    App.s_authority = App.B2CLoginAuthority;
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 10: // B2C Facebook b2clogin.com edit profile
                    _isB2CTest = true;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    App.s_authority = App.B2cAuthority;
                    AcquireEditProfileTokenAsync().ConfigureAwait(false);
                    break;
                case 11: // B2C Facebook microsoftonline.com
                    _isB2CTest = true;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    App.s_authority = App.B2cAuthority;
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 12: // B2C Local b2clogin.com
                    _isB2CTest = true;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    App.s_authority = App.B2CLoginAuthority;
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 13: // B2C Local microsoftonline.com
                    _isB2CTest = true;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    App.s_authority = App.B2cAuthority;
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 14: // B2C Google b2clogin.com
                    _isB2CTest = true;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    App.s_authority = App.B2CLoginAuthority;
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 15: // B2C Google microsoftonline.com
                    _isB2CTest = true;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    App.s_authority = App.B2cAuthority;
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
            }
        }

        private void CreateDefaultAppSettings()
        {
            App.s_authority = App.DefaultAuthority;
            App.s_scopes = App.s_defaultScopes;
            App.s_clientId = App.DefaultClientId;
        }

        private void CreateB2CAppSettings()
        {
            App.s_scopes = App.s_b2cScopes;
            App.s_clientId = App.B2cClientId;
            App.s_redirectUriOnAndroid = App.RedirectUriB2C;
            App.s_redirectUriOnIos = App.RedirectUriB2C;
        }

        private async Task PrepareTestEnvironmentAsync()
        {
            acquireResponseLabel.Text = "";
            acquireResponseTitleLabel.Text = EmptyResult;

            IEnumerable<IAccount> accounts = await App.s_msalPublicClient.GetAccountsAsync().ConfigureAwait(true);

            foreach (IAccount account in accounts)
            {
                await App.s_msalPublicClient.RemoveAsync(account).ConfigureAwait(true);
            }

            if (_isB2CTest)
            {
                CreateB2CAppSettings();
            }
            else
            {
                CreateDefaultAppSettings();
            }

            App.InitPublicClient();
            _isB2CTest = false;
        }

        private async Task AcquireTokenInteractiveAsync(Prompt prompt)
        {
            try
            {
                AcquireTokenInteractiveParameterBuilder request = App.s_msalPublicClient.AcquireTokenInteractive(App.s_scopes)
                    .WithPrompt(prompt)
                    .WithParentActivityOrWindow(App.AndroidActivity)
                    .WithUseEmbeddedWebView(true);

                AuthenticationResult result = await
                    request.ExecuteAsync().ConfigureAwait(true);

                var resText = GetResultDescription(result);

                if (resText.Contains("AccessToken"))
                {
                    s_currentUser = result.Account.Username;
                    acquireResponseTitleLabel.Text = SuccessfulResult;
                }

                acquireResponseLabel.Text = resText;
            }
            catch (Exception exception)
            {
                CreateExceptionMessage(exception);
            }
        }

        private async Task AcquireTokenSilentAsync()
        {
            try
            {
                AcquireTokenInteractiveParameterBuilder request = App.s_msalPublicClient.AcquireTokenInteractive(App.s_scopes)
                   .WithPrompt(Prompt.ForceLogin)
                   .WithParentActivityOrWindow(App.AndroidActivity)
                   .WithUseEmbeddedWebView(true);

                AuthenticationResult result = await
                    request.ExecuteAsync().ConfigureAwait(true);

                AcquireTokenSilentParameterBuilder builder = App.s_msalPublicClient.AcquireTokenSilent(
                    App.s_scopes, 
                    result.Account.Username);

                AuthenticationResult res = await builder
                    .WithForceRefresh(false)
                    .ExecuteAsync()
                    .ConfigureAwait(true);

                var resText = GetResultDescription(res);

                if (res.AccessToken != null)
                {
                    acquireResponseTitleLabel.Text = SuccessfulResult;
                }

                acquireResponseLabel.Text = "Acquire Token Silent Acquisition Result....\n" + resText;
            }
            catch (Exception exception)
            {
                CreateExceptionMessage(exception);
            }
        }

        private async Task AcquireEditProfileTokenAsync()
        {
            try
            {
                await AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);

                if (s_currentUser == null)
                {
                    acquireResponseLabel.Text = "A user is not currently logged in to the app... ";
                    return;
                }

                // Change the policy to the edit profile policy
                // Set prompt behavior to none
                App.s_authority = App.B2CEditProfilePolicyAuthority;

                AcquireTokenInteractiveParameterBuilder builder = 
                    App.s_msalPublicClient.AcquireTokenInteractive(App.s_scopes)
                        .WithPrompt(Prompt.NoPrompt)
                        .WithParentActivityOrWindow(App.AndroidActivity)
                        .WithUseEmbeddedWebView(true);

                AuthenticationResult res = await builder
                    .ExecuteAsync()
                    .ConfigureAwait(true);

                var resText = GetResultDescription(res);

                if (res.AccessToken != null)
                {
                    acquireResponseTitleLabel.Text = SuccessfulResult;
                }

                acquireResponseLabel.Text = "Starting B2C edit profile test...\n" + resText;
            }
            catch (Exception exception)
            {
                CreateExceptionMessage(exception);
            }
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

        private static string GetAccountDescription(IAccount user)
        {
            var sb = new StringBuilder();

            sb.AppendLine("user.DisplayableId : " + user.Username);
            sb.AppendLine("user.Environment : " + user.Environment);

            return sb.ToString();
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

            Console.WriteLine(exception.Message);
        }
    }
}

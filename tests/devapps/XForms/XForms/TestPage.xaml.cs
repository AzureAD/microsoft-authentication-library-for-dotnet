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

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TestPage : ContentPage
    {
        private const string EmptyResult = "Result:";
        private const string SuccessfulResult = "Result: Success";
        private bool _isB2CTest = false;
        public PublicClientApplication PublicClientApplication { get; set; }

        public TestPage()
        {
            InitializeComponent();
            _isB2CTest = false;
        }

        public void InitPublicClient()
        {
            var builder = PublicClientApplicationBuilder
                .Create(App.s_clientId)
                .WithAuthority(new Uri(App.s_authority), App.s_validateAuthority)
                .WithLogging((level, message, pii) =>
                {
                    Device.BeginInvokeOnMainThread(() => { LogPage.AddToLog("[" + level + "]" + " - " + message, pii); });
                },
                LogLevel.Verbose,
                true);

            // Let Android set its own redirect uri
            switch (Device.RuntimePlatform)
            {
                case "iOS":
                    builder = builder.WithRedirectUri(App.s_redirectUriOnIos);
                    break;
                case "Android":
                    builder = builder.WithRedirectUri(App.s_redirectUriOnAndroid);
                    break;
            }

#if IS_APPCENTER_BUILD
            builder = builder.WithIosKeychainSecurityGroup("*");
#endif
            PublicClientApplication = builder.BuildConcrete();
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
                case 2: // Consent with Select Account
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenWithConsentAndSelectAccountAsync().ConfigureAwait(false);
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
                    App.s_authority = App.B2CLoginAuthority;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 10: // B2C Local b2clogin.com edit profile
                    _isB2CTest = true;
                    App.s_authority = App.B2cAuthority;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireEditProfileTokenAsync().ConfigureAwait(false);
                    break;
                case 11: // B2C Facebook microsoftonline.com
                    _isB2CTest = true;
                    App.s_authority = App.B2cAuthority;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 12: // B2C Local b2clogin.com
                    _isB2CTest = true;
                    App.s_authority = App.B2CLoginAuthority;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 13: // B2C Local microsoftonline.com
                    _isB2CTest = true;
                    App.s_authority = App.B2cAuthority;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 14: // B2C Google b2clogin.com
                    _isB2CTest = true;
                    App.s_authority = App.B2CLoginAuthority;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
                    AcquireTokenInteractiveAsync(Prompt.ForceLogin).ConfigureAwait(false);
                    break;
                case 15: // B2C Google microsoftonline.com
                    _isB2CTest = true;
                    App.s_authority = App.B2cAuthority;
                    PrepareTestEnvironmentAsync().ConfigureAwait(false);
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

            if (PublicClientApplication != null)
            {
                IEnumerable<IAccount> accounts = await PublicClientApplication.GetAccountsAsync().ConfigureAwait(true);

                foreach (IAccount account in accounts)
                {
                    await PublicClientApplication.RemoveAsync(account).ConfigureAwait(true);
                }
            }
            if (_isB2CTest)
            {
                CreateB2CAppSettings();
            }
            else
            {
                CreateDefaultAppSettings();
            }

            InitPublicClient();
            _isB2CTest = false;
        }

        private async Task AcquireTokenInteractiveAsync(Prompt prompt)
        {
            try
            {
                AcquireTokenInteractiveParameterBuilder request = PublicClientApplication.AcquireTokenInteractive(App.s_scopes)
                    .WithPrompt(prompt)
                    .WithParentActivityOrWindow(App.RootViewController)
                    .WithUseEmbeddedWebView(true);

                AuthenticationResult result = await
                    request.ExecuteAsync().ConfigureAwait(true);

                var resText = GetResultDescription(result);

                if (result.AccessToken != null)
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

        private async Task AcquireTokenSilentAsync()
        {
            try
            {
                AcquireTokenInteractiveParameterBuilder request = PublicClientApplication.AcquireTokenInteractive(App.s_scopes)
                   .WithPrompt(Prompt.ForceLogin)
                   .WithParentActivityOrWindow(App.RootViewController)
                   .WithUseEmbeddedWebView(true);

                AuthenticationResult result = await
                    request.ExecuteAsync().ConfigureAwait(true);

                AcquireTokenSilentParameterBuilder builder = PublicClientApplication.AcquireTokenSilent(
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

        private async Task AcquireTokenWithConsentAndSelectAccountAsync()
        {
            try
            {
                AcquireTokenInteractiveParameterBuilder request = PublicClientApplication.AcquireTokenInteractive(App.s_scopes)
                    .WithPrompt(Prompt.Consent)
                    .WithParentActivityOrWindow(App.RootViewController)
                    .WithUseEmbeddedWebView(true);

                AuthenticationResult result = await
                    request.ExecuteAsync().ConfigureAwait(true);

                AcquireTokenInteractiveParameterBuilder request2 = PublicClientApplication.AcquireTokenInteractive(App.s_scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .WithParentActivityOrWindow(App.RootViewController)
                    .WithUseEmbeddedWebView(true);

                AuthenticationResult result2 = await
                   request.ExecuteAsync().ConfigureAwait(true);

                var resText = GetResultDescription(result2);

                if (result2.AccessToken != null)
                {
                    acquireResponseTitleLabel.Text = SuccessfulResult;
                }

                acquireResponseLabel.Text = "Acquire Token Consent and Select Account....\n" + resText;
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
                AcquireTokenInteractiveParameterBuilder request = PublicClientApplication.AcquireTokenInteractive(App.s_scopes)
                    .WithPrompt(Prompt.ForceLogin)
                    .WithParentActivityOrWindow(App.RootViewController)
                    .WithUseEmbeddedWebView(true);

                AuthenticationResult result = await
                    request.ExecuteAsync().ConfigureAwait(true);

                // Change the policy to the edit profile policy
                // Set prompt behavior to none
                App.s_authority = App.B2CEditProfilePolicyAuthority;
                InitPublicClient();

                AcquireTokenInteractiveParameterBuilder builder =
                   PublicClientApplication.AcquireTokenInteractive(App.s_scopes)
                        .WithPrompt(Prompt.NoPrompt)
                        .WithParentActivityOrWindow(App.RootViewController)
                        .WithUseEmbeddedWebView(true);

                AuthenticationResult res = await builder
                    .ExecuteAsync()
                    .ConfigureAwait(true);

                var resText = GetResultDescription(res);

                if (res.AccessToken != null)
                {
                    acquireResponseTitleLabel.Text = SuccessfulResult;
                }

                acquireResponseLabel.Text = "Results from B2C edit profile test...\n" + resText;
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

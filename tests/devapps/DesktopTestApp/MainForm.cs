// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.LabInfrastructure;

namespace DesktopTestApp
{
    public partial class MainForm : Form
    {
        private const string PublicClientId = "6afec070-b576-4a2f-8d95-41f317b28e06";
        private string _b2CClientId = "e3b9ad76-9763-4827-b088-80c7a7888f79";
        public const string B2CCustomDomainClientId = "64a88201-6bbd-49f5-ab46-9153798493fd ";

        private PublicClientHandler _publicClientHandler;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly string[] _b2CScopes = { "https://msidlabb2c.onmicrosoft.com/msidlabb2capi/read" };
        public static string[] B2cCustomDomainScopes = { "https://public.msidlabb2c/b2cwebapp/read" };
        private const string B2CAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_SISOPolicy/";
        private const string B2CEditProfileAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_ProfileEditPolicy/";
        public const string B2CCustomDomainAuthority = "https://public.msidlabb2c.com/tfp/public.msidlabb2c.com/B2C_1_signupsignin_userflow/";
        public const string B2CROPCAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_ROPC_Auth";

        private bool IsForceRefreshEnabled => forceRefreshCheckBox.Checked;

        public MainForm()
        {
            InitializeComponent();
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.Selecting += TabControl1_Selecting;
            logLevel.SelectedIndex = logLevel.Items.Count - 1;
            userPasswordTextBox.PasswordChar = '*';
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _publicClientHandler = new PublicClientHandler(PublicClientId, LogDelegate);
            LoadSettings();
        }


        public void LogDelegate(LogLevel level, string message, bool containsPii)
        {
            Action action = null;

            if (containsPii)
            {
                action = () =>
                {
                    msalPIILogsTextBox.AppendText(message + Environment.NewLine);
                };
            }
            else
            {
                action = () =>
                {
                    msalLogsTextBox.AppendText(message + Environment.NewLine);
                };
            }

            BeginInvoke(new MethodInvoker(action));
        }

        public void RefreshUserList()
        {
            List<IAccount> accounts = _publicClientHandler.PublicClientApplication.GetAccountsAsync().Result.ToList();

            userList.DataSource = accounts;
            userList.Refresh();
        }

        #region PublicClient UI Controls

        private void loginHint_TextChanged(object sender, EventArgs e)
        {
            _publicClientHandler.LoginHint = loginHintTextBox.Text;
        }

        private void userList_SelectedIndexChanged(object sender, EventArgs e)
        {
            _publicClientHandler.CurrentUser = (IAccount)userList.SelectedItem;
        }

        private void overriddenAuthority_TextChanged(object sender, EventArgs e)
        {
            _publicClientHandler.AuthorityOverride = overriddenAuthority.Text;
        }

        private void acquire_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = publicClientTabPage;
            RefreshUserList();
        }

        private void settings_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = settingsTabPage;
        }

        private void cache_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = cacheTabPage;
            LoadCacheTabPageAsync().ConfigureAwait(true);
        }

        private void logs_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = logsTabPage;
        }

        #endregion

        #region PublicClientApplication Acquire Token
        private async void AcquireTokenInteractive_Click(object sender, EventArgs e)
        {
            using (new UIProgressScope(this))
            {
                ClearResultPageInfo();
                _publicClientHandler.ApplicationId = PublicClientId;
                _publicClientHandler.LoginHint = loginHintTextBox.Text;
                _publicClientHandler.AuthorityOverride = overriddenAuthority.Text;
                _publicClientHandler.InteractiveAuthority = authority.Text;

                if (IgnoreUserCbx.Checked)
                {
                    _publicClientHandler.CurrentUser = null;
                }
                else
                {
                    _publicClientHandler.CurrentUser = userList.SelectedItem as Account;
                }

                try
                {
                    AuthenticationResult authenticationResult = await _publicClientHandler.AcquireTokenInteractiveAsync(
                        SplitScopeString(scopes.Text),
                        GetUIBehavior(),
                        _publicClientHandler.ExtraQueryParams).ConfigureAwait(true);

                    SetResultPageInfo(authenticationResult);
                    RefreshUserList();
                }
                catch (Exception exc)
                {
                    CreateException(exc);
                }
            }
        }

        private async void acquireTokenByWindowsIntegratedAuth_Click(object sender, EventArgs e)
        {
            using (new UIProgressScope(this))
            {
                ClearResultPageInfo();
                string username = loginHintTextBox.Text; // Can be blank

                try
                {
                    AuthenticationResult authenticationResult = await _publicClientHandler.PublicClientApplication
                        .AcquireTokenByIntegratedWindowsAuth(SplitScopeString(scopes.Text))
                        .WithUsername(username)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(true);

                    SetResultPageInfo(authenticationResult);
                    RefreshUserList();

                }
                catch (Exception exc)
                {
                    CreateException(exc);
                }
            }
        }

        private async void acquireTokenByUPButton_Click(object sender, EventArgs e)
        {
            try
            {
                _publicClientHandler.PublicClientApplication = PublicClientApplicationBuilder
                    .Create(PublicClientId)
                    .WithAuthority("https://login.microsoftonline.com/organizations")
                    .BuildConcrete();

                await AcquireTokenByUsernamePasswordAsync().ConfigureAwait(true);
            }
            catch (Exception exc)
            {
                CreateException(exc);
            }
        }

        private async Task AcquireTokenByUsernamePasswordAsync()
        {
            try
            {
                ClearResultPageInfo();
                userPasswordTextBox.PasswordChar = '*';

                string username = loginHintTextBox.Text; //Can be blank for U/P
                SecureString securePassword = ConvertToSecureString(userPasswordTextBox);

                AuthenticationResult authResult = await _publicClientHandler.PublicClientApplication
                    .AcquireTokenByUsernamePassword(
                        SplitScopeString(scopes.Text),
                        username,
                        securePassword)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                SetResultPageInfo(authResult);
                RefreshUserList();
            }
            catch (Exception exc)
            {
                CreateException(exc);
            }
        }

        private SecureString ConvertToSecureString(TextBox textBox)
        {
            if (userPasswordTextBox.Text.Length > 0)
            {
                SecureString securePassword = new SecureString();
                userPasswordTextBox.Text.ToCharArray().ToList().ForEach(p => securePassword.AppendChar(p));
                securePassword.MakeReadOnly();
                return securePassword;
            }
            return null;
        }

        private async void acquireTokenSilent_Click(object sender, EventArgs e)
        {
            using (new UIProgressScope(this))
            {
                ClearResultPageInfo();

                _publicClientHandler.ApplicationId = PublicClientId;
                _publicClientHandler.AuthorityOverride = overriddenAuthority.Text;
                if (IgnoreUserCbx.Checked)
                {
                    _publicClientHandler.CurrentUser = null;
                }
                else
                {
                    _publicClientHandler.CurrentUser = userList.SelectedItem as Account;
                }

                try
                {
                    AuthenticationResult authenticationResult =
                        await _publicClientHandler.AcquireTokenSilentAsync(SplitScopeString(scopes.Text), IsForceRefreshEnabled).ConfigureAwait(true);

                    SetResultPageInfo(authenticationResult);
                }
                catch (Exception exc)
                {
                    CreateException(exc);
                }
            }
        }

        private async void acquireTokenInteractiveAuthority_Click(object sender, EventArgs e)
        {
            ClearResultPageInfo();
            _publicClientHandler.LoginHint = loginHintTextBox.Text;
            _publicClientHandler.AuthorityOverride = overriddenAuthority.Text;
            _publicClientHandler.InteractiveAuthority = authority.Text;

            if (IgnoreUserCbx.Checked)
            {
                _publicClientHandler.CurrentUser = null;
            }
            else
            {
                _publicClientHandler.CurrentUser = userList.SelectedItem as Account;
            }

            try
            {
                AuthenticationResult authenticationResult = await _publicClientHandler
                    .AcquireTokenInteractiveWithAuthorityAsync(
                        SplitScopeString(scopes.Text),
                        GetUIBehavior(),
                        _publicClientHandler.ExtraQueryParams).ConfigureAwait(true);

                SetResultPageInfo(authenticationResult);
            }
            catch (Exception exc)
            {
                CreateException(exc);
            }
        }
        #endregion

        private void CreateException(Exception ex)
        {
            string output = string.Empty;

            if (ex is MsalException exception)
            {
                output += string.Format(
                    CultureInfo.InvariantCulture,
                    "Error Code - {0}" + Environment.NewLine + "Message - {1}" + Environment.NewLine,
                    exception.ErrorCode,
                    exception.Message);

                if (exception is MsalServiceException)
                {
                    output += string.Format(CultureInfo.InvariantCulture, "Status Code - {0}" + Environment.NewLine, ((MsalServiceException)exception).StatusCode);
                    output += string.Format(CultureInfo.InvariantCulture, "Claims - {0}" + Environment.NewLine, ((MsalServiceException)exception).Claims);
                    output += string.Format(CultureInfo.InvariantCulture, "Raw Response - {0}" + Environment.NewLine, ((MsalServiceException)exception).ResponseBody);
                }
            }
            else
            {
                output = ex.Message + Environment.NewLine + ex.StackTrace;
            }


            callResult.Text = output;
        }

        private Prompt GetUIBehavior()
        {
            Prompt behavior = Prompt.SelectAccount;

            if (forceLogin.Checked)
            {
                behavior = Prompt.ForceLogin;
            }

            if (never.Checked)
            {
                behavior = Prompt.Never;
            }

            if (consent.Checked)
            {
                behavior = Prompt.Consent;
            }

            if (noPrompt.Checked)
            {
                behavior = Prompt.NoPrompt;
            }

            return behavior;
        }

        #region App logic

        public void SetResultPageInfo(AuthenticationResult authenticationResult)
        {
            callResult.Text = @"Access Token: " + authenticationResult.AccessToken + Environment.NewLine +
                              @"Expires On: " + authenticationResult.ExpiresOn + Environment.NewLine +
                              @"Tenant Id: " + authenticationResult.TenantId + Environment.NewLine +
                              @"User: " + authenticationResult.Account.Username + Environment.NewLine +
                              @"Id Token: " + authenticationResult.IdToken;
        }

        public void ClearResultPageInfo()
        {
            callResult.Text = string.Empty;
        }

        #endregion

        #region Cache Tab Operations
        private async Task LoadCacheTabPageAsync()
        {
            while (cachePageTableLayout.Controls.Count > 0)
            {
                cachePageTableLayout.Controls[0].Dispose();
            }

            // Bring the cache back into memory
            var acc = _publicClientHandler.PublicClientApplication.GetAccountsAsync().Result;
            Trace.WriteLine("Accounts: " + acc.Count());

            cachePageTableLayout.RowCount = 0;
            var allRefreshTokens = await _publicClientHandler
                .PublicClientApplication
                .UserTokenCacheInternal
                .GetAllRefreshTokensAsync(true)
                .ConfigureAwait(true);

            var allAccessTokens = await _publicClientHandler
                .PublicClientApplication
                .UserTokenCacheInternal
                .GetAllAccessTokensAsync(true)
                .ConfigureAwait(true);

            foreach (MsalRefreshTokenCacheItem rtItem in allRefreshTokens)
            {
                AddControlToCachePageTableLayout(
                    new MsalUserRefreshTokenControl(_publicClientHandler.PublicClientApplication, rtItem)
                    {
                        RefreshViewAsyncDelegate = LoadCacheTabPageAsync
                    });

                foreach (MsalAccessTokenCacheItem atItem in allAccessTokens)
                {
                    if (atItem.HomeAccountId.Equals(rtItem.HomeAccountId, StringComparison.OrdinalIgnoreCase))
                    {
                        AddControlToCachePageTableLayout(
                            new MsalUserAccessTokenControl(_publicClientHandler.PublicClientApplication.UserTokenCacheInternal,
                                atItem)
                            {
                                RefreshViewAsyncDelegate = LoadCacheTabPageAsync
                            });
                    }
                }
            }
        }

        private void AddControlToCachePageTableLayout(Control ctl)
        {
            cachePageTableLayout.RowCount += 1;
            cachePageTableLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize, ctl.Height));
            ctl.Dock = DockStyle.Fill;
            cachePageTableLayout.Controls.Add(ctl, 0, cachePageTableLayout.RowCount - 1);
            foreach (RowStyle rs in cachePageTableLayout.RowStyles)
            {
                rs.Height = ctl.Height;
            }
        }
        #endregion

        #region Settings Tab Operations
        private void TabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            //tab page is not settings tab. Apply values from settings page.
            if (tabControl1.SelectedIndex != 2)
            {
                LoadSettings();
            }
        }

        private void LoadSettings()
        {
            _publicClientHandler.ExtraQueryParams = extraQueryParams.Text;
            Environment.SetEnvironmentVariable("MsalExtraQueryParameter", environmentQP.Text);
        }

        #endregion

        private void clearLogsButton_Click(object sender, EventArgs e)
        {
            msalLogsTextBox.Text = string.Empty;
            msalPIILogsTextBox.Text = string.Empty;
        }

        private void authority_FocusLeave(object sender, EventArgs e)
        {
            _publicClientHandler.CreateOrUpdatePublicClientApp(this.authority.Text, PublicClientId);
        }

        private async void acquireTokenDeviceCode_Click(object sender, EventArgs e)
        {
            ClearResultPageInfo();

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                AuthenticationResult authenticationResult = await _publicClientHandler.PublicClientApplication
                    .AcquireTokenWithDeviceCode(
                        SplitScopeString(scopes.Text),
                        dcr =>
                        {
                            BeginInvoke(new MethodInvoker(() => callResult.Text = dcr.Message));
                            return Task.FromResult(0);
                        })
                    .ExecuteAsync(_cancellationTokenSource.Token)
                    .ConfigureAwait(true);

                SetResultPageInfo(authenticationResult);
            }
            catch (Exception ex)
            {
                CreateException(ex);
            }
        }

        private void cancelOperationButton_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        private IEnumerable<string> SplitScopeString(string scopes)
        {
            if (string.IsNullOrWhiteSpace(scopes))
            {
                return new string[] { };
            }

            return scopes.Split(new[] { " " }, StringSplitOptions.None);
        }

        private async void b2cLogin_Click(object sender, EventArgs e)
        {
            using (new UIProgressScope(this))
            {
                await GetB2CClientIdFromLabAsync().ConfigureAwait(false);

                ClearResultPageInfo();

                _publicClientHandler.InteractiveAuthority = B2CAuthority;
                _publicClientHandler.ApplicationId = _b2CClientId;

                try
                {
                    AuthenticationResult authenticationResult = await _publicClientHandler.AcquireTokenInteractiveAsync(
                        _b2CScopes,
                        GetUIBehavior(),
                        _publicClientHandler.ExtraQueryParams).ConfigureAwait(true);

                    SetResultPageInfo(authenticationResult);
                    RefreshUserList();
                }
                catch (Exception exc)
                {
                    CreateException(exc);
                }
            }
        }

        private async void b2cEditProfile_Click(object sender, EventArgs e)
        {
            using (new UIProgressScope(this))
            {
                await GetB2CClientIdFromLabAsync().ConfigureAwait(false);

                ClearResultPageInfo();

                _publicClientHandler.InteractiveAuthority = B2CEditProfileAuthority;
                _publicClientHandler.ApplicationId = _b2CClientId;

                try
                {
                    AuthenticationResult authenticationResult = await _publicClientHandler.AcquireTokenInteractiveAsync(
                        _b2CScopes,
                        GetUIBehavior(),
                        _publicClientHandler.ExtraQueryParams).ConfigureAwait(true);

                    SetResultPageInfo(authenticationResult);
                    RefreshUserList();
                }
                catch (Exception exc)
                {
                    CreateException(exc);
                }
            }
        }

        private async void b2cSilentFlow_Click(object sender, EventArgs e)
        {
            using (new UIProgressScope(this))
            {
                ClearResultPageInfo();

                _publicClientHandler.InteractiveAuthority = B2CAuthority;
                _publicClientHandler.ApplicationId = _b2CClientId;

                try
                {
                    AuthenticationResult authenticationResult =
                        await _publicClientHandler.AcquireTokenSilentAsync(_b2CScopes, IsForceRefreshEnabled).ConfigureAwait(true);

                    SetResultPageInfo(authenticationResult);
                }
                catch (Exception exc)
                {
                    CreateException(exc);
                }
            }
        }

        private async void B2cCustomDomain_Click(object sender, EventArgs e)
        {
            using (new UIProgressScope(this))
            {
                ClearResultPageInfo();

                _publicClientHandler.InteractiveAuthority = B2CCustomDomainAuthority;
                _publicClientHandler.ApplicationId = B2CCustomDomainClientId;

                try
                {
                    AuthenticationResult authenticationResult =
                        await _publicClientHandler.AcquireTokenInteractiveWithB2CAuthorityAsync(
                            B2cCustomDomainScopes,
                            Prompt.SelectAccount,
                            null,
                            B2CCustomDomainAuthority).ConfigureAwait(true);

                    SetResultPageInfo(authenticationResult);
                }
                catch (Exception exc)
                {
                    CreateException(exc);
                }
            }
        }

        private async void B2cSilentCustomDomain_Click(object sender, EventArgs e)
        {
            using (new UIProgressScope(this))
            {
                ClearResultPageInfo();

                _publicClientHandler.InteractiveAuthority = B2CCustomDomainAuthority;
                _publicClientHandler.ApplicationId = B2CCustomDomainClientId;

                try
                {
                    AuthenticationResult authenticationResult =
                        await _publicClientHandler.AcquireTokenSilentAsync(
                            B2cCustomDomainScopes,
                            IsForceRefreshEnabled).ConfigureAwait(true);

                    SetResultPageInfo(authenticationResult);
                }
                catch (Exception exc)
                {
                    CreateException(exc);
                }
            }
        }

        private async void RopcB2CAT_Click(object sender, EventArgs e)
        {
            try
            {
                _publicClientHandler.PublicClientApplication = PublicClientApplicationBuilder
                   .Create(_b2CClientId)
                   .WithB2CAuthority(B2CROPCAuthority)
                   .BuildConcrete();
                await AcquireTokenByUsernamePasswordAsync().ConfigureAwait(true);
            }
            catch (Exception exc)
            {
                CreateException(exc);
            }
        }
        
        private async Task GetB2CClientIdFromLabAsync()
        {
            if (_b2CClientId != null)
            {
                return;
            }
            LabResponse labResponse = await LabUserHelper.GetB2CLocalAccountAsync().ConfigureAwait(false);
            _b2CClientId = labResponse.App.AppId;
        }
    }
}

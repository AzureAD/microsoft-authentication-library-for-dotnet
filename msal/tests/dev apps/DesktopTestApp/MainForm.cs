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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;

namespace DesktopTestApp
{
    public partial class MainForm : Form
    {
        private const string publicClientId = "0615b6ca-88d4-4884-8729-b178178f7c27";

        private readonly PublicClientHandler _publicClientHandler = new PublicClientHandler(publicClientId);

        public MainForm()
        {
            InitializeComponent();
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.Selecting += TabControl1_Selecting;
            logLevel.SelectedIndex = logLevel.Items.Count - 1;

            LoadSettings();
            Logger.LogCallback = LogDelegate;
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

            this.BeginInvoke(new MethodInvoker(action));
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
            LoadCacheTabPage();
        }

        private void logs_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = logsTabPage;
        }

        #endregion

        #region PublicClientApplication Acquire Token
        private async void AcquireTokenInteractive_Click(object sender, EventArgs e)
        {
            ClearResultPageInfo();
            _publicClientHandler.LoginHint = loginHintTextBox.Text;
            _publicClientHandler.AuthorityOverride = overriddenAuthority.Text;
            _publicClientHandler.InteractiveAuthority = authority.Text;

            if (userList.SelectedIndex == 0)
            {
                _publicClientHandler.CurrentUser = null;
            }
            else
            {
                _publicClientHandler.CurrentUser = userList.SelectedItem as Account;
            }

            try
            {
                AuthenticationResult authenticationResult = await _publicClientHandler.AcquireTokenInteractiveAsync(scopes.Text.AsArray(), GetUIBehavior(), _publicClientHandler.ExtraQueryParams, new UIParent());

                SetResultPageInfo(authenticationResult);
                RefreshUserList();
            }
            catch (Exception exc)
            {
                CreateException(exc);
            }
        }

        private async void acquireTokenByWindowsIntegratedAuth_Click(object sender, EventArgs e)
        {
            ClearResultPageInfo();
            try
            {
                var app = new PublicClientApplication(publicClientId, authority.Text);
                AuthenticationResult authenticationResult = await app.AcquireTokenByIntegratedWindowsAuthAsync(scopes.Text.AsArray());
                SetResultPageInfo(authenticationResult);
            }
            catch (Exception exc)
            {
                CreateException(exc);
            }
        }

        private async void acquireTokenSilent_Click(object sender, EventArgs e)
        {
            ClearResultPageInfo();

            _publicClientHandler.AuthorityOverride = overriddenAuthority.Text;
            if (userList.SelectedIndex == 0)
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
                    await _publicClientHandler.AcquireTokenSilentAsync(scopes.Text.AsArray());

                SetResultPageInfo(authenticationResult);
            }
            catch (Exception exc)
            {
                CreateException(exc);
            }
        }

        private async void acquireTokenInteractiveAuthority_Click(object sender, EventArgs e)
        {
            ClearResultPageInfo();
            _publicClientHandler.LoginHint = loginHintTextBox.Text;
            _publicClientHandler.AuthorityOverride = overriddenAuthority.Text;
            _publicClientHandler.InteractiveAuthority = authority.Text;

            if (userList.SelectedIndex == 0)
            {
                _publicClientHandler.CurrentUser = null;
            }
            else
            {
                _publicClientHandler.CurrentUser = userList.SelectedItem as Account;
            }

            try
            {
                AuthenticationResult authenticationResult = await _publicClientHandler.AcquireTokenInteractiveWithAuthorityAsync(scopes.Text.AsArray(), GetUIBehavior(), _publicClientHandler.ExtraQueryParams, new UIParent());

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
            MsalException exception = ex as MsalException;

            if (exception != null)
            {
                output += string.Format("Error Code - {0}" + Environment.NewLine + "Message - {1}" + Environment.NewLine, exception.ErrorCode, exception.Message);
                if (exception is MsalServiceException)
                {
                    output += string.Format("Status Code - {0}" + Environment.NewLine, ((MsalServiceException)exception).StatusCode);
                    output += string.Format("Claims - {0}" + Environment.NewLine, ((MsalServiceException)exception).Claims);
                    output += string.Format("Raw Response - {0}" + Environment.NewLine, ((MsalServiceException)exception).ResponseBody);
                }
            }
            else
            {
                output = ex.Message + Environment.NewLine + ex.StackTrace;
            }


            callResult.Text = output;
        }

        private UIBehavior GetUIBehavior()
        {
            UIBehavior behavior = UIBehavior.SelectAccount;

            if (forceLogin.Checked)
            {
                behavior = UIBehavior.ForceLogin;
            }

            if (never.Checked)
            {
                behavior = UIBehavior.Never;
            }

            if (consent.Checked)
            {
                behavior = UIBehavior.Consent;
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
        private void LoadCacheTabPage()
        {
            while (cachePageTableLayout.Controls.Count > 0)
            {
                cachePageTableLayout.Controls[0].Dispose();
            }
            
            // Bring the cache back into memory
            var acc = _publicClientHandler.PublicClientApplication.GetAccountsAsync().Result;
            Trace.WriteLine("Accounts: " + acc.Count());

            cachePageTableLayout.RowCount = 0;
            var allRefreshTokens = _publicClientHandler.PublicClientApplication.UserTokenCache
                .GetAllRefreshTokensForClient(new RequestContext(new MsalLogger(Guid.NewGuid(), null)));
            var allAccessTokens = _publicClientHandler.PublicClientApplication.UserTokenCache
                    .GetAllAccessTokensForClient(new RequestContext(new MsalLogger(Guid.NewGuid(), null)));

            foreach (MsalRefreshTokenCacheItem rtItem in allRefreshTokens)
            {
                AddControlToCachePageTableLayout(
                    new MsalUserRefreshTokenControl(_publicClientHandler.PublicClientApplication, rtItem)
                    {
                        RefreshViewDelegate = LoadCacheTabPage
                    });

                foreach (MsalAccessTokenCacheItem atItem in allAccessTokens)
                {
                    if (atItem.HomeAccountId.Equals(rtItem.HomeAccountId, StringComparison.OrdinalIgnoreCase))
                    {
                        AddControlToCachePageTableLayout(
                            new MsalUserAccessTokenControl(_publicClientHandler.PublicClientApplication.UserTokenCache,
                                atItem)
                            {
                                RefreshViewDelegate = LoadCacheTabPage
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

            Logger.Level = (LogLevel)Enum.Parse(typeof(LogLevel), (string)logLevel.SelectedItem);
            Logger.PiiLoggingEnabled = PiiLoggingEnabled.Checked;
        }

        #endregion

        private void forceRefreshTrueBtn_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void clearLogsButton_Click(object sender, EventArgs e)
        {
            msalLogsTextBox.Text = string.Empty;
            msalPIILogsTextBox.Text = string.Empty;
        }
    }
}
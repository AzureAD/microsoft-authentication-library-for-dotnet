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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;

namespace DesktopTestApp
{
    public partial class MainForm : Form
    {
        readonly LoggerCallback myCallback = new LoggerCallback();

        public MainForm()
        {
            //ClearResultPageInfo();

            InitializeComponent();
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;

            Logger.Callback = myCallback;
            Logger.Level = Logger.LogLevel.Info;
            userList.DataSource = new PublicClientApplication(
                "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc")
            { UserTokenCache = TokenCacheHelper.GetCache() }.Users.ToList();
        }

        private void acquire_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = publicClientTabPage;
        }

        private void settings_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = settingsTabPage;
        }

        private void cache_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = cacheTabPage;
        }

        private void logs_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = logsTabPage;
        }
        private void confidentialClient_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = confidentialClientTabPage;
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private async void acquireTokenInteractive_Click(object sender, EventArgs e)
        {
            ClearResultPageInfo();

            PublicClientApplication clientApplication = CreateClientApplication();
            string output = string.Empty;
            callResult.Text = output;
            try
            {
                IAuthenticationResult result;
                if (userList.SelectedIndex != -1)
                {
                    result = await clientApplication.AcquireTokenAsync(scopes.Text.Split(' '),
                        (User)userList.SelectedItem, GetUIBehavior(), extraQueryParams.Text);
                }
                else
                {
                    result = await clientApplication.AcquireTokenAsync(scopes.Text.Split(' '), loginHint.Text,
                        GetUIBehavior(), extraQueryParams.Text);
                }

                SetResultPageInfo(result);
            }
            catch (Exception exc)
            {
                MsalServiceException exception = exc as MsalServiceException;

                if (exception != null)
                {
                   output = exception.ErrorCode;
                }

                output = exc.Message + Environment.NewLine + exc.StackTrace;
            }
            finally
            {
                callResult.Text = output;
                RefreshUI();
            }
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

        private PublicClientApplication CreateClientApplication()
        {
            PublicClientApplication clientApplication = null;
            if (!string.IsNullOrEmpty(overridenAuthority.Text))
            {
                clientApplication = new PublicClientApplication(
                    "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc");
            }
            else
            {
                clientApplication = new PublicClientApplication(
                    "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc", authority.Text);
            }

            return clientApplication;
        }

        private void applySettings_Click(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("ExtraQueryParameters", environmentQP.Text);
        }

        private async void acquireTokenSilent_Click(object sender, EventArgs e)
        {
            PublicClientApplication clientApplication = CreateClientApplication();
            string output = string.Empty;
            callResult.Text = output;
            try
            {
                IAuthenticationResult result;
                if (userList.SelectedIndex != -1)
                {
                    result = await clientApplication.AcquireTokenAsync(scopes.Text.Split(' '),
                        (User)userList.SelectedItem, GetUIBehavior(), extraQueryParams.Text);
                }
                else
                {
                    result = await clientApplication.AcquireTokenAsync(scopes.Text.Split(' '), loginHint.Text,
                        GetUIBehavior(), extraQueryParams.Text);
                }

                output = JsonHelper.SerializeToJson(result);
            }
            catch (Exception exc)
            {
                if (exc is MsalServiceException)
                {
                    output += ((MsalServiceException)exc).ErrorCode;
                }

                output = exc.Message + Environment.NewLine + exc.StackTrace;
            }
            finally
            {
                callResult.Text = output;
                RefreshUI();
            }

        }

        private void RefreshUI()
        {
            msalPIILogs.Text = myCallback.DrainPiiLogs();
            msalLogs.Text = myCallback.DrainLogs();
            userList.DataSource = new PublicClientApplication(
                    "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc")
            { UserTokenCache = TokenCacheHelper.GetCache() }.Users.ToList();
        }

        #region App logic
        private void SetResultPageInfo(IAuthenticationResult authenticationResult)
        {
            AccessTokenResult.Text = authenticationResult.AccessToken;
            ExpiresOnResult.Text = authenticationResult.ExpiresOn.ToString();
            TenantIdResult.Text = authenticationResult.TenantId;
            UserResult.Text = authenticationResult.User.DisplayableId;
            IdTokenResult.Text = authenticationResult.IdToken;
            ScopeResult.DataSource = authenticationResult.Scope;
        }

        private void ClearResultPageInfo()
        {
            AccessTokenResult.Text = string.Empty;
            ExpiresOnResult.Text = string.Empty;
            TenantIdResult.Text = string.Empty;
            UserResult.Text = string.Empty;
            IdTokenResult.Text = string.Empty;
            ScopeResult.DataSource = null;
        }
        #endregion
    }
}
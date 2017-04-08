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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;

namespace DesktopTestApp
{
    public partial class MainForm : Form
    {
        readonly LoggerCallback myCallback = new LoggerCallback();

        #region Properties

        private PublicClientApplication _publicClientApplication = new PublicClientApplication(
            clientId: "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc")
        {
            //UserTokenCache = TokenCacheHelper.GetCache()
        };
        private ConfidentialClientApplication _confidentialClientApplication;

        public IUser CurrentUser { get; set; }

        #endregion

        public MainForm()
        {
            InitializeComponent();
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;

            Logger.Callback = myCallback;
            Logger.Level = Logger.LogLevel.Info;
            PiiLogging();

            ResetUserList(addFakeUsers: false);
        }

        private void ResetUserList(bool addFakeUsers)
        {
            List<IUser> userListDataSource = _publicClientApplication.Users.ToList();
           
            userList.DataSource = userListDataSource;
            usersListBox.DataSource = userListDataSource;
            userList.Refresh();
            usersListBox.Refresh();
        }
        #region UI Controls

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

        #endregion

        #region Public Client Acquire Token Logic

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
                    // if (modalWebview.Checked)
                    // {
                    result = await clientApplication.AcquireTokenAsync(scopes.Text.Split(' '),
                        (User)userList.SelectedItem, GetUIBehavior(), extraQueryParams.Text, new UIParent(/*this*/));
                    // }
                    // else
                    //  {
                    //     result = await clientApplication.AcquireTokenAsync(scopes.Text.Split(' '),
                    //         (User) userList.SelectedItem, GetUIBehavior(), extraQueryParams.Text);
                    //  }
                }
                else
                {
                    // if (modalWebview.Checked)
                    // {
                    //     result = await clientApplication.AcquireTokenAsync(scopes.Text.Split(' '), loginHint.Text,
                    //        GetUIBehavior(), extraQueryParams.Text, new UIParent(this));
                    // }
                    //  else
                    //  {
                    string[] scopeArray = scopes.Text.Split(' ');
                    UIBehavior uiBehavior = GetUIBehavior();
                    result = await clientApplication.AcquireTokenAsync(scopeArray, loginHint.Text, uiBehavior, extraQueryParams.Text);
                    // }
                }

                CurrentUser = result.User;
                SetResultPageInfo(result);
                ResetUserList(addFakeUsers: true);
            }
            catch (Exception exc)
            {
                MsalServiceException exception = exc as MsalServiceException;

                if (exception != null)
                {
                    output = exc.Message + Environment.NewLine + exc.StackTrace;
                }
                SetErrorPageInfo(output);
            }
            finally
            {
                RefreshUI();
            }
        }

        private async void acquireTokenSilent_Click(object sender, EventArgs e)
        {
            ClearResultPageInfo();

            string output = string.Empty;
            callResult.Text = output;

            try
            {
                IAuthenticationResult result =
                    await _publicClientApplication.AcquireTokenSilentAsync(scopes.Text.Split(' '), CurrentUser);

                SetResultPageInfo(result);
            }
            catch (Exception exc)
            {
                MsalServiceException exception = exc as MsalServiceException;
                if (exception != null)
                {
                    output = exc.Message + Environment.NewLine + exc.StackTrace;
                }

                SetErrorPageInfo(output);
            }
            finally
            {
                RefreshUI();
            }
        }

        #endregion

        #region Confidential Client Acquire Token Logic
        private async void confClientAcquireTokenBtn_Click_1(object sender, EventArgs e)
        {
            ClearConfidentialClientResultPageInfo();
            callResultConfClient.SendToBack();

            ConfidentialClientApplication clientApplication = CreateConfidentialClientApplication();
            string output = string.Empty;
            callResultConfClient.Text = output;
            try
            {
                IAuthenticationResult result;
                if (confClientUserList.SelectedIndex != -1)
                {
                    result = await clientApplication.AcquireTokenForClientAsync(confClientScopesTextBox.Text.Split(' '));
                }
                else
                {
                    result = await clientApplication.AcquireTokenForClientAsync(confClientScopesTextBox.Text.Split(' '), true);
                }
                CurrentUser = result.User;
                SetConfidentialClientPageInfo(result);
            }
            catch (Exception exc)
            {
                MsalServiceException exception = exc as MsalServiceException;

                if (exception != null)
                {
                    output = exception.ErrorCode;
                }

                output = exc.Message + Environment.NewLine + exc.StackTrace;

                SetConfidentClientErrorPageInfo(output);
            }
            finally
            {
                callResultConfClient.Text = output;
                RefreshUI();
            }
        }


        #endregion

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
            if (_publicClientApplication != null) return _publicClientApplication;

            if (!string.IsNullOrEmpty(overriddenAuthority.Text))
            {
                _publicClientApplication = new PublicClientApplication(
                    "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc");
            }
            else
            {
                _publicClientApplication = new PublicClientApplication(
                    "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc", authority.Text);
            }

            return _publicClientApplication;
        }

        private ConfidentialClientApplication CreateConfidentialClientApplication()
        {
            if (_confidentialClientApplication != null) return _confidentialClientApplication;

            ClientCredential clientCredential = new ClientCredential(confClientTextBox.Text);

            if (!string.IsNullOrEmpty(overriddenAuthority.Text))
            {
                _confidentialClientApplication = new ConfidentialClientApplication(
                    "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc",
                    "https://localhost:", clientCredential,
                    TokenCacheHelper.GetCache(), TokenCacheHelper.GetCache());
            }
            else
            {
                _confidentialClientApplication = new ConfidentialClientApplication(
                    "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc", authority.Text,
                    "https://localhost:", clientCredential,
                    TokenCacheHelper.GetCache(), TokenCacheHelper.GetCache());
            }
            return _confidentialClientApplication;
        }

        private void applySettings_Click(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("ExtraQueryParameters", environmentQP.Text);
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
            callResult.Text = @"Access Token: " + authenticationResult.AccessToken + Environment.NewLine +
                              @"Expires On: " + authenticationResult.ExpiresOn + Environment.NewLine +
                              @"Tenant Id: " + authenticationResult.TenantId + Environment.NewLine + @"User: " +
                              authenticationResult.User.DisplayableId + Environment.NewLine +
                              @"Id Token: " + authenticationResult.IdToken;
        }

        private void SetErrorPageInfo(string errorMessage)
        {
            callResult.Text = errorMessage;
        }

        private void ClearResultPageInfo()
        {
            callResult.Text = string.Empty;
        }

        private void SetConfidentialClientPageInfo(IAuthenticationResult authenticationResult)
        {
            confClientAccessTokenResult.Text = authenticationResult.AccessToken;
            //TODO: result in cache
            confClientExpiresOnResult.Text = authenticationResult.ExpiresOn.ToString();
            //TODO: Expires on in cache
            confClientTenantIdResult.Text = authenticationResult.TenantId;
            //TODO: User result in cache
            confClientUserResult.Text = authenticationResult.User.DisplayableId;
            confClientIdTokenResult.Text = authenticationResult.IdToken;
            confClientScopesResult.DataSource = authenticationResult.Scope;
        }

        private void SetConfidentClientErrorPageInfo(string errorMessage)
        {
            callResultConfClient.BringToFront();

            callResultConfClient.Text = errorMessage;
        }

        private void ClearConfidentialClientResultPageInfo()
        {
            confClientAccessTokenResult.Text = string.Empty;
            confClientExpiresOnResult.Text = string.Empty;
            confClientTenantIdResult.Text = string.Empty;
            confClientUserResult.Text = string.Empty;
            confClientIdTokenResult.Text = string.Empty;
            confClientScopesResult.DataSource = null;
        }

        #endregion

        private void PiiLogging()
        {
            if (PiiLoggingEnabled.Checked)
            {
                Logger.PiiLoggingEnabled = true;
            }
            Logger.PiiLoggingEnabled = false;
        }

        private void expireAT1Btn_Click(object sender, EventArgs e)
        {
            //TODO: Expire AccessToken
        }

        private void deleteAT1Btn_Click(object sender, EventArgs e)
        {
            //TODO: delete AccessToken
            
        }

        private ICollection<string> GetAccessToken()
        {
            return _publicClientApplication.UserTokenCache.TokenCacheAccessor.GetAllAccessTokensAsString();
        }

        private void signOutUserBtn_Click(object sender, EventArgs e)
        {
            _publicClientApplication.Remove(CurrentUser);
            idTokenAT1Result.Text = @"The user: " + CurrentUser.DisplayableId + @" has been signed out";
            RefreshUI();
        }

        private void usersListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedUserChanged();
        }
        private void SelectedUserChanged()
        {
            // Clear values in cache UI 
            ClearCacheUIPage();

            // Define the User in the listbox
            User selectedUser = (User)usersListBox.SelectedItem;

            //Get all token cache items from TokenCacheAccessor
            ICollection<string> accessTokens = GetAccessToken();

            ICollection<string> userAccessTokens = new List<string>();

            //Find the token related to the selected user
            foreach (string accessToken in accessTokens)
            {
                AccessTokenCacheItem accessTokenCacheItem = JsonHelper.DeserializeFromJson<AccessTokenCacheItem>(accessToken);
                //if (string.Compare(accessTokenCacheItem.User.DisplayableId, selectedUser.DisplayableId, StringComparison.InvariantCultureIgnoreCase) == 0)
                if (accessTokenCacheItem.User.DisplayableId == selectedUser.DisplayableId)
                {
                    userAccessTokens.Add(accessTokenCacheItem.AccessToken);
                    // Populate the token cache UI page
                    idTokenAT1Result.Text = accessTokenCacheItem.IdToken.Issuer;
                    expiresOnAT1Result.Text = accessTokenCacheItem.ExpiresOn.ToString();
                    tenantIdAT1Result.Text = accessTokenCacheItem.IdToken.TenantId;
                    scopeAT1Result.Text = accessTokenCacheItem.Scope;
                }
            }
            //Send result to userTokensListBox
            userTokensListBox.DataSource = userAccessTokens;
            userOneUpnResult.Text = selectedUser.DisplayableId;
        }

        private void ClearCacheUIPage()
        {
            idTokenAT1Result.Text = string.Empty;
            expiresOnAT1Result.Text = string.Empty;
            tenantIdAT1Result.Text = string.Empty;
            scopeAT1Result.Text = string.Empty;
        }
    }
}
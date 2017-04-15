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
        private readonly PublicClientHandler _publicClientHandler = new PublicClientHandler();

        private readonly ConfidentialClientHandler _confidentialClientHandler = new ConfidentialClientHandler();

        private readonly AppLogger _appLogger = new AppLogger();

        private const string ApplicationId = "0615b6ca-88d4-4884-8729-b178178f7c27";

        public MainForm()
        {
            InitializeComponent();
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;

            Logger.LogCallback = _appLogger.Log;
            Logger.Level = Logger.LogLevel.Info;
            Logger.PiiLoggingEnabled = PiiLoggingEnabled.Checked;

            // ResetUserList();
        }

        public void ResetUserList()
        {
            List<IUser> userListDataSource = _publicClientHandler.PublicClientApplication.Users.ToList();

            userList.DataSource = userListDataSource;
            usersListBox.DataSource = userListDataSource;
            userList.Refresh();
            usersListBox.Refresh();
        }
        #region PublicClient UI Controls

        private void loginHint_TextChanged(object sender, EventArgs e)
        {
            _publicClientHandler.LoginHint = loginHintTextBox.Text;
        }

        private void userList_SelectedIndexChanged(object sender, EventArgs e)
        {
            _publicClientHandler.CurrentUser = (IUser)userList.SelectedItem;
        }

        private void extraQueryParams_TextChanged(object sender, EventArgs e)
        {
            _publicClientHandler.ExtraQueryParams = extraQueryParams.Text;
        }

        private void scopes_TextChanged(object sender, EventArgs e)
        {
            _publicClientHandler.Scopes = scopes.Text.Split(' ');
        }

        private void overriddenAuthority_TextChanged(object sender, EventArgs e)
        {
            _publicClientHandler.AuthorityOverride = overriddenAuthority.Text;
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

        #endregion

        #region ConfidentialClient UI Controls
        private void confClientScopesTextBox_TextChanged(object sender, EventArgs e)
        {
            _confidentialClientHandler.ConfClientScopes = scopes.Text.Split(' ');
        }

        private void ConfClientOverrideAuthority_TextChanged(object sender, EventArgs e)
        {
            _confidentialClientHandler.ConfClientOverriddenAuthority = confClientOverrideAuthority.Text;
        }

        private void clientSecretTxtBox_TextChanged(object sender, EventArgs e)
        {
            //   ClientCredential = clientSecretTxtBox.Text;
        }

        private void forceRefreshGroupBox_Enter(object sender, EventArgs e)
        {
            if (forceRefreshFalseBtn.Checked)
            {
                _confidentialClientHandler.ForceRefresh = false;
            }
            _confidentialClientHandler.ForceRefresh = true;
        }
        #endregion

        #region PublicClientApplication Acquire Token
        private async void acquireTokenInteractive_Click(object sender, EventArgs e)
        {
            ClearResultPageInfo();
            try
            {
                AuthenticationResult authenticationResult = await _publicClientHandler.AcquireTokenInteractive(_publicClientHandler.AuthorityOverride, ApplicationId, _publicClientHandler.Scopes,
                    _publicClientHandler.CurrentUser, GetUIBehavior(), _publicClientHandler.ExtraQueryParams, new UIParent(/*this*/), _publicClientHandler.LoginHint);

                // if (modalWebview.Checked)
                // {

                // }
                // else
                //  {
                //     result = await clientApplication.AcquireTokenAsync(scopes.Text.Split(' '),
                //         (User) userList.SelectedItem, GetUIBehavior(), extraQueryParams.Text);
                //  }

                // if (modalWebview.Checked)
                // {
                //     result = await clientApplication.AcquireTokenAsync(scopes.Text.Split(' '), loginHint.Text,
                //        GetUIBehavior(), extraQueryParams.Text, new UIParent(this));
                // }
                //  else
                //  {

                // }
                SetResultPageInfo(authenticationResult);
                ResetUserList();
            }
            catch (Exception exc)
            {
                CreateException(exc);
            }
        }

        private async void acquireTokenSilent_Click(object sender, EventArgs e)
        {
            ClearResultPageInfo();

            try
            {
                AuthenticationResult authenticationResult =
                    await _publicClientHandler.AcquireTokenSilent(_publicClientHandler.Scopes, _publicClientHandler.CurrentUser);

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
            string output;

            MsalServiceException exception = ex as MsalServiceException;

            if (exception != null)
            {
                output = ex.Message + Environment.NewLine + ex.StackTrace;
            }
            else
            {
                output = ex.Message;
            }

            SetErrorPageInfo(output);

            RefreshUI();
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

        private void applySettings_Click(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("ExtraQueryParameters", environmentQP.Text);
        }

        public void RefreshUI()
        {
            msalPIILogsTextBox.Text = _appLogger.DrainPiiLogs();
            msalLogsTextBox.Text = _appLogger.DrainLogs();
            userList.SelectedItem = _publicClientHandler.PublicClientApplication;
        }

        #region App logic

        public void SetResultPageInfo(AuthenticationResult authenticationResult)
        {
            callResult.Text = @"Access Token: " + authenticationResult.AccessToken + Environment.NewLine +
                              @"Expires On: " + authenticationResult.ExpiresOn + Environment.NewLine +
                              @"Tenant Id: " + authenticationResult.TenantId + Environment.NewLine + @"User: " +
                              authenticationResult.User.DisplayableId + Environment.NewLine +
                              @"Id Token: " + authenticationResult.IdToken;
        }

        public void SetErrorPageInfo(string errorMessage)
        {
            callResult.Text = errorMessage;
        }

        public void ClearResultPageInfo()
        {
            callResult.Text = string.Empty;
        }

        private void SetConfidentialClientPageInfo(AuthenticationResult authenticationResult)
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

        private void SetConfidentialClientErrorPageInfo(string errorMessage)
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

        private void expireAT1Btn_Click(object sender, EventArgs e)
        {
            // Expire AccessToken

        }

        private void deleteAT1Btn_Click(object sender, EventArgs e)
        {
            // Delete AccessToken
            DeleteSelectedAccessToken();
        }

        private void clearLogsButton_Click(object sender, EventArgs e)
        {
            msalLogsTextBox.Text = string.Empty;
            msalPIILogsTextBox.Text = string.Empty;
        }

        private void DeleteSelectedAccessToken()
        {
            // Define AccessToken in listbox
            string selectedUserAccessToken = (string)userTokensListBox.SelectedItem;

            // Find the AccessToken for the selected user and delete
            _publicClientHandler.PublicClientApplication.UserTokenCache.TokenCacheAccessor.DeleteAccessToken(selectedUserAccessToken);

            ICollection<string> deletedAccessToken = GetAccessTokens();

            userTokensListBox.DataSource = deletedAccessToken;

            ClearCacheUIPage();
        }

        private void signOutUserBtn_Click(object sender, EventArgs e)
        {
            _publicClientHandler.PublicClientApplication.Remove(_publicClientHandler.CurrentUser);
            idTokenAT1Result.Text = @"The user: " + _publicClientHandler.CurrentUser.DisplayableId + @" has been signed out";
            RefreshUI();
        }

        private void usersListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FindAccessTokenForSelectedUser();
        }

        private void FindAccessTokenForSelectedUser()
        {
            // Clear values in cache UI 
            ClearCacheUIPage();

            //Get all token cache items from TokenCacheAccessor
            ICollection<string> accessTokens = GetAccessTokens();

            ICollection<string> userAccessTokens = new List<string>();

            //Find the token related to the selected user
            foreach (string accessToken in accessTokens)
            {
                AccessTokenCacheItem accessTokenCacheItem = JsonHelper.DeserializeFromJson<AccessTokenCacheItem>(accessToken);
                //if (string.Compare(accessTokenCacheItem.User.DisplayableId, selectedUser.DisplayableId, StringComparison.InvariantCultureIgnoreCase) == 0)
                if (accessTokenCacheItem.User.DisplayableId == _publicClientHandler.CurrentUser.DisplayableId)
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
            userOneUpnResult.Text = _publicClientHandler.CurrentUser.DisplayableId;
        }

        private ICollection<string> GetAccessTokens()
        {
            return _publicClientHandler.PublicClientApplication.UserTokenCache.TokenCacheAccessor.GetAllAccessTokensAsString();
        }

        private void ClearCacheUIPage()
        {
            idTokenAT1Result.Text = string.Empty;
            expiresOnAT1Result.Text = string.Empty;
            tenantIdAT1Result.Text = string.Empty;
            scopeAT1Result.Text = string.Empty;
        }

        private void forceRefreshTrueBtn_CheckedChanged(object sender, EventArgs e)
        {

        }

    }
}
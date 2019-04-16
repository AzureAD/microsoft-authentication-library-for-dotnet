// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Identity.Client;

namespace AutomationApp
{
    public partial class AutomationUI : Form
    {
        private delegate Task<AuthenticationResult> Command(Dictionary<string, string> input);
        private readonly AppLogger _appLogger;
        private Command _commandToRun;
        private readonly TokenHandler _tokenHandlerApp;

        public AutomationUI()
        {
            _appLogger = new AppLogger();
            _tokenHandlerApp = new TokenHandler(_appLogger);
            InitializeComponent();
        }

        public Dictionary<string, string> CreateDictionaryFromJson(string json)
        {
            var jss = new JavaScriptSerializer();
            return jss.Deserialize<Dictionary<string, string>>(json);
        }

        #region Main Page Tab Button Click Handlers

        private void acquireToken_Click(object sender, EventArgs e)
        {
            _commandToRun = _tokenHandlerApp.AcquireTokenAsync;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void acquireTokenSilent_Click(object sender, EventArgs e)
        {
            _commandToRun = _tokenHandlerApp.AcquireTokenSilentAsync;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void expireAccessToken_Click(object sender, EventArgs e)
        {
            _commandToRun = null;
            pageControl1.SelectedTab = dataInputPage;
        }

        #endregion

        private async void GoBtn_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> dict = CreateDictionaryFromJson(dataInput.Text);
            try
            {
                if (_commandToRun == null)
                {
                    _tokenHandlerApp.ExpireAccessToken(dict);
                    ClearResultPageInfo();
                    messageResult.Text = "The access token has expired.";
                }
                else
                {
                    AuthenticationResult authenticationResult = await _commandToRun(dict).ConfigureAwait(true);
                    SetResultPageInfo(authenticationResult);
                }
            }
            catch (Exception exception)
            {
                exceptionResult.Text = exception.ToString();
            }
            msalLogs.Text = _appLogger.GetMsalLogs();
            pageControl1.SelectedTab = resultPage;
        }

        private void Done_Click(object sender, EventArgs e)
        {
            ClearResultPageInfo();
            pageControl1.SelectedTab = mainPage;
        }

        private void SetResultPageInfo(AuthenticationResult authenticationResult)
        {
            if (!string.IsNullOrWhiteSpace(authenticationResult.AccessToken))
            {
                testResultBox.Text = "Result: Success";
            }
            else
            {
                testResultBox.Text = "Result: Failure";
            }

            accessTokenResult.Text = authenticationResult.AccessToken;
            expiresOnResult.Text = authenticationResult.ExpiresOn.ToString(CultureInfo.InvariantCulture);
            tenantIdResult.Text = authenticationResult.TenantId;
            userResult.Text = authenticationResult.Account.Username;
            idTokenResult.Text = authenticationResult.IdToken;
            scopeResult.DataSource = authenticationResult.Scopes;
        }

        private void ClearResultPageInfo()
        {
            accessTokenResult.Text = string.Empty;
            expiresOnResult.Text = string.Empty;
            tenantIdResult.Text = string.Empty;
            userResult.Text = string.Empty;
            idTokenResult.Text = string.Empty;
            scopeResult.DataSource = null;
            messageResult.Text = string.Empty;
        }

        private void AutomationUI_Load(object sender, EventArgs e)
        {
            ClearResultPageInfo();
        }
    }
}

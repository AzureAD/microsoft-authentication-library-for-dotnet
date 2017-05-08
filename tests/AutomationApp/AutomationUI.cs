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
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Identity.Client;

namespace AutomationApp
{
    public partial class AutomationUI : Form
    {
        private delegate Task<AuthenticationResult> Command(Dictionary<string, string> input);
        private readonly AppLogger _appLogger = new AppLogger();
        private Command _commandToRun;
        private readonly TokenHandler _tokenHandlerApp = new TokenHandler();

        public AutomationUI()
        {
            InitializeComponent();
            Logger.LogCallback = _appLogger.Log;
        }

        public Dictionary<string, string> CreateDictionaryFromJson(string json)
        {
            var jss = new JavaScriptSerializer();
            return jss.Deserialize<Dictionary<string, string>>(json);
        }

        #region Main Page Tab Button Click Handlers

        private void acquireToken_Click(object sender, EventArgs e)
        {
            _commandToRun = _tokenHandlerApp.AcquireToken;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void acquireTokenSilent_Click(object sender, EventArgs e)
        {
            _commandToRun = _tokenHandlerApp.AcquireTokenSilent;
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
                    AuthenticationResult authenticationResult = await _commandToRun(dict);
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
            accessTokenResult.Text = authenticationResult.AccessToken;
            expiresOnResult.Text = authenticationResult.ExpiresOn.ToString();
            tenantIdResult.Text = authenticationResult.TenantId;
            userResult.Text = authenticationResult.User.DisplayableId;
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

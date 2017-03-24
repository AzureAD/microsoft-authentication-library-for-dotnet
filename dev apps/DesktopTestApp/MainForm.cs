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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;

namespace DesktopTestApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;
            authority.SelectedIndex = 0;

            userList.DataSource = new PublicClientApplication(
                "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc") {UserTokenCache = TokenCacheHelper.GetCache()}.Users.ToList();
        }

        private void acquire_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = acquireTabPage;

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

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private async void acquireTokenInteractive_Click(object sender, EventArgs e)
        {

            PublicClientApplication clientApplication = CreateClientApplication();
            string output = string.Empty;
            callResult.Text = output;
            IAuthenticationResult result;
            try
            {
                result = await clientApplication.AcquireTokenAsync(scopes.Text.Split(' '));
                output = JsonHelper.SerializeToJson(result);
            }
            catch (Exception exc)
            {

                if (exc is MsalServiceException)
                {
                    output += ((MsalServiceException) exc).ErrorCode;
                }

                output = exc.Message + Environment.NewLine + exc.StackTrace;
            }

            callResult.Text = output;
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
                    "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc", authority.SelectedItem.ToString());
            }

            return clientApplication;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace WinFormsAutomationApp
{
    public partial class MainForm : Form
    {
        private delegate Task<string> Command(Dictionary<string, string> input);
        LoggerCallbackImpl loggerCallback = new LoggerCallbackImpl();
        private Command _commandToRun = null;

        public MainForm()
        {
            InitializeComponent();
            TokenCache.DefaultShared.AfterAccess += TokenCacheDelegates.AfterAccessNotification;
            TokenCache.DefaultShared.BeforeAccess += TokenCacheDelegates.BeforeAccessNotification;
            LoggerCallbackHandler.Callback = loggerCallback;
        }

        private void acquireToken_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.AcquireToken;
            pageControl1.SelectedTab = dataInputPage;
        }

        private async void go_Click(object sender, EventArgs e)
        {
            string output = await _commandToRun((AuthenticationHelper.CreateDictionaryFromJson(dataInput.Text)));
            pageControl1.SelectedTab = resultPage;
            resultInfo.Text = output;
            adalLogs.Text = loggerCallback.GetAdalLogs();
        }

        private void done_Click(object sender, EventArgs e)
        {
            resultInfo.Text = string.Empty;
            dataInput.Text = string.Empty;
            pageControl1.SelectedTab = mainPage;
        }

        private void acquireTokenSilent_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.AcquireTokenSilent;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void expireAccessToken_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.ExpireAccessToken;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void invalidateRefreshToken_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.InvalidateRefreshToken;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void readCache_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.ReadCache;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void clearCache_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.ClearCache;
            pageControl1.SelectedTab = resultPage;
            adalLogs.Text = loggerCallback.GetAdalLogs();
        }

        private void acquireTokenDeviceProfile_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.AcquireTokenUsingDeviceProfile;
            pageControl1.SelectedTab = dataInputPage;
        }
    }
}

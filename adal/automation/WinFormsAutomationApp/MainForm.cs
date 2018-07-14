using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace WinFormsAutomationApp
{
    public partial class MainForm : Form
    {
        private delegate Task<string> Command(Dictionary<string, string> input);

        private readonly StringBuilder _logCollector = new StringBuilder();


        public string GetAdalLogs()
        {
            return _logCollector.ToString();
        }

        private Command _commandToRun = null;

        public MainForm()
        {
            DeleteCache.CleanCookies();
            InitializeComponent();

            void LogCallback(LogLevel level, string message, bool containsPii)
            {
                _logCollector.AppendLine(message);
            }

            LoggerCallbackHandler.LogCallback = LogCallback;
        }

        private void acquireToken_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.AcquireTokenAsync;
            pageControl1.SelectedTab = dataInputPage;
        }
        
        private async void RequestGo_Click(object sender, EventArgs e)
        {
             string output = await _commandToRun((AuthenticationHelper.CreateDictionaryFromJson(requestInfo.Text)));
            pageControl1.SelectedTab = resultPage;
            resultInfo.Text = output;
            resultLogs.Text = GetAdalLogs();
        }

        private void resultDone_Click(object sender, EventArgs e)
        {
            resultInfo.Text = string.Empty;
            requestInfo.Text = string.Empty;
            pageControl1.SelectedTab = mainPage;
        }

        private void acquireTokenSilent_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.AcquireTokenSilentAsync;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void expireAccessToken_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.ExpireAccessTokenAsync;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void invalidateRefreshToken_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.InvalidateRefreshTokenAsync;
            pageControl1.SelectedTab = dataInputPage;
        }

        private async void readCache_Click(object sender, EventArgs e)
        {
            string output = await AuthenticationHelper.ReadCacheAsync(); ;
            pageControl1.SelectedTab = resultPage;
            resultInfo.Text = output;
            resultLogs.Text = GetAdalLogs();
        }

        private async void clearCache_Click(object sender, EventArgs e)
        {
            string output = await AuthenticationHelper.ClearCacheAsync(null);
            pageControl1.SelectedTab = resultPage;
            resultInfo.Text = output;
            resultLogs.Text = GetAdalLogs();
        }

        private void acquireTokenDeviceProfile_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.AcquireTokenUsingDeviceProfileAsync;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void acquireDeviceCode_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.AcquireDeviceCodeAsync;
            pageControl1.SelectedTab = dataInputPage;
        }        
    }
}

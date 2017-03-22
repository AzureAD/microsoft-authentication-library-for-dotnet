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
        private delegate Task<string> Command(Dictionary<string, string> input);
        private readonly LoggerCallbackImpl _loggerCallback = new LoggerCallbackImpl();
        private Command _commandToRun;
        private readonly TokenHandler _tokenHandlerApp = new TokenHandler();

        public AutomationUI()
        {
            InitializeComponent();
            Logger.Callback = _loggerCallback;
        }

        public Dictionary<string, string> CreateDictionaryFromJson(string json)
        {
            var jss = new JavaScriptSerializer();
            return jss.Deserialize<Dictionary<string, string>>(json);
        }

        private void AutomationApp_Load(object sender, EventArgs e)
        {

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
            _commandToRun = _tokenHandlerApp.ExpireAccessToken;
            pageControl1.SelectedTab = dataInputPage;
        }

        #endregion

        private async void GoBtn_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> dict = CreateDictionaryFromJson(dataInput.Text);
            try
            {
                resultInfo.Text = await _commandToRun(dict);
            }
            catch (Exception exception)
            {
                resultInfo.Text = exception.ToString();
            }
            msalLogs.Text = _loggerCallback.GetMsalLogs();
            pageControl1.SelectedTab = resultPage;
        }

        private void Done_Click(object sender, EventArgs e)
        {
            resultInfo.Text = string.Empty;
            pageControl1.SelectedTab = mainPage;
        }
    }
}

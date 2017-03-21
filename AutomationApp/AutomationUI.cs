using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using static Microsoft.Identity.Client.TokenCache;

namespace AutomationApp
{
    public partial class AutomationUI : Form
    {
        private delegate Task<string> Command(Dictionary<string, string> input);
        LoggerCallbackImpl _loggerCallback = new LoggerCallbackImpl();
        private Command _commandToRun = null;
        internal TokenCache UserTokenCache { get; set; }
        private TokenHandler tokenHandlerApp = new TokenHandler();

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
            _commandToRun = tokenHandlerApp.AcquireToken;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void acquireTokenSilent_Click(object sender, EventArgs e)
        {
            _commandToRun = tokenHandlerApp.AcquireTokenSilent;
            pageControl1.SelectedTab = dataInputPage;
        }

        private void expireAccessToken_Click(object sender, EventArgs e)
        {
            _commandToRun = tokenHandlerApp.ExpireAccessToken;
        }

        /* private void clearCache_Click(object sender, EventArgs e)
         {
             _commandToRun = tokenHandlerApp.ClearCache;
         }*/

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

        private void invalidateToken_Click(object sender, EventArgs e)
        {

        }

    }
}

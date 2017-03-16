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

namespace AutomationApp
{
    public partial class AutomationApp : Form
    {
        private delegate Task<string> Command(Dictionary<string, string> input);
        LoggerCallbackImpl _loggerCallback = new LoggerCallbackImpl();
        private Command _commandToRun = null;

        public AutomationApp()
        {
            InitializeComponent();
            //TokenCache
            
            Logger.Callback = _loggerCallback;
        }

        private void AutomationApp_Load(object sender, EventArgs e)
        {
            
        }

        private void acquireToken_Click(object sender, EventArgs e)
        {
            _commandToRun = AuthenticationHelper.AcquireToken;
            pageControl1.SelectedTab = dataInputPage;
        }

        private async void go_Click(object sender, EventArgs e)
        {
            string output = await _commandToRun(AuthenticationHelper.CreateDictionaryFromJson(dataInput.Text));
            pageControl1.SelectedTab = resultPage;
            resultInfo.Text = output;
            msalLogs.Text = _loggerCallback.GetMsalLogs();
        }

        private void Done_Click(object sender, EventArgs e)
        {
            resultInfo.Text = string.Empty;
            dataInput.Text = string.Empty;
            pageControl1.SelectedTab = mainPage;
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void invalidateToken_Click(object sender, EventArgs e)
        {

        }
    }
}

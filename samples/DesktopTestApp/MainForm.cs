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

namespace DesktopTestApp
{
    public partial class MainForm : Form
    {
        readonly PublicClientApplication _clientApplication = new PublicClientApplication("5a434691-ccb2-4fd1-b97b-b64bcfbc03fc");
        public MainForm()
        {
            InitializeComponent();
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;
            environment.SelectedIndex = 0;
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

        private void acquireTokenInteractive_Click(object sender, EventArgs e)
        {
            UriBuilder authorityBuilder  = new UriBuilder(environment.SelectedItem.ToString());
            authorityBuilder.Path = tenant.Text;
            
        }
    }
}

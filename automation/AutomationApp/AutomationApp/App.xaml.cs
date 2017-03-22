using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace AutomationApp
{
    public partial class App : Application
    {
        public delegate string Command(Dictionary<string, string> input);

        public App()
        {
            InitializeComponent();

            MainPage = new AutomationApp.MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}

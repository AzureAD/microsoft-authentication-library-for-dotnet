using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MSAL.XamarinForms.Services;
using MSAL.XamarinForms.Views;

namespace MSAL.XamarinForms
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}

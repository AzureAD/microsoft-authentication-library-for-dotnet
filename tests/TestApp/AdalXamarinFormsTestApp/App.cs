using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace AdalXamarinFormsTestApp
{
    public delegate IAuthorizationParameters CreateParameters();

    public class App : Application
    {
        private CreateParameters createParameters;

        public App(CreateParameters createParameters)
        {
            this.createParameters = createParameters;

            // The root page of your application
            MainPage = new NavigationPage(new MainPage());
        }

        public IAuthorizationParameters CreateAuthorizationParameters()
        {
            return this.createParameters();
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

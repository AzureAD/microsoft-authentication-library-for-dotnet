using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client;


using Xamarin.Forms;

namespace XForms
{
    public partial class App : Application
    {
        public static PublicClientApplication PCA;
        public static string ClientID = "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc";
        public static string[] Scopes = { "User.Read" };

        class LoggerCallback : ILoggerCallback
        {

            public void Log(Logger.LogLevel level, string message, bool containsPii)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    LogPage.AddToLog("[" + level + "]" + " - " + message);
                });
            }
        }

        public App()
        {
            PCA = new PublicClientApplication(ClientID);
            Device.OnPlatform(Android: () => {
                PCA.RedirectUri = "adaliosxformsapp://com.yourcompany.xformsapp";
            });

            Device.OnPlatform(iOS: () => {
                PCA.RedirectUri = "msauth-5a434691-ccb2-4fd1-b97b-b64bcfbc03fc://com.microsoft.identity.client.sample";
            });

            MainPage = new XForms.MainPage();
            LogPage.AddToLog("hello from App page");

            Logger.Callback = new LoggerCallback();

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

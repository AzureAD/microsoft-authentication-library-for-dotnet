using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Xamarin.Forms;
using AdalXamarinFormsTestApp;

namespace AdalXamarinFormsTestApp.Droid
{
    [Activity(Label = "AdalXamarinFormsTestApp", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App(this.CreateParameters));
        }

        private IAuthorizationParameters CreateParameters()
        {
            return new AuthorizationParameters(this);
        }
    }
}


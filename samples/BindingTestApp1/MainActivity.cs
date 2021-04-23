using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;

using System.Threading.Tasks;

using Com.Microsoft.Identity.Client;
using MsalAndroid = Com.Microsoft.Identity.Client;

namespace BindingTestApp1
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            tryAuth();
        }

        private async void tryAuth()
        {
            //AppListner listner = new AppListner();

            //MsalAndroid.PublicClientApplication.Create(
            //    Android.App.Application.Context,
            //    "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8",
            //    "https://login.microsoftonline.com/common/",
            //    "msauth://com.companyname.xamarindev/Fy2zjTiLYs5sXM3sGy+PGcv8MaQ=",
            //    listner);

            //Java.IO.File config = null;
            //Android.Content.Res.AssetManager assets = this.Assets;
            //using (System.IO.StreamReader sr = new System.IO.StreamReader(assets.Open("config.json")))
            //{
            //    System.IO.Stream s = sr.BaseStream;
            //    config = new Java.IO.File("config.json");
            //}
            ISingleAccountPublicClientApplication _boundApplication = null;


            await Task.Run(() => _boundApplication = SingleAccountPublicClientApplication.CreateSingleAccountPublicClientApplication
                                                                               (
                                                                                   Android.App.Application.Context,
                                                                                   //config
                                                                                   Resource.Raw.config
                                                                               )
            ).ConfigureAwait(false);


            AndroidAuthCallback callback = new AndroidAuthCallback();

            var builder = new MsalAndroid.AcquireTokenParameters.Builder();
            builder.StartAuthorizationFromActivity(this)
                .WithCallback(callback)
                .WithScopes(new[] { "user.read" });

            MsalAndroid.AcquireTokenParameters parameters = builder.Build() as MsalAndroid.AcquireTokenParameters;
            _boundApplication.AcquireToken(parameters);
            Console.WriteLine("DONE");
            Console.WriteLine(callback.Result);
        }

        internal class AndroidAuthCallback : global::Java.Lang.Object, MsalAndroid.IAuthenticationCallback
        {
            public MsalAndroid.IAuthenticationResult Result { get; set; }

            public void OnCancel()
            {
                throw new NotImplementedException();
            }

            public void OnError(MsalAndroid.Exception.MsalException p0)
            {
                throw new NotImplementedException();
            }

            public void OnSuccess(MsalAndroid.IAuthenticationResult result)
            {
                Result = result;
            }
        }

        internal class AppListner : global::Java.Lang.Object, MsalAndroid.IPublicClientApplicationApplicationCreatedListener
        {
            public MsalAndroid.IPublicClientApplication PublicClientApplication
            {
                get;
                set;
            }

            public void OnCreated(MsalAndroid.IPublicClientApplication publicClientApplication)
            {
                PublicClientApplication = publicClientApplication;
            }

            public void OnError(MsalAndroid.Exception.MsalException p0)
            {

            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View)sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}

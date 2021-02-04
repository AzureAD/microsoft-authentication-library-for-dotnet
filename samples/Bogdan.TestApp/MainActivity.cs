using System;
using System.Threading;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using Com.Microsoft.Identity.Client;
using Com.Microsoft.Identity.Client.Exception;
using Java.Util;

namespace App1
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private TextView _textView;

        private ISingleAccountPublicClientApplication _pca;
        private IAccount _account;


        protected override void OnCreate(Bundle savedInstanceState)
        {

            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            Button signInBtn = FindViewById<Button>(Resource.Id.signInBtn);
            signInBtn.Click += SignInBtn_Click;
            Button helpBtbn = FindViewById<Button>(Resource.Id.helpBtn);
            helpBtbn.Click += HelpBtbn_Click;
            Button deviceCodeBtn = FindViewById<Button>(Resource.Id.deviceCodeBtn);
            deviceCodeBtn.Click += DeviceCodeBtn_Click;
            Button silentBtn = FindViewById<Button>(Resource.Id.silentBtn);
            silentBtn.Click += SilentBtn_Click;
            Button signOutBtn = FindViewById<Button>(Resource.Id.singOutBtn);
            signOutBtn.Click += SignOutBtn_Click;

            _textView = FindViewById<TextView>(Resource.Id.txtView);
            LogMessage("MainActivity::Created");



            // Create PCA
            int resourceId = Resource.Raw.single_account_config;

            PublicClientApplication.CreateSingleAccountPublicClientApplication(
                this,
                resourceId,
                new SingleAccountApplicationCreatedListener(
                    onCreatedAction: (pca) =>
                    {
                        LogMessage("PCA created!");
                        _pca = pca;
                        LoadAccount();
                    },
                    onExceptionAction: (ex) => LogMessage(ex.ToString())));

            LogMessage("Finished OnInit");

        }

        private void SignOutBtn_Click(object sender, EventArgs e)
        {
            _pca.SignOut(
                new SignOutCallback(
                    onSuccessAction: () => LogMessage("Signed out!"),
                    onErrorAction: (e) => LogMessage(e.ToString()))
                );
            
        }

        private void HelpBtbn_Click(object sender, EventArgs e)
        {
            _textView.Text = "";
            LogMessage("HelpBtbn::Click");
            //IPublicClientApplication publicClientApplication = null;
            PublicClientApplication.ShowExpectedMsalRedirectUriInfo(this);
        }

        private void SignInBtn_Click(object sender, System.EventArgs e)
        {
            _textView.Text = "";
            LogMessage("SignInBtn::Click");
            if (_pca == null)
            {
                LogMessage("PCA not yet initialized!");
                return;
            }

            // Doesn't work, no browser pop-up :(
            //_pca.SignIn(
            //    /*activity */ this,
            //    /*login_hint*/"liu.kang@bogavrilltd.onmicrosoft.com",
            //    new[] { "User.Read" },
            //    new InteractiveAuthCallback(
            //        onCancelAction: () => LogMessage("Auth cancelled"),
            //        onErrorAction: (ex) => LogMessage(ex.ToString()),
            //        onSuccessAction: (result) =>
            //        {
            //            _account = result.Account;
            //            LogMessage(
            //                $"Success!! Token for {result.Account.Username}," +
            //                $" tenant {result.TenantId} - " +
            //                $" token {result.AccessToken} ");
            //        }));


            _pca.AcquireToken(
                this, 
                new[] { "User.Read" },
                new InteractiveAuthCallback(
                    onCancelAction: () => LogMessage("Auth cancelled"),
                    onErrorAction: (ex) => LogMessage(ex.ToString()),
                    onSuccessAction: (result) =>
                    {
                        _account = result.Account;
                        LogMessage(
                            $"Success!! Token for {result.Account.Username}," +
                            $" tenant {result.TenantId} - " +
                            $" token {result.AccessToken} ");
                    }));
        }

        private void SilentBtn_Click(object sender, EventArgs e)
        {
            _textView.Text = "";
            // TODO: try out the AcquireTokenSilent version as well, it's supposed to be for background thread
            _pca.AcquireTokenSilentAsync(
                new[] { "User.Read" },
                _account?.Username,
                new SilentAuthCallback(
                    onErrorAction: (ex) => LogMessage("Error! " + ex.ToString()),
                     onSuccessAction: (result) =>
                     {
                         _account = result.Account;
                         LogMessage(
                             $"Success!! Token for {result.Account.Username}," +
                             $" tenant {result.TenantId} - " +
                             $" token {result.AccessToken} ");
                     }));

        }

        private void DeviceCodeBtn_Click(object sender, EventArgs e)
        {
            _textView.Text = "";
            _pca.AcquireTokenWithDeviceCode(
               new[] { "User.Read" },
               new PublicClientApplicationDeviceCodeFlowCallback(
                   onErrorAction: (ex) => LogMessage(ex.ToString()),
                   onSuccess: (result) =>
                   {
                       _account = result.Account;

                       LogMessage(
                       $"Success!! Token for {result.Account.Username}," +
                       $" tenant {result.TenantId} - " +
                       $" token {result.AccessToken} ");
                   },
                   onCodeReceived: (s1, s2, s3, d) =>
                   {
                       LogMessage(
                           $"Code received!!! Go to {s1} and type {s2} ( detailed instructions: {s3})");
                   }));
        }

        private void LoadAccount()
        {
            if (_pca == null)
            {
                return;
            }

            _pca.GetCurrentAccountAsync(
                new SingleAccountApplicationCurrentAccountCallback(
                    onAccountChangedAction: (priorAccount, newAccount) =>
                    {
                        LogMessage($"Account changed. Was {priorAccount?.Username ?? "null" }, is {newAccount.Username}");
                        _account = newAccount;
                    },
                    onAccountLoaded: (acc) =>
                    {
                        LogMessage($"Account loaded {acc?.Username}");
                        _account = acc;
                    },
                    onException: (ex) => LogMessage(ex.ToString()))
                );


        }

        #region Callbacks

        internal class SignOutCallback : Java.Lang.Object, ISingleAccountPublicClientApplicationSignOutCallback
        {
            private readonly Action _onSuccessAction;
            private readonly Action<MsalException> _onErrorAction;

            public SignOutCallback(
                Action onSuccessAction,
                Action<MsalException> onErrorAction)
            {
                _onSuccessAction = onSuccessAction;
                _onErrorAction = onErrorAction;
            }

            public void OnError(MsalException p0)
            {
                _onErrorAction(p0);
            }
            public void OnSignOut()
            {
                _onSuccessAction();
            }
        }

        internal class SilentAuthCallback : Java.Lang.Object, ISilentAuthenticationCallback
        {
            private readonly Action<IAuthenticationResult> _onSuccessAction;
            private readonly Action<MsalException> _onErrorAction;

            public SilentAuthCallback(
                Action<IAuthenticationResult> onSuccessAction,
                Action<MsalException> onErrorAction)
            {
                _onSuccessAction = onSuccessAction;
                _onErrorAction = onErrorAction;
            }

            public void OnError(MsalException p0)
            {
                _onErrorAction(p0);
            }

            public void OnSuccess(IAuthenticationResult p0)
            {
                _onSuccessAction(p0);
            }
        }

        internal class PublicClientApplicationDeviceCodeFlowCallback :
            Java.Lang.Object,
            IPublicClientApplicationDeviceCodeFlowCallback, IDisposable
        {

            private readonly Action<MsalException> _onErrorAction;
            private readonly Action<AuthenticationResult> _onSuccess;
            private readonly Action<string, string, string, Date> _onCodeReceived;

            public PublicClientApplicationDeviceCodeFlowCallback(
                Action<MsalException> onErrorAction,
                Action<AuthenticationResult> onSuccess,
                Action<string, string, string, Date> onCodeReceived)
            {
                _onErrorAction = onErrorAction;
                _onSuccess = onSuccess;
                _onCodeReceived = onCodeReceived;
            }

            public void OnError(MsalException p0)
            {
                _onErrorAction(p0);
            }

            public void OnTokenReceived(AuthenticationResult p0)
            {
                _onSuccess(p0);
            }

            public void OnUserCodeReceived(string p0, string p1, string p2, Date p3)
            {
                _onCodeReceived(p0, p1, p2, p3);
            }
        }

        internal class InteractiveAuthCallback :
            Java.Lang.Object,
            IAuthenticationCallback
        {
            private readonly Action _onCancelAction;
            private readonly Action<IAuthenticationResult> _onSuccessAction;
            private readonly Action<MsalException> _onErrorAction;

            public InteractiveAuthCallback(
                Action onCancelAction,
                Action<IAuthenticationResult> onSuccessAction,
                Action<MsalException> onErrorAction)
            {
                _onCancelAction = onCancelAction;
                _onSuccessAction = onSuccessAction;
                _onErrorAction = onErrorAction;
            }
            public void OnCancel()
            {
                _onCancelAction();
            }

            public void OnError(MsalException p0)
            {
                _onErrorAction(p0);
            }

            public void OnSuccess(IAuthenticationResult p0)
            {
                _onSuccessAction(p0);
            }
        }

        internal class SingleAccountApplicationCurrentAccountCallback :
            Java.Lang.Object,
            ISingleAccountPublicClientApplicationCurrentAccountCallback
        {
            private readonly Action<IAccount, IAccount> _onAccountChangedAction;
            private readonly Action<IAccount> _onAccountLoaded;
            private readonly Action<MsalException> _onException;

            public SingleAccountApplicationCurrentAccountCallback(
                Action<IAccount, IAccount> onAccountChangedAction,
                Action<IAccount> onAccountLoaded,
                Action<MsalException> onException)
            {
                _onAccountChangedAction = onAccountChangedAction;
                _onAccountLoaded = onAccountLoaded;
                _onException = onException;
            }

            public void OnAccountChanged(IAccount p0, IAccount p1)
            {
                _onAccountChangedAction(p0, p1);
            }

            public void OnAccountLoaded(IAccount p0)
            {
                _onAccountLoaded(p0);
            }

            public void OnError(MsalException ex)
            {
                _onException(ex);
            }
        }

        internal class SingleAccountApplicationCreatedListener :
            Java.Lang.Object, IPublicClientApplicationSingleAccountApplicationCreatedListener
        {
            private readonly Action<ISingleAccountPublicClientApplication> _onCreatedAction;
            private readonly Action<MsalException> _onExceptionAction;

            public SingleAccountApplicationCreatedListener(
                Action<ISingleAccountPublicClientApplication> onCreatedAction,
                Action<MsalException> onExceptionAction)
            {
                _onCreatedAction = onCreatedAction;
                _onExceptionAction = onExceptionAction;
            }

            public void OnCreated(ISingleAccountPublicClientApplication pca)
            {
                _onCreatedAction(pca);
            }

            public void OnError(MsalException ex)
            {
                _onExceptionAction(ex);
            }
        }
        #endregion

        private void LogMessage(string message)
        {
            this.RunOnUiThread(
                () =>
                _textView.Text = (_textView.Text ?? "") + "\n" + message);
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}


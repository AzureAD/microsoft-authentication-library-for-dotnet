using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using Com.Microsoft.Identity.Client;
using Com.Microsoft.Identity.Client.Exception;

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
            _textView = FindViewById<TextView>(Resource.Id.txtView);
            LogMessage("MainActivity::Created");


            // Not working - show msal redirect info
            // PublicClientApplication.ShowExpectedMsalRedirectUriInfo(this);

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
                    },
                    onExceptionAction: (ex) => LogMessage(ex.ToString())));


            LogMessage("Finished OnInit");

        }

        private void HelpBtbn_Click(object sender, EventArgs e)
        {
            LogMessage("HelpBtbn::Click");
            PublicClientApplication.ShowExpectedMsalRedirectUriInfo(this);
        }

        private void SignInBtn_Click(object sender, System.EventArgs e)
        {
            LogMessage("SignInBtn::Click");
            if (_pca == null)
            {
                LogMessage("PCA not yet initialized!");
                return;
            }

            _pca.SignIn(
                /*activity */ this,
                /*login_hint*/null,
                new[] { "User.Read" },
                new InteractiveAuthCallback(
                    onCancelAction: () => LogMessage("Auth cancelled"),
                    onErrorAction: (ex) => LogMessage(ex.ToString()),
                    onSuccessAction: (result) => {
                        _account = result.Account;
                        LogMessage(
                            $"Success!! Token for {result.Account.Username}," +
                            $" tenant {result.TenantId} - " +
                            $" token {result.AccessToken} ");
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
                        LogMessage($"Account changed. Was {priorAccount.Username}, is {newAccount.Username}");
                        _account = newAccount;
                    },
                    onAccountLoaded: (acc) =>
                    {
                        LogMessage($"Account loaded {acc.Username}");
                        _account = acc;
                    },
                    onException: (ex) => LogMessage(ex.ToString()))
                );


        }

        #region Callbacks

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
            _textView.Text = (_textView.Text ?? "") + "\n" + message;
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}


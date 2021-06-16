using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using AndroidX.AppCompat.App;
using Com.Microsoft.Identity.Client;
using Com.Microsoft.Identity.Client.Exception;
using Java.Security;
using Java.Util;

[assembly: UsesPermission(Android.Manifest.Permission.Internet)]

namespace App1
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const string RedirectUriScheme = "msauth";
        private TextView _textView;
        private Button _showPopupMenu;

        private IMultipleAccountPublicClientApplication _ipca;
        private MultipleAccountPublicClientApplication _spca;
        private IAccount _account;
        private bool loggerSet = false;

        MultipleAccountApplicationCreatedListener saacl;
        InteractiveAuthCallback cb;

        INativeWrapper _nativeWrapper;
        bool _useCSAPI = true;

#pragma warning disable CA2000 // Dispose objects before losing scope
        protected override void OnCreate(Bundle savedInstanceState)
        {
            Logger.Instance.SetLogLevel(Logger.LogLevel.Verbose);
            if (loggerSet == false)
            {
                Logger.Instance.SetExternalLogger(new LoggerCallback(this));
                loggerSet = true;
            }

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

            _textView = FindViewById<EditText>(Resource.Id.txtView);

            _showPopupMenu = FindViewById<Button>(Resource.Id.testCaseBtn);
            _showPopupMenu.Click += ShowPopupMenu_Click;

                LogMessage("MainActivity::Created");

            // Create PCA
            int resourceId = Resource.Raw.single_account_config;

            PublicClientApplication.CreateMultipleAccountPublicClientApplication(
                this,
                resourceId,
                saacl = new MultipleAccountApplicationCreatedListener(
                    onCreatedAction: (pca) =>
                    {
                        LogMessage("PCA created!");
                        _ipca = pca;
                        _nativeWrapper = new AndroidWrapper();
                        _nativeWrapper.Init(_ipca);
                        ((AndroidWrapper)_nativeWrapper).Current = this;
                        LoadAccount();
                    },
                    onExceptionAction: (ex) =>
                    {
                        LogMessage(ex.ToString());
                    }));

            LogMessage("Finished OnInit");

        }

        private void ShowPopupMenu_Click(object sender, EventArgs e)
        {
            PopupMenu menu = new PopupMenu(this, _showPopupMenu);

            // Call inflate directly on the menu:
            menu.Inflate(Resource.Menu.popup_menu);
            menu.MenuItemClick += Menu_MenuItemClick;
            menu.Show();
        }

        private void Menu_MenuItemClick(object sender, PopupMenu.MenuItemClickEventArgs e)
        {
            _textView.Text = "";
            LogMessage("MainActivity.SignInBtn.Click");

            _spca = (MultipleAccountPublicClientApplication)_ipca;
            if (_spca == null)
            {
                LogMessage("PCA not yet initialized!");
                return;
            }

            _spca.GetAccounts(new MultipleAccountApplicationCurrentAccountCallback(
                onAccountChangedAction: (p0, p1) => LogMessage(""),
                onAccountLoaded: (p0) => LogMessage("AccountLoaded is called"),
                onException: (p0) => LogMessage("Error")
                ));
            switch (e.Item.ItemId)
            {
                case App1.Resource.Id.Item5430:
                    TestCase_5430();
                    break;

                case App1.Resource.Id.Item6672:
                    TestCase_6672();
                    break;

                case App1.Resource.Id.Item6681:
                    TestCase_6681();
                    break;

                case App1.Resource.Id.Item6682:
                    TestCase_6682();
                    break;

                case App1.Resource.Id.Item6683:
                    TestCase_6683();
                    break;

                case App1.Resource.Id.Item6692:
                    TestCase_6692();
                    break;

                case App1.Resource.Id.Item795986:
                    TestCase_795986();
                    break;

                case App1.Resource.Id.Item795992:
                    TestCase_795992();
                    break;

                default:
                    break;
            }
        }

        private void SignOutBtn_Click(object sender, EventArgs e)
        {
            _ipca.RemoveAccount(_account,
                new SignOutCallback(
                    onSuccessAction: () =>
                    {
                        LogMessage("Signout success!!!");
                        _account = null;
                    },
                    onErrorAction: (MsalException ex) =>
                    {
                        LogMessage(ex.ToString());
                    }));

            LogMessage("Signed out!");
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
            LogMessage("MainActivity.SignInBtn.Click");

            _spca = (MultipleAccountPublicClientApplication)_ipca;
            if (_spca == null)
            {
                LogMessage("PCA not yet initialized!");
                return;
            }

            _spca.GetAccounts(new MultipleAccountApplicationCurrentAccountCallback(
                onAccountChangedAction: (p0, p1) => LogMessage(""),
                onAccountLoaded: (p0) => LogMessage("AccountLoaded is called"),
                onException: (p0) => LogMessage("Error")
                ));

            // Doesn't work, no browser pop-up :(
            this.RunOnUiThread( () =>
                        {
                            if (_useCSAPI)
                            {
                                SignIn_CSharpFlavor();
                            }
                            else
                            {
                                SignIn_AndroidFlavor();
                            }
                        });

            //_pca.AcquireToken(
            //    this, 
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
        }

        private void SignIn_AndroidFlavor()
        {
            _spca.AcquireToken(
                /*activity */ this,
                /*login_hint*//*"IDLAB@msidlab4.onmicrosoft.com",*/
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

        /// <summary>
        /// Here is how developers will use the API in CSharp style async/await
        /// </summary>
        private async void SignIn_CSharpFlavor()
        {
            try
            {
                var result = await _nativeWrapper.AcquireTokenInteractiveAsync(new[] { "User.Read" }).ConfigureAwait(false);
                _account = result.Account;
                LogMessage(
                    $"Success!! Token for {result.Account.Username}," +
                    $" tenant {result.TenantId} - " +
                    $" token {result.AccessToken} ");
            }
            catch (TaskCanceledException ex)
            {
                LogMessage("Auth cancelled");
            }
            catch (Exception ex)
            {
                LogMessage(ex.ToString());
            }
        }

        private void TestCase_5430()
        {
            // var scopes = new[] { "User.Read" };
            var scopes = new[] { "https://microsoft.sharepoint-df.com//.default" };
            string loginHint = "";
            Prompt prompt = Prompt.WhenRequired;
            string resource = "https://microsoft.sharepoint-df.com//.default";

            AcquireTokenParameters.Builder tokenParamsBuilder = new AcquireTokenParameters.Builder()
                                           .StartAuthorizationFromActivity(this)
                                           .WithPrompt(prompt);
            if (!string.IsNullOrEmpty(loginHint))
            {
                tokenParamsBuilder.WithLoginHint(loginHint);
            }

            tokenParamsBuilder.WithScopes(scopes);

            // tokenParamsBuilder.WithResource(resource);
            tokenParamsBuilder.WithCallback(new InteractiveAuthCallback(
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

            AcquireTokenParameters tokenParams = (AcquireTokenParameters)tokenParamsBuilder.Build();

            this.RunOnUiThread(() =>
            {
                _spca.AcquireToken(tokenParams);
            });
        }

        void TestCase_6672()
        {
            // var scopes = new[] { "User.Read" };
            // var scopes = new[] { "https://microsoft.sharepoint-df.com//.default" };
            var scopes = new[] { "https://graph.microsoft.com/.default" }; // https://graph.microsoft.com
            string loginHint = "";
            Prompt prompt = Prompt.WhenRequired; // use YourAlias@microsoft.com
            string resource = "https://microsoft.sharepoint-df.com//.default";

            AcquireTokenParameters.Builder tokenParamsBuilder = new AcquireTokenParameters.Builder()
                                           .StartAuthorizationFromActivity(this)
                                           .WithPrompt(prompt);
            if (!string.IsNullOrEmpty(loginHint))
            {
                tokenParamsBuilder.WithLoginHint(loginHint);
            }

            tokenParamsBuilder.WithScopes(scopes);

            // tokenParamsBuilder.WithResource(resource);
            tokenParamsBuilder.WithCallback(new InteractiveAuthCallback(
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

            AcquireTokenParameters tokenParams = (AcquireTokenParameters)tokenParamsBuilder.Build();

            this.RunOnUiThread(() =>
            {
                _spca.AcquireToken(tokenParams);
            });

        }

        void TestCase_6681()
        {
            // var scopes = new[] { "User.Read" };
            // var scopes = new[] { "https://microsoft.sharepoint-df.com//.default" };
            var scopes = new[] { "https://graph.microsoft.com/.default" }; // https://graph.microsoft.com
            string loginHint = "YourAlias@microsoft.com";
            Prompt prompt = Prompt.Login; // use YourAlias@microsoft.com
            string resource = "https://microsoft.sharepoint-df.com//.default";

            AcquireTokenParameters.Builder tokenParamsBuilder = new AcquireTokenParameters.Builder()
                                           .StartAuthorizationFromActivity(this)
                                           .WithPrompt(prompt);

            if (!string.IsNullOrEmpty(loginHint))
            {
                tokenParamsBuilder.WithLoginHint(loginHint);
            }

            tokenParamsBuilder.WithScopes(scopes);

            // tokenParamsBuilder.WithResource(resource);
            tokenParamsBuilder.WithCallback(new InteractiveAuthCallback(
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

            AcquireTokenParameters tokenParams = (AcquireTokenParameters)tokenParamsBuilder.Build();

            this.RunOnUiThread(() =>
            {
                _spca.AcquireToken(tokenParams);
            });
        }

        void TestCase_6682()
        {
            // var scopes = new[] { "User.Read" };
            // var scopes = new[] { "https://microsoft.sharepoint-df.com//.default" };
            var scopes = new[] { "https://graph.microsoft.com/.default" }; // https://graph.microsoft.com
            string loginHint = "YourAlias@microsoft.com";
            Prompt prompt = Prompt.SelectAccount; // use YourAlias@microsoft.com
            string resource = "https://microsoft.sharepoint-df.com//.default";

            AcquireTokenParameters.Builder tokenParamsBuilder = new AcquireTokenParameters.Builder()
                                           .StartAuthorizationFromActivity(this)
                                           .WithPrompt(prompt);

            if (!string.IsNullOrEmpty(loginHint))
            {
                tokenParamsBuilder.WithLoginHint(loginHint);
            }

            tokenParamsBuilder.WithScopes(scopes);

            // tokenParamsBuilder.WithResource(resource);
            tokenParamsBuilder.WithCallback(new InteractiveAuthCallback(
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

            AcquireTokenParameters tokenParams = (AcquireTokenParameters)tokenParamsBuilder.Build();

            this.RunOnUiThread(() =>
            {
                _spca.AcquireToken(tokenParams);
            });

        }

        void TestCase_6683()
        {
            // var scopes = new[] { "User.Read" };
            // var scopes = new[] { "https://microsoft.sharepoint-df.com//.default" };
            var scopes = new[] { "https://graph.microsoft.com/.default" }; // https://graph.microsoft.com
            string loginHint = "YourAlias@microsoft.com";
            Prompt prompt = Prompt.Login; // use YourAlias@microsoft.com
            string resource = "https://microsoft.sharepoint-df.com//.default";

            AcquireTokenParameters.Builder tokenParamsBuilder = new AcquireTokenParameters.Builder()
                                           .StartAuthorizationFromActivity(this)
                                           .WithPrompt(prompt);

            if (!string.IsNullOrEmpty(loginHint))
            {
                tokenParamsBuilder.WithLoginHint(loginHint);
            }

            tokenParamsBuilder.WithScopes(scopes);

            // tokenParamsBuilder.WithResource(resource);
            tokenParamsBuilder.WithCallback(new InteractiveAuthCallback(
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

            AcquireTokenParameters tokenParams = (AcquireTokenParameters)tokenParamsBuilder.Build();

            this.RunOnUiThread(() =>
            {
                _spca.AcquireToken(tokenParams);
            });
        }

        private void TestCase_6692()
        {
            // use non-joined account. 
            // Expected - prompt for the user
            // var scopes = new[] { "User.Read" };
            // var scopes = new[] { "https://microsoft.sharepoint-df.com//.default" };
            var scopes = new[] { "https://graph.microsoft.com/.default" }; // https://graph.microsoft.com
            string loginHint = "";
            Prompt prompt = Prompt.Login; // use IDLAB@msidlab4.onmicrosoft.com
            string resource = "https://microsoft.sharepoint-df.com//.default";

            AcquireTokenParameters.Builder tokenParamsBuilder = new AcquireTokenParameters.Builder()
                                           .StartAuthorizationFromActivity(this)
                                           .WithPrompt(prompt);

            if (!string.IsNullOrEmpty(loginHint))
            {
                tokenParamsBuilder.WithLoginHint(loginHint);
            }

            tokenParamsBuilder.WithScopes(scopes);

            // tokenParamsBuilder.WithResource(resource);
            tokenParamsBuilder.WithCallback(new InteractiveAuthCallback(
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

            AcquireTokenParameters tokenParams = (AcquireTokenParameters)tokenParamsBuilder.Build();

            this.RunOnUiThread(() =>
            {
                _spca.AcquireToken(tokenParams);
            });

        }

        void TestCase_795986()
        {
            // use non-joined account. 
            // Expected - prompt for the user
            // var scopes = new[] { "User.Read" };
            // var scopes = new[] { "https://microsoft.sharepoint-df.com//.default" };
            var scopes = new[] { "https://graph.microsoft.com/.default" }; // https://graph.microsoft.com
            string loginHint = "YourAlias@microsoft.com";
            Prompt prompt = Prompt.Login; // use IDLAB@msidlab4.onmicrosoft.com
            string resource = "https://microsoft.sharepoint-df.com//.default";

            AcquireTokenParameters.Builder tokenParamsBuilder = new AcquireTokenParameters.Builder()
                                           .StartAuthorizationFromActivity(this)
                                           .WithPrompt(prompt);

            if (!string.IsNullOrEmpty(loginHint))
            {
                tokenParamsBuilder.WithLoginHint(loginHint);
            }

            tokenParamsBuilder.WithScopes(scopes);

            // tokenParamsBuilder.WithResource(resource);
            tokenParamsBuilder.WithCallback(new InteractiveAuthCallback(
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

            AcquireTokenParameters tokenParams = (AcquireTokenParameters)tokenParamsBuilder.Build();

            this.RunOnUiThread(() =>
            {
                _spca.AcquireToken(tokenParams);
            });
        }

        void TestCase_795992()
        {
            // use non-joined account. 
            // Expected - prompt for the user
            // var scopes = new[] { "User.Read" };
            // var scopes = new[] { "https://microsoft.sharepoint-df.com//.default" };
            var scopes = new[] { "https://graph.microsoft.com/.default" }; // https://graph.microsoft.com
            string loginHint = "";
            Prompt prompt = Prompt.SelectAccount; // use IDLAB@msidlab4.onmicrosoft.com
            string resource = "https://microsoft.sharepoint-df.com//.default";

            AcquireTokenParameters.Builder tokenParamsBuilder = new AcquireTokenParameters.Builder()
                                           .StartAuthorizationFromActivity(this)
                                           .WithPrompt(prompt);

            if (!string.IsNullOrEmpty(loginHint))
            {
                tokenParamsBuilder.WithLoginHint(loginHint);
            }

            tokenParamsBuilder.WithScopes(scopes);

            // tokenParamsBuilder.WithResource(resource);
            _ = tokenParamsBuilder.WithCallback(new InteractiveAuthCallback(
                    onCancelAction: () => LogMessage("Auth cancelled"),
                    onErrorAction: (ex) => LogMessage(ex.ToString()),
                    onSuccessAction: (result) =>
                    {
                        _account = result.Account;
                        LogMessage(
                            $"Success!! Token for {result.Account.Username}," +
                            $" tenant {result.TenantId} - " +
                            $" token {result.AccessToken} ");

                        Task.Factory.StartNew(() =>
                        {
                            string authority = @"https://login.microsoftonline.com/common";
                            var resultSilent = _spca.AcquireTokenSilent(scopes, _account, authority);
                            LogMessage(resultSilent.AccessToken);
                        });
                    }));

            AcquireTokenParameters tokenParams = (AcquireTokenParameters)tokenParamsBuilder.Build();

            this.RunOnUiThread(() =>
            {
                _spca.AcquireToken(tokenParams);
            });
        }

        private void SilentBtn_Click(object sender, EventArgs e)
        {
            var cb = new SilentAuthCallback(
                    onErrorAction: (ex) => 
                    LogMessage("Error! " + ex.ToString()),
                     onSuccessAction: (result) =>
                     {
                         _account = result.Account;
                         LogMessage(
                             $"Success!! Token for {result.Account.Username}," +
                             $" tenant {result.TenantId} - " +
                             $" token {result.AccessToken} ");
                     });

            _textView.Text = "";
            // TODO: try out the AcquireTokenSilent version as well, it's supposed to be for background thread
            _ipca.AcquireTokenSilentAsync(
                new[] { "User.Read" },
                _account,
                "https://login.microsoftonline.com/f645ad92e38d4d1ab510d1b09a74a8ca",
                cb
                );
            Task.WaitAll();

        }

        private void DeviceCodeBtn_Click(object sender, EventArgs e)
        {
            _textView.Text = "";
            _ipca.AcquireTokenWithDeviceCode(
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
            if (_ipca == null)
            {
                return;
            }

            _ipca.GetAccounts(
                new MultipleAccountApplicationCurrentAccountCallback(
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

        internal class LoggerCallback : Java.Lang.Object, ILoggerCallback
        {
            MainActivity _mainActivity;

            public LoggerCallback(MainActivity mainActivity)
            {
                _mainActivity = mainActivity;
            }

            public void Log(string p0, Logger.LogLevel p1, string p2, bool p3)
            {
                _mainActivity.LogMessage(p0 + " " + p2);
            }
        }

        internal class SignOutCallback : Java.Lang.Object, IMultipleAccountPublicClientApplicationRemoveAccountCallback
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
            public void OnRemoved()
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

        internal class MultipleAccountApplicationCurrentAccountCallback : LoadAccountsFix.LoadAccountsCallback
        //Java.Lang.Object,
        //IPublicClientApplicationLoadAccountsCallback
        {
            private readonly Action<IAccount, IAccount> _onAccountChangedAction;
            private readonly Action<IAccount> _onAccountLoaded;
            private readonly Action<Java.Lang.Object> _onException;
            private readonly Action<MsalException> _onMsalException;

            public MultipleAccountApplicationCurrentAccountCallback(
                Action<IAccount, IAccount> onAccountChangedAction,
                Action<IAccount> onAccountLoaded,
                Action<Java.Lang.Object> onException)
            {
                _onAccountChangedAction = onAccountChangedAction;
                _onAccountLoaded = onAccountLoaded;
                _onException = onException;
            }

            public void OnError(MsalException ex)
            {
                _onMsalException(ex);
            }

            public void OnError(Java.Lang.Object p0)
            {
                _onException(p0);
            }

            public void OnTaskCompleted(IList<IAccount> result)
            {
                _onAccountLoaded(result.FirstOrDefault());
            }

            public void OnTaskCompleted(Java.Lang.Object p0)
            {
                //throw new NotImplementedException();
            }
        }

        internal class MultipleAccountApplicationCreatedListener :
            Java.Lang.Object, IPublicClientApplicationMultipleAccountApplicationCreatedListener
        {
            private readonly Action<IMultipleAccountPublicClientApplication> _onCreatedAction;
            private readonly Action<MsalException> _onExceptionAction;

            public MultipleAccountApplicationCreatedListener(
                Action<IMultipleAccountPublicClientApplication> onCreatedAction,
                Action<MsalException> onExceptionAction)
            {
                _onCreatedAction = onCreatedAction;
                _onExceptionAction = onExceptionAction;
            }

            public void OnCreated(IMultipleAccountPublicClientApplication pca)
            {
                _onCreatedAction(pca);
            }

            public void OnError(MsalException ex)
            {
                _onExceptionAction(ex);
            }
        }
        #endregion

        public void LogMessage(string message)
        {
            this.RunOnUiThread(
                () =>
                {
                    _textView.Text = (_textView.Text ?? "") + "\n" + message;
                    Android.Util.Log.Info("MSAL",message);
                });
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
#pragma warning restore CA2000 // Dispose objects before losing scope
    }
}


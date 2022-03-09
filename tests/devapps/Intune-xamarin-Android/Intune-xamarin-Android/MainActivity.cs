using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Microsoft.Identity.Client;
using Android.Content;
using Microsoft.Intune.Mam.Policy;
using Microsoft.Intune.Mam.Client.App;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Intune_xamarin_Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        /// <summary>
        /// The authority for the MSAL PublicClientApplication. Sign in will use this URL.
        /// </summary>
        private const string _authority = "https://login.microsoftonline.com/organizations";

        private static string _clientID = "6d50af5d-2529-4ff4-912f-c1d6ad06953e";

        private static string _redirectURI = $"msauth://com.sameerk.intune.taskr.xamarin/EHyvOdXj4uLXJXDaOMy5lwANmp0=";
        private static string _tenantID = "7257a09f-53cc-4a91-aca8-0cb6713642a5";

        /// <summary>
        /// Identifier of the target resource that is the recipient of the requested token.
        /// </summary>
        internal static string[] Scopes = { "https://graph.microsoft.com/User.Read" };
        static string[] clientCapabilities = { "protapp" };

        internal static IPublicClientApplication PCA { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            
            Android.Widget.Button actButton = FindViewById<Android.Widget.Button>(Resource.Id.acqToken);
            actButton.Click += ActButton_Click;

            Android.Widget.Button signOutButton = FindViewById<Android.Widget.Button>(Resource.Id.signOut);
            signOutButton.Click += SignOutButton_Click;
        }

        private async void ActButton_Click(object sender, EventArgs e)
        {
            bool useLab20 = true;
            if (useLab20)
            {
                // idlab20truemamca@msidlab20.onmicrosoft.com
                // Config
                _clientID = "6d50af5d-2529-4ff4-912f-c1d6ad06953e"; // my app in the lab
                _redirectURI = $"msauth://com.sameerk.intune.test.xamarin/EHyvOdXj4uLXJXDaOMy5lwANmp0=";
                // 
                _tenantID = "7257a09f-53cc-4a91-aca8-0cb6713642a5";
                Scopes[0] = "api://09aec9b9-0b0f-488a-81d6-72fd13a3a1c1/Hello.World"; // needs admin consent
            }
            else
            {
                // xammamtrust@msidlab4.onmicrosoft.com

                // TODOs
                // Lab4
                // Wiki
                // Shane about the dying broker + company portal
                // Lab4
                _clientID = "bd9933c9-a825-4f9a-82a0-bbf23c9049fd";
                _redirectURI = $"msauth://com.sameerk.intune.test.xamarin/EHyvOdXj4uLXJXDaOMy5lwANmp0=";
                _tenantID = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
                Scopes[0] = "api://a8bf4bd3-c92d-44d0-8307-9753d975c21e/Hello.World"; // needs admin consent
            }


            // Build PCA
            if (PCA == null)
            {
                PCA = PublicClientApplicationBuilder
                    .Create(_clientID)
                    .WithAuthority(_authority)
                    .WithLogging(MSALLog, LogLevel.Info, true)
                    //.WithHttpClientFactory(new HttpSnifferClientFactory())
                    .WithBroker()
                    .WithClientCapabilities(clientCapabilities)
                    .WithTenantId(_tenantID)
                    .WithRedirectUri(_redirectURI)
                    .Build();
            }

            AuthenticationResult result = null;
            try
            {
                result = await PCA.AcquireTokenInteractive(Scopes)
                                        .WithParentActivityOrWindow(this)
                                        .WithUseEmbeddedWebView(true)
                                        .ExecuteAsync()
                                        .ConfigureAwait(false);

                
                ShowMessage("Silent 1", result.AccessToken);
            }
            catch (IntuneAppProtectionPolicyRequiredException exProtection)
            {
                DoMAMRegister(exProtection);
                try
                {
                    result = await DoSilentAsync(Scopes).ConfigureAwait(false);
                    ShowMessage("Silent 2", result.AccessToken);
                }
                catch (Exception ex)
                {
                    ShowMessage("Exception 1", ex.Message);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Exception 2", ex.Message);
            }
        }

        private async void SignOutButton_Click(object sender, EventArgs e)
        {
            var accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            IMAMEnrollmentManager mgr = MAMComponents.Get<IMAMEnrollmentManager>();
            while (accounts.Any())
            {
                var acct = accounts.FirstOrDefault();
                // this will wipe and close the app
                mgr.UnregisterAccountForMAM(acct.Username);
                await PCA.RemoveAsync(acct).ConfigureAwait(false);
                accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            }
        }

        private static void DoMAMRegister(IntuneAppProtectionPolicyRequiredException exProtection)
        {
            IntuneSampleApp.MAMRegsiteredEvent.Reset();
            Task.Run(() =>
            {
                IMAMComplianceManager mgr = MAMComponents.Get<IMAMComplianceManager>();
                mgr.RemediateCompliance(exProtection.Upn, exProtection.AccountUserId, exProtection.TenantId, exProtection.AuthorityUrl, false);
            });

            IntuneSampleApp.MAMRegsiteredEvent.WaitOne();
        }

        internal static async Task<AuthenticationResult> DoSilentAsync(string[] scopes)
        {
            if (PCA == null)
            {
                return null;
            }

            var accts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            var acct = accts.FirstOrDefault();
            if (acct != null)
            {
                var silentParamBuilder = PCA.AcquireTokenSilent(scopes, acct);
                var authResult = await silentParamBuilder
                                            .ExecuteAsync().ConfigureAwait(false);
                return authResult;
            }
            else
            {
                throw new MsalUiRequiredException("ErrCode", "ErrMessage");
            }
        }

        private static void MSALLog(LogLevel level, string message, bool containsPii)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        private void ShowMessage(string title, string message)
        {
            Looper.Prepare();

            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            builder.SetTitle(title);
            builder.SetMessage(message);
            builder.SetNeutralButton("OK", (s, e) => { });

            var alertDialog = builder.Show();
            alertDialog.Show();

            System.Diagnostics.Debug.WriteLine($"Title = {title}  Message = {message}");
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
        }
    }
}

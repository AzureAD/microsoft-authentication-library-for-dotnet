using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Microsoft.Identity.Client;
using Android.Content;
using Microsoft.Intune.Mam.Policy;
using Microsoft.Intune.Mam.Client.App;
using System.Threading.Tasks;

namespace Intune_xamarin_Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        /// <summary>
        /// The scopes that are protected by conditional access
        /// </summary>
        internal static string[] Scopes = { "api://a8bf4bd3-c92d-44d0-8307-9753d975c21e/Hello.World" }; // TODO - change scopes are per your enterprise app

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

        /// <summary>
        /// This method shows calling pattern to access resource protected by Conditional Access with App Protection Policy
        /// </summary>
        /// <param name="sender">Sender button</param>
        /// <param name="e">arguments</param>
        private async void ActButton_Click(object sender, EventArgs e)
        {
            AuthenticationResult result = null;

            try
            {
                // attempt silent login.
                // If this is very first time and the device is not enrolled, it will throw MsalUiRequiredException
                // If the device is enrolled, this will succeed.
                result = await PCAWrapper.Instance.DoSilentAsync(Scopes).ConfigureAwait(false);

                _ = await ShowMessage("Silent 1", result.AccessToken).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException )
            {
                try
                {
                    // This executes UI interaction
                    result = await PCAWrapper.Instance.DoInteractiveAsync(Scopes, this).ConfigureAwait(false);

                    _ = await ShowMessage("Interactive 1", result.AccessToken).ConfigureAwait(false);
                }
                catch (IntuneAppProtectionPolicyRequiredException exProtection)
                {
                    // if the scope requires App Protection Policy,  IntuneAppProtectionPolicyRequiredException is thrown.
                    // Perform registration operation here and then do the silent token acquisition
                    _ = await DoMAMRegister(exProtection).ContinueWith(async (s) =>
                      {
                          try
                          {
                              // Now the device is registered, perform silent token acquisition
                              result = await PCAWrapper.Instance.DoSilentAsync(Scopes).ConfigureAwait(false);

                              _ = await ShowMessage("Silent 2", result.AccessToken).ConfigureAwait(false) ;
                          }
                          catch (Exception ex)
                          {
                              _ = await ShowMessage("Exception 1", ex.Message).ConfigureAwait(false);
                          }
                      }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _ = await ShowMessage("Exception 2", ex.Message).ConfigureAwait(false);
            }
        }

        private async void SignOutButton_Click(object sender, EventArgs e)
        {
            // Even after the signout, broker may retain the token
            await PCAWrapper.Instance.SignOut().ConfigureAwait(false);
        }

        /// <summary>
        /// Perform registration with MAM
        /// </summary>
        /// <param name="exProtection"></param>
        /// <returns></returns>
        private async Task DoMAMRegister(IntuneAppProtectionPolicyRequiredException exProtection)
        {
            // reset the registered event
            IntuneSampleApp.MAMRegsiteredEvent.Reset();
            
            // Invoke compliance API on a different thread
            await Task.Run(() =>
                                {
                                    IMAMComplianceManager mgr = MAMComponents.Get<IMAMComplianceManager>();
                                    mgr.RemediateCompliance(exProtection.Upn, exProtection.AccountUserId, exProtection.TenantId, exProtection.AuthorityUrl, false);
                                }).ConfigureAwait(false);

            // wait till the registration completes
            // Note: This is a sample app for MSAL.NET. Scenarios such as what if enrollment fails or user chooses not to enroll will be as
            // per the business requirements of the app and not considered in the sample app.
            IntuneSampleApp.MAMRegsiteredEvent.WaitOne();
        }

        private Task<bool> ShowMessage(string title, string message)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            RunOnUiThread(() =>
            {

                var builder = new Android.App.AlertDialog.Builder(this);
                builder.SetTitle(title);
                builder.SetMessage(message);
                builder.SetNeutralButton("OK", (s, e) => { tcs.SetResult(true); });

                var alertDialog = builder.Show();
                alertDialog.Show();
            });
            return tcs.Task;
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

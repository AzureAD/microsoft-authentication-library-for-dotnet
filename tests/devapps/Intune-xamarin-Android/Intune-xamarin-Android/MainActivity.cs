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
        internal static string[] Scopes = { "api://09aec9b9-0b0f-488a-81d6-72fd13a3a1c1/Hello.World" };

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

                ShowMessage("Silent 1", result.AccessToken);
            }
            catch (MsalUiRequiredException )
            {
                try
                {
                    // This executes UI interaction
                    result = await PCAWrapper.Instance.DoInteractiveAsync(Scopes, this).ConfigureAwait(false);

                    ShowMessage("Interctive 1", result.AccessToken);
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

                              ShowMessage("Silent 2", result.AccessToken);
                          }
                          catch (Exception ex)
                          {
                              ShowMessage("Exception 1", ex.Message);
                          }
                      }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Exception 2", ex.Message);
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

            // wait till the registration takes place
            IntuneSampleApp.MAMRegsiteredEvent.WaitOne();
        }

        private void ShowMessage(string title, string message)
        {
            Looper.Prepare();

            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            builder.SetTitle(title);
            builder.SetMessage(message);
            builder.SetNeutralButton("OK", (s, e) => { });

            // somehow the dialog does not show up
            // looking at it
            var alertDialog = builder.Show();
            alertDialog.Show();

            // Write to the debug window in the meantime
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

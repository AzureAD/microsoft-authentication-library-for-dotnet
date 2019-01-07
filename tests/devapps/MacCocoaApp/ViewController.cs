using System;

using AppKit;
using Foundation;
using Microsoft.Identity.Client;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MacCocoaApp
{
    public partial class ViewController : NSViewController
    {
        private const string ClientId = "0615b6ca-88d4-4884-8729-b178178f7c27";
        private const string Authority = "https://login.microsoftonline.com/common";
        private readonly string[] Scopes = new[] { "User.Read" };
        // Consider having a single object for the entire app.
        PublicClientApplication _pca;


        public ViewController(IntPtr handle) : base(handle)
        {
            // use a simple file cache (alternatively, use no cache and the app will lose all tokens if restarted)
            _pca = new PublicClientApplication(ClientId, Authority, UserTokenCache.GetUserTokenCache()); 


            Logger.LogCallback = (lvl, msg, pii)=>
            {
                Console.WriteLine($"MSAL {lvl} {pii} {msg}");
                Console.ResetColor();
            };
            Logger.Level = LogLevel.Verbose;
            Logger.PiiLoggingEnabled = true;
        }             

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Do any additional setup after loading the view.
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }

      
#pragma warning disable AvoidAsyncVoid // Avoid async void
        async partial void GetTokenClickAsync(NSObject sender)
#pragma warning restore AvoidAsyncVoid // Avoid async void
        {
            try
            {
                AuthenticationResult result = null;
                var existingAccounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
                var firstExistingAccount = existingAccounts.FirstOrDefault();

                if (firstExistingAccount != null)
                {
                    try
                    {
                        result = await _pca.AcquireTokenSilentAsync(Scopes, firstExistingAccount).ConfigureAwait(false);
                    }
                    catch (MsalUiRequiredException)
                    {
                        Console.WriteLine("No token found the in the cache, need to call AcquireTokenAsync");                
                    }
                }

                if (result == null)
                {
                    result = await _pca.AcquireTokenAsync(Scopes).ConfigureAwait(false);
                }

                UpdateStatus($"Access token acquired: {result.AccessToken}");

            }
            catch (Exception e)
            {
                UpdateStatus("Unexpected error: " + 
                    e.Message + Environment.NewLine + e.StackTrace);               
            }
        }



        async partial void ShowCacheStatusAsync(NSObject sender)
        {
            var existingAccounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            if (!existingAccounts.Any())
            {
                UpdateStatus("There are no tokens in the cache");
            }
            else
            {
                UpdateStatus($"There are {existingAccounts.Count()} accounts in the cache." +
                	$" {Environment.NewLine} {string.Join(Environment.NewLine, existingAccounts)} ");
            }
        }

        async partial void ClearCacheClickAsync(Foundation.NSObject sender)
        {
            var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            foreach (var acc in accounts)
            {
                await _pca.RemoveAsync(acc).ConfigureAwait(false);
            }

            ShowCacheStatusAsync(sender);
        }

        async partial void GetTokenDeviceCodeAsync(Foundation.NSObject sender)
        {
            try
            {
                // Left out checking the token cache for clarity
                var result = await _pca.AcquireTokenWithDeviceCodeAsync(
                                   Scopes,
                                   deviceCodeResult =>
                                   {
                                       UpdateStatus(deviceCodeResult.Message);
                                       return Task.FromResult(0);
                                   }).ConfigureAwait(false);

                UpdateStatus($"Access token acquired: {result.AccessToken}");
            }
            catch (Exception e)
            {
                UpdateStatus("Unexpected error: " +
                    e.Message + Environment.NewLine + e.StackTrace);
            }


        }

        private void UpdateStatus(string message)
        {
            NSRunLoop.Main.BeginInvokeOnMainThread(() =>
                    this.OutputLabel.StringValue = message);

        }
    }
}

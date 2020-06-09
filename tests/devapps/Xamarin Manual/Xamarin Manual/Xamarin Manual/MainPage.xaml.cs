using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Xamarin_Manual
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private IPublicClientApplication _pca;
        public static object CurrentActivity { get; set; }

        /// <summary>
        /// The ClientID is the Application ID found in the portal (https://go.microsoft.com/fwlink/?linkid=2083908). 
        /// You can use the below id however if you create an app of your own you should replace the value here.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            pckWebView.SelectedIndex = 0;

            CreatePca();
        }

        private async void atsBtn_Clicked(object sender, EventArgs ea)
        {
            try
            {
                var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
                var result = await _pca.AcquireTokenSilent(AuthConfig.Scopes, accounts.FirstOrDefault()).ExecuteAsync().ConfigureAwait(false);
                UpdateStatus($"ATS Token! Valid for: {(result.ExpiresOn - DateTimeOffset.Now).TotalMinutes} min");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                UpdateStatus("Failed! " + e);
            }
        }

        // this is for testing purposes only, product code should AcquireTokenSilent and if that fails
        // it should fallback to AcquireTokenInteractive
        private async void atiBtn_Clicked(object sender, EventArgs ea)
        {
            try
            {
                var builder = _pca
                    .AcquireTokenInteractive(AuthConfig.Scopes)
                    .WithParentActivityOrWindow(CurrentActivity);                    

                builder = ConfigureWebview(builder);

                var result = await builder
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                UpdateStatus($"ATI Token! Valid for: {(result.ExpiresOn - DateTimeOffset.Now).TotalMinutes} min");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                UpdateStatus("Failed! " + e);
            }
        }

        private AcquireTokenInteractiveParameterBuilder ConfigureWebview(AcquireTokenInteractiveParameterBuilder builder)
        {
            switch ((string)pckWebView.SelectedItem)
            {
                case "System":
                    builder = builder.WithUseEmbeddedWebView(false);
                    break;
                case "Embedded":
                    builder = builder.WithUseEmbeddedWebView(true);
                    break;

                default:
                    throw new NotImplementedException();
            }

            return builder;
        }


        private void UpdateStatus(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                { lblStatus.Text = message; }
            );
        }


        private void CreatePca()
        {
            _pca = PublicClientApplicationBuilder.Create(AuthConfig.ClientID)
              .WithRedirectUri(AuthConfig.BrokerRedirectUri) 
              .WithIosKeychainSecurityGroup("com.microsoft.adalcache")
              .WithLogging(
                    (lvl, msg, pii) => Trace.WriteLine($"[{lvl}] {msg}"), LogLevel.Verbose, true)
              .WithBroker(swBroker.IsToggled)
              .Build();
        }

        private void swBroker_Toggled(object sender, ToggledEventArgs e)
        {
            CreatePca();
        }

        private async void clearCacheBtn_Clicked(object sender, EventArgs e)
        {
            var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            foreach (var acc in accounts)
            {
                await _pca.RemoveAsync(acc).ConfigureAwait(false);
            }

            showCacheBtn_Clicked(null, e);
        }

        private async void showCacheBtn_Clicked(object sender, EventArgs e)
        {
            var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            string accDescription;
            if (accounts.Any())
            {
                accDescription = string.Join(Environment.NewLine, accounts.Select(a => a.Username));
            }
            else
            {
                accDescription = "No accounts";
            }

            UpdateStatus(accDescription);
        }
    }
}

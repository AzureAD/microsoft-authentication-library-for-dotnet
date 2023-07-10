// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading;
using Windows.Security.Authentication.Web;
using System.Diagnostics;
using System.Globalization;
using System.Text;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
namespace UWP_standalone
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Async event handlers should not return task")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Async event handlers should not return task")]
    public sealed partial class MainPage : Page
    {
        private static readonly string s_clientID = "1d18b3b0-251b-4714-a02a-9956cec86c2d";
        private static readonly string s_authority = "https://login.microsoftonline.com/common/";
        private static readonly IEnumerable<string> s_scopes = new[] { "user.read" };
        private const string CacheFileName = "msal_user_cache.json";

        public MainPage()
        {
            InitializeComponent();

            // returns something like s-1-15-2-2601115387-131721061-1180486061-1362788748-631273777-3164314714-2766189824
            string sid = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host;

            // use uppercase S
            sid = sid.Replace('s', 'S');

            // the redirect URI
            string redirectUri = $"ms-appx-web://microsoft.aad.brokerplugin/{sid}";
        }

        private IPublicClientApplication CreatePublicClient()
        {
            var pca = PublicClientApplicationBuilder.Create(s_clientID)
                .WithAuthority(s_authority)
                .WithBroker(chkUseBroker.IsChecked.Value)
                //.WithWindowsBrokerOptions(new WindowsBrokerOptions() { HeaderText = "aaa" })
                .WithLogging((x, y, z) => Debug.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
                .Build();

            SynchronizedEncryptedFileMsalCache cache = new SynchronizedEncryptedFileMsalCache();
            cache.Initialize(pca.UserTokenCache);

            return pca;
        }

        private async void AcquireTokenIWA_ClickAsync(object sender, RoutedEventArgs e)
        {
            var pca = CreatePublicClient();
            AuthenticationResult result = null;
            try
            {
                result = await pca.AcquireTokenByIntegratedWindowsAuth(s_scopes).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await DisplayErrorAsync(ex).ConfigureAwait(false);
                return;
            }

            await DisplayResultAsync(result).ConfigureAwait(false);
        }

        private async void GetAccountsAsync(object sender, RoutedEventArgs e)
        {
            var pca = CreatePublicClient();
            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Getting accounts ...");
            foreach (IAccount account in accounts)
            {
                sb.AppendLine($"{account.Username} .... from {account.Environment}");
            }

            sb.AppendLine("Done getting accounts.");

            await DisplayMessageAsync(sb.ToString()).ConfigureAwait(false);
        }

        private async void ExpireAtsAsync(object sender, RoutedEventArgs e)
        {
            var pca = CreatePublicClient();

            await (pca.UserTokenCache as TokenCache).ExpireAllAccessTokensForTestAsync().ConfigureAwait(false);

            await DisplayMessageAsync("Done expiring tokens.").ConfigureAwait(false);
        }

        private async void ClearCacheAsync(object sender, RoutedEventArgs e)
        {
            var pca = CreatePublicClient();

            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Clearing the cache ...");

            foreach (IAccount account in accounts)
            {
                await pca.RemoveAsync(account).ConfigureAwait(false);
            }

            sb.AppendLine("Done clearing the cache.");
            await DisplayMessageAsync(sb.ToString()).ConfigureAwait(false);

        }

        private async void ATS_ClickAsync(object sender, RoutedEventArgs e)
        {
            var pca = CreatePublicClient();
            var upnPrefix = tbxUpn.Text;

            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            var acc = accounts.SingleOrDefault(a => a.Username.StartsWith(upnPrefix));

            AuthenticationResult result = null;
            try
            {
                result = await pca
                    .AcquireTokenSilent(s_scopes, acc)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await DisplayErrorAsync(ex).ConfigureAwait(false);
                return;
            }

            await DisplayResultAsync(result).ConfigureAwait(false);

        }

        private async void ATI_ClickAsync(object sender, RoutedEventArgs e)
        {
            var pca = CreatePublicClient();
            var upnPrefix = tbxUpn.Text;

            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(true); // stay on UI thread
            var acc = accounts.SingleOrDefault(a => a.Username.StartsWith(upnPrefix));

            try
            {
                var result = await pca.AcquireTokenInteractive(s_scopes)
                    .WithAccount(acc)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                await DisplayResultAsync(result).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                await DisplayErrorAsync(ex).ConfigureAwait(false);
                return;
            }

        }

        private async void ATIDesktop_ClickAsync(object sender, RoutedEventArgs e)
        {
            var pca = PublicClientApplicationBuilder.Create(s_clientID)
                .WithAuthority(s_authority)
                .WithBroker(chkUseBroker.IsChecked.Value)
                .WithLogging((x, y, z) => Debug.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
                .Build();

            SynchronizedEncryptedFileMsalCache cache = new SynchronizedEncryptedFileMsalCache();
            cache.Initialize(pca.UserTokenCache);

            var upnPrefix = tbxUpn.Text;

            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(true); // stay on UI thread
            var acc = accounts.SingleOrDefault(a => a.Username.StartsWith(upnPrefix));

            try
            {
                var result = await pca.AcquireTokenInteractive(s_scopes)
                    .WithAccount(acc)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                await DisplayResultAsync(result).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                await DisplayErrorAsync(ex).ConfigureAwait(false);
                return;
            }
        }

        private async Task DisplayErrorAsync(Exception ex)
        {
            await DisplayMessageAsync(ex.ToString()).ConfigureAwait(false);
        }

        private async Task DisplayResultAsync(AuthenticationResult result)
        {
            await DisplayMessageAsync($"Signed in User - {result.Account.Username}\nAccess token from {result.AuthenticationResultMetadata.TokenSource}: \n{result.AccessToken}").ConfigureAwait(false);
        }

        private async Task DisplayMessageAsync(string message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                   () =>
                   {
                       Log.Text = message;
                   });
        }
    }
}

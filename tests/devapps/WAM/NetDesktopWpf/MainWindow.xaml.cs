using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Desktop;

namespace NetDesktopWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string s_clientID = "1d18b3b0-251b-4714-a02a-9956cec86c2d";
        private static readonly string s_authority = "https://login.microsoftonline.com/common/";
        private static readonly IEnumerable<string> s_scopes = new[] { "user.read" };
        private const string UserCacheFile = "msal_user_cache.json";

        public MainWindow()
        {
            InitializeComponent();
        }

        private IPublicClientApplication CreatePublicClient()
        {
            var pca = PublicClientApplicationBuilder.Create(s_clientID)
                .WithAuthority(s_authority)
                .WithWindowsBroker(true)
                .WithLogging((x, y, z) => Debug.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
                .Build();

            BindCache(pca.UserTokenCache, UserCacheFile);

            return pca;
        }

        private IPublicClientApplication CreatePublicClientForRuntime()
        {
            var pca = PublicClientApplicationBuilder.Create(s_clientID)
                .WithAuthority(s_authority)
                .WithBrokerPreview(true)
                .WithLogging((x, y, z) => Debug.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
                .Build();

            BindCache(pca.UserTokenCache, UserCacheFile);

            return pca;
        }

        private static void BindCache(ITokenCache tokenCache, string file)
        {
            tokenCache.SetBeforeAccess(notificationArgs =>
            {
                notificationArgs.TokenCache.DeserializeMsalV3(File.Exists(file)
                    ? File.ReadAllBytes(UserCacheFile)
                    : null);
            });

            tokenCache.SetAfterAccess(notificationArgs =>
            {
                // if the access operation resulted in a cache update
                if (notificationArgs.HasStateChanged)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(file, notificationArgs.TokenCache.SerializeMsalV3());
                }
            });
        }

        private async void AtsAti_Click(object sender, RoutedEventArgs e)
        {
            var pca = CreatePublicClient();
            var upnPrefix = UpnTbx.Text;

            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(true);
            var acc = accounts.SingleOrDefault(
                a => !String.IsNullOrEmpty(upnPrefix) &&
                a.Username.StartsWith(upnPrefix));

            AuthenticationResult result = null;
            try
            {
                result = await pca
                    .AcquireTokenSilent(s_scopes, acc)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    var task = await Dispatcher.InvokeAsync(() =>
                        pca.AcquireTokenInteractive(s_scopes)
                                     .WithAccount(acc)
                                     .ExecuteAsync());

                    result = await task.ConfigureAwait(false);

                }
                catch (Exception ex3)
                {
                    DisplayMessage(ex3.Message.ToString());
                    return;
                }
            }
            catch (Exception ex2)
            {
                DisplayMessage(ex2.Message.ToString());
                return;
            }

            DisplayMessage($"Success! We have a token for {result.Account.Username} valid until {result.ExpiresOn}");
        }

        private async void SignOut_Click(object sender, RoutedEventArgs e)
        {
            var pca = CreatePublicClient();
            var upnPrefix = UpnTbx.Text;

            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(true);
            var acc = accounts.FirstOrDefault();

            if (acc == null)
            {
                DisplayMessage($"No accounts to remove!!!");
                return;
            }

            try
            {
                await pca.RemoveAsync(acc).ConfigureAwait(false);
            }
            catch (MsalException ex1)
            {
                try
                {
                    DisplayMessage(ex1.Message.ToString());
                    return;
                }
                catch (Exception ex3)
                {
                    DisplayMessage(ex3.Message.ToString());
                    return;
                }
            }
            catch (Exception ex2)
            {
                DisplayMessage(ex2.ToString());
                return;
            }

            DisplayMessage($"Success! Account removed!!!");
        }

        private async void AtsAti_Runtime_Click(object sender, RoutedEventArgs e)
        {
            var pca = CreatePublicClientForRuntime();
            var upnPrefix = UpnTbx.Text;

            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(true);
            var acc = accounts.SingleOrDefault(
                a => !String.IsNullOrEmpty(upnPrefix) &&
                a.Username.StartsWith(upnPrefix));

            AuthenticationResult result = null;
            try
            {
                result = await pca
                    .AcquireTokenSilent(s_scopes, acc)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    IntPtr handle = new WindowInteropHelper(this).Handle;
                    var task = await Dispatcher.InvokeAsync(() =>
                        pca.AcquireTokenInteractive(s_scopes)
                                     .WithParentActivityOrWindow(handle)
                                     .WithAccount(acc)
                                     .ExecuteAsync());

                    result = await task.ConfigureAwait(false);

                }
                catch (MsalClientException ex1)
                {
                    DisplayMessage(ex1.Message.ToString());
                    return;
                }
                catch (Exception ex3)
                {
                    DisplayMessage(ex3.Message.ToString());
                    return;
                }
            }
            catch (Exception ex2)
            {
                DisplayMessage(ex2.Message.ToString());
                return;
            }

            DisplayMessage($"Success! We have a token for {result.Account.Username} valid until {result.ExpiresOn}");
        }

        private async void GetAccounts_Click(object sender, RoutedEventArgs e)
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

            DisplayMessage(sb.ToString());
        }

        private void DisplayMessage(string message)
        {
            Dispatcher.Invoke(
                   () =>
                   {
                       Log.Text = message;
                   });
        }

        private async void ClearCache(object sender, RoutedEventArgs e)
        {
            DisplayMessage("Clearing the cache ...");
            var pca = CreatePublicClient();
            foreach (var acc in (await pca.GetAccountsAsync().ConfigureAwait(false)))
            {
                await pca.RemoveAsync(acc).ConfigureAwait(false);
            }

            DisplayMessage("Done clearing the cache.");
        }

        private void Clear_Button_Click(object sender, RoutedEventArgs e)
        {
            DisplayMessage("");
        }
    }
}

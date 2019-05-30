using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AuthenticationResult = Microsoft.Identity.Client.AuthenticationResult;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IPublicClientApplication _pca;
        //TODO...update the commented ClientId to have the webAuthBroker redirectUri
        private static readonly string s_clientID = "3082fdc1-da23-4881-bd1d-1d11c7f0cb38"; //"9058d700-ccd7-4dd4-a029-aec31995add0";
        private static readonly string s_authority = "https://login.microsoftonline.com/common/";
        private static readonly IEnumerable<string> s_scopes = new[] { "user.read" };
        private const string CacheFileName = "msal_user_cache.json";

        private const string NullRedirectUri = "None - WEB redirect uri";
        private readonly ObservableCollection<string> _redirectUris = new ObservableCollection<string>();

        public MainPage()
        {
            InitializeComponent();

            _redirectUris.Add(NullRedirectUri);
            _redirectUris.Add("https://MyDirectorySearcherApp");
        }

        private void CreatePublicClient()
        {
            _pca = PublicClientApplicationBuilder.Create(s_clientID)
               .WithAuthority(s_authority)
               .WithRedirectUri(GetRedirectUri())
               .Build();

            // custom serialization - this is very similar to what MSAL is doing
            // but extenders can implement their own cache.
            _pca.UserTokenCache.SetAfterAccess((tokenCacheNotifcation) =>
            {
                if (tokenCacheNotifcation.HasStateChanged)
                {
                    StorageFile cacheFile = ApplicationData.Current.LocalFolder.CreateFileAsync(
                        CacheFileName,
                        CreationCollisionOption.ReplaceExisting).AsTask().GetAwaiter().GetResult();

                    byte[] blob = tokenCacheNotifcation.TokenCache.SerializeMsalV3();
                    IBuffer buffer = DpApiProxy.SampleProtectAsync(blob, "LOCAL=user").GetAwaiter().GetResult();

                    FileIO.WriteBufferAsync(cacheFile, buffer).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                }
            });

            _pca.UserTokenCache.SetBeforeAccess((tokenCacheNotifcation) =>
            {
                IStorageFile cacheFile = (ApplicationData.Current.LocalFolder.TryGetItemAsync(CacheFileName)
                    .AsTask().ConfigureAwait(false).GetAwaiter().GetResult()) as IStorageFile;

                if (cacheFile != null)
                {
                    IBuffer contents = FileIO.ReadBufferAsync(cacheFile).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    var result = DpApiProxy.SampleUnprotectDataAsync(contents).ConfigureAwait(false).GetAwaiter().GetResult();

                    tokenCacheNotifcation.TokenCache.DeserializeMsalV3(result);
                }
            });

#if ARIA_TELEMETRY_ENABLED
            Telemetry.GetInstance().RegisterReceiver(
                (new Microsoft.Identity.Client.AriaTelemetryProvider.ServerTelemetryHandler()).OnEvents);
#endif
        }

        private string GetRedirectUri()
        {
            if (redirectUriCbx.SelectedValue == null)
            {
                return null;
            }

            string selectedRedirectUri = redirectUriCbx.SelectedValue.ToString();
            if (selectedRedirectUri == NullRedirectUri)
            {
                return null;
            }

            return selectedRedirectUri;
        }

        private async void AcquireTokenIWA_ClickAsync(object sender, RoutedEventArgs e)
        {
            CreatePublicClient();
            AuthenticationResult result = null;
            try
            {
                result = await _pca.AcquireTokenByIntegratedWindowsAuth(s_scopes).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await DisplayErrorAsync(ex).ConfigureAwait(false);
                return;
            }

            await DisplayResultAsync(result).ConfigureAwait(false);

        }

        private async void ShowCacheCountAsync(object sender, RoutedEventArgs e)
        {
            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            string message =
                $"There are {accounts.Count()} in the MSAL token cache. " +
                Environment.NewLine +
                string.Join(", ", accounts.Select(a => a.Username));

            await DisplayMessageAsync(message).ConfigureAwait(false);
            ;

        }

        private async void ClearCacheAsync(object sender, RoutedEventArgs e)
        {
            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            foreach (IAccount account in accounts)
            {
                await _pca.RemoveAsync(account).ConfigureAwait(false);
            }

            await DisplayMessageAsync("Cache Cleared").ConfigureAwait(false);
        }

        private async void ClearFirstAccountAsync(object sender, RoutedEventArgs e)
        {
            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            if (accounts.Any())
            {
                await _pca.RemoveAsync(accounts.First()).ConfigureAwait(false);
            }
        }

        private async void AccessTokenSilentButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (_pca == null)
            {
                await DisplayMessageAsync("No PCA created yet. Call acquire token interactive first. ").ConfigureAwait(false);
            }
            else
            {
                IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);

                AuthenticationResult result = null;
                try
                {
                    result = await _pca
                        .AcquireTokenSilent(s_scopes, accounts.FirstOrDefault())
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
        }

        private async void AccessTokenButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            CreatePublicClient();
            AuthenticationResult result = null;
            try
            {
                IEnumerable<IAccount> users = await _pca.GetAccountsAsync().ConfigureAwait(false);
                IAccount user = users.FirstOrDefault();

                result = await _pca.AcquireTokenInteractive(s_scopes)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        AccessToken.Text = "\nAccessToken: \n" + result.AccessToken;
                    });
            }
            catch (Exception ex)
            {
                await DisplayErrorAsync(ex).ConfigureAwait(false);
                return;
            }

            await DisplayResultAsync(result).ConfigureAwait(false);
        }

        private async Task DisplayErrorAsync(Exception ex)
        {
            await DisplayMessageAsync(ex.Message).ConfigureAwait(false);
        }

        private async Task DisplayResultAsync(AuthenticationResult result)
        {
            await DisplayMessageAsync("Signed in User - " + result.Account.Username + "\nAccessToken: \n" + result.AccessToken).ConfigureAwait(false);
        }


        private async Task DisplayMessageAsync(string message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                   () =>
                   {
                       AccessToken.Text = message;
                   });
        }
    }
}

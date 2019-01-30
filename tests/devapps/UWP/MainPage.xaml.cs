using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IPublicClientApplication _pca;
        private readonly AuthenticationContext _authenticationContext;
        private static readonly string ClientID = "9058d700-ccd7-4dd4-a029-aec31995add0";
        private static readonly string Authority = "https://login.microsoftonline.com/common/";
        private static readonly IEnumerable<string> Scopes = new[] { "https://graph.windows.net/.default" };
        private const string Resource = "https://graph.windows.net";


        public MainPage()
        {
            InitializeComponent();

            _pca = new PublicClientApplication(ClientID, Authority);

            // custom serialization
            _pca.UserTokenCache.SetAfterAccess((tokenCacheNotifcation) =>
            {
                if (tokenCacheNotifcation.HasStateChanged)
                {
                    StorageFile cacheFile = ApplicationData.Current.LocalFolder.CreateFileAsync(
                        "msal_cache.bin",
                        CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

                    byte[] blob = tokenCacheNotifcation.TokenCache.Serialize();
                    IBuffer buffer = DpApiProxy.SampleProtectAsync(blob, "LOCAL=user").ConfigureAwait(false).GetAwaiter().GetResult();

                    FileIO.WriteBufferAsync(cacheFile, buffer).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                }
            });

            _pca.UserTokenCache.SetBeforeAccess((tokenCacheNotifcation) =>
            {

                IStorageFile cacheFile = (ApplicationData.Current.LocalFolder.TryGetItemAsync("msal_cache.bin")
                    .AsTask().ConfigureAwait(false).GetAwaiter().GetResult()) as IStorageFile;

                if (cacheFile != null)
                {
                    IBuffer contents = FileIO.ReadBufferAsync(cacheFile).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    var result = DpApiProxy.SampleUnprotectDataAsync(contents).ConfigureAwait(false).GetAwaiter().GetResult();

                    tokenCacheNotifcation.TokenCache.Deserialize(result);
                }
            });

            _authenticationContext = new AuthenticationContext(Authority);

#if ARIA_TELEMETRY_ENABLED
            Telemetry.GetInstance().RegisterReceiver(
                (new Microsoft.Identity.Client.AriaTelemetryProvider.ServerTelemetryHandler()).OnEvents);
#endif
        }

        private async void AcquireTokenIWA_ClickAsync(object sender, RoutedEventArgs e)
        {
            AuthenticationResult result = null;
            try
            {
                result = await _pca.AcquireTokenByIntegratedWindowsAuthAsync(Scopes).ConfigureAwait(false);
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
                string.Join(", ", accounts.Select(a => a.Username)) +
                Environment.NewLine +
                $"There are { _authenticationContext.TokenCache.Count} items in the ADAL token cache. "
                + Environment.NewLine +
                string.Join(", ", _authenticationContext.TokenCache.ReadItems().Select(i => i.DisplayableId));

            await DisplayMessageAsync(message).ConfigureAwait(false); ;

        }

        private async void ClearCacheAsync(object sender, RoutedEventArgs e)
        {
            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            foreach (IAccount account in accounts)
            {
                await _pca.RemoveAsync(account).ConfigureAwait(false);
            }
        }

        private async void ClearFirstAccountAsync(object sender, RoutedEventArgs e)
        {
            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            if (accounts.Any())
            {
                await _pca.RemoveAsync(accounts.First()).ConfigureAwait(false);
            }
        }

        private async void ADALButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult result = await _authenticationContext.AcquireTokenAsync(
                "https://graph.windows.net",
                ClientID,
                new Uri("urn:ietf:wg:oauth:2.0:oob"),
                new PlatformParameters(PromptBehavior.SelectAccount, false))
                .ConfigureAwait(false);

            await DisplayMessageAsync("Signed in User - " + result.UserInfo.DisplayableId + "\nAccessToken: \n" + result.AccessToken)
                .ConfigureAwait(false);

        }

        private async void ADALSilentButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult result = await _authenticationContext.AcquireTokenSilentAsync(
                Resource,
                ClientID)
                .ConfigureAwait(false);

            await DisplayMessageAsync("Signed in User - " + result.UserInfo.DisplayableId + "\nAccessToken: \n" + result.AccessToken)
                .ConfigureAwait(false);

            await DisplayMessageAsync("Done " + i++).ConfigureAwait(false);

        }

        private static int i = 0;

        private async void AccessTokenSilentButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);

            AuthenticationResult result = null;
            try
            {
                result = await _pca.AcquireTokenSilentAsync(Scopes, accounts.FirstOrDefault()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await DisplayErrorAsync(ex).ConfigureAwait(false);
                return;
            }

            await DisplayResultAsync(result).ConfigureAwait(false);
        }

        private async void AccessTokenButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            AuthenticationResult result = null;
            try
            {
                IEnumerable<IAccount> users = await _pca.GetAccountsAsync().ConfigureAwait(false);
                IAccount user = users.FirstOrDefault();

                result = await _pca.AcquireTokenAsync(Scopes).ConfigureAwait(false);
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

        //public async Task ProtectAsync()
        //{
        //    // Initialize function arguments.
        //    String strMsg = "This is a message to be protected.";
        //    String strDescriptor = "LOCAL=user";
        //    BinaryStringEncoding encoding = BinaryStringEncoding.Utf8;

        //    // Protect a message to the local user.
        //    IBuffer buffProtected = await this.SampleProtectAsync(
        //        strMsg,
        //        strDescriptor,
        //        encoding).ConfigureAwait(false);

        //    // Decrypt the previously protected message.
        //    String strDecrypted = await this.SampleUnprotectDataAsync(
        //        buffProtected,
        //        encoding).ConfigureAwait(false);
        //}

        //public async Task<IBuffer> SampleProtectAsync(
        //    String strMsg,
        //    String strDescriptor,
        //    BinaryStringEncoding encoding)
        //{
        //    // Create a DataProtectionProvider object for the specified descriptor.
        //    DataProtectionProvider Provider = new DataProtectionProvider(strDescriptor);

        //    // Encode the plaintext input message to a buffer.
        //    encoding = BinaryStringEncoding.Utf8;
        //    IBuffer buffMsg = CryptographicBuffer.ConvertStringToBinary(strMsg, encoding);

        //    // Encrypt the message.
        //    IBuffer buffProtected = await Provider.ProtectAsync(buffMsg);

        //    // Execution of the SampleProtectAsync function resumes here
        //    // after the awaited task (Provider.ProtectAsync) completes.
        //    return buffProtected;
        //}

        //public async Task<String> SampleUnprotectDataAsync(
        //    IBuffer buffProtected,
        //    BinaryStringEncoding encoding)
        //{
        //    // Create a DataProtectionProvider object.
        //    DataProtectionProvider Provider = new DataProtectionProvider();

        //    // Decrypt the protected message specified on input.
        //    IBuffer buffUnprotected = await Provider.UnprotectAsync(buffProtected);

        //    // Execution of the SampleUnprotectData method resumes here
        //    // after the awaited task (Provider.UnprotectAsync) completes
        //    // Convert the unprotected message from an IBuffer object to a string.
        //    String strClearText = CryptographicBuffer.ConvertBinaryToString(encoding, buffUnprotected);

        //    // Return the plaintext string.
        //    return strClearText;
        //}


    }
}

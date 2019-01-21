using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
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
        private AuthenticationContext _authenticationContext;
        private readonly static string ClientID = "9058d700-ccd7-4dd4-a029-aec31995add0";
        private readonly static string Authority = "https://login.microsoftonline.com/common/";
        private readonly static IEnumerable<string> Scopes = new[] { "https://graph.windows.net/.default" };

        public MainPage()
        {
            this.InitializeComponent();

            _pca = new PublicClientApplication(ClientID, Authority);
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
            var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
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
            var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            foreach (var account in accounts)
            {
                await _pca.RemoveAsync(account).ConfigureAwait(false);
            }
        }

        private async void ADALButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            AuthenticationContext authenticationContext = new AuthenticationContext(Authority);
            var result = await authenticationContext.AcquireTokenAsync(
                "https://graph.windows.net",
                ClientID,
                new Uri("urn:ietf:wg:oauth:2.0:oob"),
                new PlatformParameters(PromptBehavior.SelectAccount, false))
                .ConfigureAwait(false);

            await DisplayMessageAsync("Signed in User - " + result.UserInfo.DisplayableId + "\nAccessToken: \n" + result.AccessToken)
                .ConfigureAwait(false);

        }

        private async void AccessTokenSilentButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);

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


    }
}

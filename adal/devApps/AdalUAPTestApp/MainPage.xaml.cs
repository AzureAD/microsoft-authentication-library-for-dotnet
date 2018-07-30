using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UAPTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string ClientId = "cd01dc27-9d3c-4812-beda-8229d5d4a8d5";
        private const string ReturnUri = "https://MyDirectorySearcherApp";

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void AccessTokenButton_Click(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");

            try
            {
                AuthenticationResult result = await ctx.AcquireTokenAsync("https://graph.windows.net",
                    ClientId, new Uri(ReturnUri),
                    new PlatformParameters(PromptBehavior.Auto, false));

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        AccessToken.Text = "Signed in User - " + result.UserInfo.DisplayableId + "\nAccessToken: \n" + result.AccessToken;
                    });
            }
            catch (Exception exc)
            {
                this.AccessToken.Text = exc.Message;
            }
        }

        private async void ClearCache(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            try
            {
                ctx.TokenCache.Clear();
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    AccessToken.Text = "Cache was cleared";
                });
            }
            catch (Exception exc)
            {
                this.AccessToken.Text = exc.Message;
            }
        }

        private async void ShowCacheCount(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    AccessToken.Text = "Token Cache count is " + ctx.TokenCache.Count;
                });
            }
            catch (Exception exc)
            {
                this.AccessToken.Text = exc.Message;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void Button_Click_2(object sender, RoutedEventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            this.AccessToken.Text = string.Empty;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void AcquireTokenClientCred_Click(object sender, RoutedEventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            this.AccessToken.Text = string.Empty;
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");

            try
            {
                AuthenticationResult result = await ctx.AcquireTokenAsync(
                    "https://graph.windows.net",
                    ClientId,
                    new UserCredential());

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        AccessToken.Text = "Signed in User - " + result.UserInfo.DisplayableId + "\nAccessToken: \n" + result.AccessToken;
                    });
            }
            catch (Exception exc)
            {
                this.AccessToken.Text = exc.Message;
            }
        }
    }
}

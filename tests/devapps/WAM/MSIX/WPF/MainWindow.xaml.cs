using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly IEnumerable<string> s_scopes = new[] { "user.read" };

        public MainWindow()
        {
            InitializeComponent();
        }

        async void GetAccount(string clientId)
        {
            IPublicClientApplication pca = PublicClientApplicationBuilder
              .Create(clientId)
              .WithAuthority("https://login.microsoftonline.com/common")
              .WithBrokerPreview(true)   // this method exists in Microsoft.Identity.Client.Broker package
              .Build();

            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(true);
            var acc = accounts.SingleOrDefault();

            IntPtr windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;

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
                        .WithParentActivityOrWindow(windowHandle)
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

        private void AtsAti_Click(object sender, RoutedEventArgs e)
        {
            GetAccount("4b0db8c2-9f26-4417-8bde-3f0e3656f8e0");
        }

        private void DisplayMessage(string message)
        {
            Dispatcher.Invoke(
                   () =>
                   {
                       Log.Text = message;
                   });
        }
    }
}

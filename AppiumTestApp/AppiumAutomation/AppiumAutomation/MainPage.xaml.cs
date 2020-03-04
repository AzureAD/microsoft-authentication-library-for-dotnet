using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Xamarin.Forms;

namespace AppiumAutomation
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private const string ClientId = "53f141d4-993d-4320-b47d-74f0b41fb751";

        public MainPage()
        {
            InitializeComponent();
        }

        private async Task AcquireTokenInteractiveAsync()
        {
            try
            {
                // TODO: reset MSAL caches!
                var pca = PublicClientApplicationBuilder
                    .Create(ClientId)
                    .WithRedirectUri("msal53f141d4-993d-4320-b47d-74f0b41fb751://auth")
                    .WithLogging((level, message, pii) =>
                    {
                        Console.WriteLine($"[MSAL LOG][{level}] {message} ");
                    })
                    .Build();

                var result = await pca.AcquireTokenInteractive(new[] { "user.read" })
                    .WithParentActivityOrWindow(App.CurrentActivityOrViewController)
                    .WithUseEmbeddedWebView(false)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                DisplayResult("Passed! " + result.TenantId);

            }
            catch (Exception e)
            {
                DisplayResult("Failed! " + e);
            }

        }

        private void DisplayResult(string text)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                TestResult.Text = text;
            });
        }

        private void PrepareTestEnvironmentAsync()
        {
            throw new NotImplementedException();
        }

        private async void RunClickedAsync(object sender, EventArgs e)
        {
            int selectedIndex = uiTestPicker.SelectedIndex;

            switch (selectedIndex)
            {
                case 0: // AT Interactive
                    //PrepareTestEnvironmentAsync().ConfigureAwait(false); 
                    await AcquireTokenInteractiveAsync().ConfigureAwait(false); 
                    break;
            }
        }
    }
}

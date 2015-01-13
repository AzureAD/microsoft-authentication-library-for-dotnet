using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

using Windows.ApplicationModel.Activation;
using Windows.Foundation.Diagnostics;
using Windows.Storage;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using Test.ADAL.Common;
using Test.ADAL.WinPhoneSL.Dashboard.Resources;

namespace Test.ADAL.WinPhoneSL.Dashboard
{
    public partial class MainPage : PhoneApplicationPage, IWebAuthenticationContinuable
    {
        private AuthenticationContext context;

        private Sts sts;

        private FileLoggingSession fls;
        private LoggingSession ls;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            this.sts = StsFactory.CreateSts(StsType.AAD);

            this.fls = new FileLoggingSession("Test File Logging Session");
            this.fls.AddLoggingChannel(AdalTrace.AdalLoggingChannel, LoggingLevel.Verbose);

            this.ls = new LoggingSession("Test Logging Session");
            this.ls.AddLoggingChannel(AdalTrace.AdalLoggingChannel, LoggingLevel.Verbose);
        }

        public async void ContinueWebAuthentication(WebAuthenticationBrokerContinuationEventArgs args)
        {
            await this.context.ContinueAcquireTokenAsync(args);
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            await this.fls.CloseAndSaveToFileAsync();
            StorageFile logfile = await this.ls.SaveToFileAsync(localFolder, "logfile");
            this.AccessToken.Text = logfile.DisplayName;
        }
        
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;

            this.context = await AuthenticationContext.CreateAsync(sts.Authority);

            var result = await this.context.AcquireTokenSilentAsync(sts.ValidResource, sts.ValidClientId, sts.ValidUserId);
            if (result.Status == AuthenticationStatus.Success)
            {
                this.DisplayToken(result);
            }
            else
            {
                this.context.AcquireTokenAndContinue(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, sts.ValidUserId, this.DisplayToken);
            }
        }

        private void DisplayToken(AuthenticationResult result)
        {
            if (!string.IsNullOrEmpty(result.AccessToken))
            {
                this.AccessToken.Text = result.AccessToken;
            }
            else
            {
                this.AccessToken.Text = result.ErrorDescription;
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.AccessToken.Text = string.Empty;
            this.context = await AuthenticationContext.CreateAsync(sts.Authority);
            this.context.TokenCache.Clear();
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}
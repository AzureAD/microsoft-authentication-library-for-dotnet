// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using MauiAppWithBroker.MSALClient;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MauiAppWithBroker.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        private const string RedirectURIWindows = "ms-appx-web://microsoft.aad.brokerplugin/858b4a09-dc31-45d3-83a7-2b5f024f99cd";

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            // configure redirect URI for your application
            PlatformConfig.Instance.RedirectUri = RedirectURIWindows;
            var app = MauiAppWithBroker.App.Current;
            PlatformConfig.Instance.ParentWindow = ((MauiWinUIWindow)app.Windows[0].Handler.PlatformView).WindowHandle;
        }
    }
}
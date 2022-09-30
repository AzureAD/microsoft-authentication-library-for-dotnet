// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Foundation;
using MauiAppWithBroker.MSALClient;
using Microsoft.Identity.Client;
using UIKit;

namespace MauiAppWithBroker
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        private const string iOSRedirectURI = "msauth.com.companyname.mauiappwithbroker://auth"; // TODO - Replace with your redirectURI

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // configure platform specific params
            PlatformConfig.Instance.RedirectUri = iOSRedirectURI;
            PlatformConfig.Instance.ParentWindow = new UIViewController(); // iOS broker requires a view controller

            return base.FinishedLaunching(application, launchOptions);
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
        {
            if (AuthenticationContinuationHelper.IsBrokerResponse(null))
            {
                // Done on different thread to allow return in no time.
                _ = Task.Factory.StartNew(() => AuthenticationContinuationHelper.SetBrokerContinuationEventArgs(url));

                return true;
            }

            else if (!AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(url))
            {
                return false;
            }

            return true;
        }
    }
}

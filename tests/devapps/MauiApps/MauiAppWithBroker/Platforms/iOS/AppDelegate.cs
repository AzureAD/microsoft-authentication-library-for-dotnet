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
        /*
         let kClientID = "bff27aee-5b7f-4588-821a-ed4ce373d8e2"
        let kRedirectUri = "msauth.com.companyname.mauiappwithbroker://auth"
        let kAuthority = "https://login.microsoftonline.com/common"
        let kGraphEndpoint = "https://graph.microsoft.com/"
         */
        private const string iOSRedirectURI = "msauth.com.companyname.mauiappwithbroker://auth"; // TODO - Replace with your redirectURI

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // configure platform specific params
            PlatformConfigImpl.Instance.RedirectUri = iOSRedirectURI;
            PlatformConfigImpl.Instance.ParentWindow = new UIViewController(); // iOS broker requires a view controller

            return base.FinishedLaunching(application, launchOptions);
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
        {
            // TBD - change the hardcoded
            if (AuthenticationContinuationHelper.IsBrokerResponse("com.microsoft.azureauthenticator"))
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

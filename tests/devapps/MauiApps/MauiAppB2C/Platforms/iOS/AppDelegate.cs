// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Foundation;
using MauiB2C.MSALClient;
using UIKit;

namespace MauiB2C;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // configure platform specific params
        PlatformConfig.Instance.RedirectUri = $"msal{B2CConstants.ClientID}://auth";
        
        return base.FinishedLaunching(application, launchOptions);
    }
}

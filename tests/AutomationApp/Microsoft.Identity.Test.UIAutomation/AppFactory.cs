// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Xamarin.UITest;

namespace Microsoft.Identity.Test.UIAutomation
{
    /// <summary>
    /// Initializes the app object that represents the main gateway to interact with the app on the device
    /// </summary>
    public static class AppFactory
    {
        public static IApp StartApp(Platform platform, string targetApp)
        {
            switch (platform)
            {
                case Platform.Android:
                    return ConfigureApp.Android.InstalledApp(targetApp).StartApp();
                case Platform.iOS:
                    return ConfigureApp.iOS.InstalledApp(targetApp).StartApp();
                default:
                    throw new PlatformNotSupportedException("Unknown platform: " + platform);
            }
        }
    }
}

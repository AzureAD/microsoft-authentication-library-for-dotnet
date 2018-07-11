using System;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Test.Microsoft.Identity.Core.UIAutomation
{
    /// <summary>
    /// Initializes the app object that represents the main gateway to interact with the app on the device
    /// </summary>
	public class AppFactory
	{
        public static IApp StartApp(Platform platform, string targetApp)
        {
            switch (platform)
            {
                case Platform.Android:
                    return ConfigureApp.Android.InstalledApp(targetApp).StartApp();
                case Platform.iOS:
                    return ConfigureApp.iOS.StartApp();
                default:
                    throw new PlatformNotSupportedException();
            }
        }
    }
}
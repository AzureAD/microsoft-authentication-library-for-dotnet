// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using MauiB2C.MSALClient;
using Microsoft.Identity.Client;

namespace MauiB2C;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // configure platform specific params
        PlatformConfig.Instance.RedirectUri = $"msal{B2CConstants.ClientID}://auth";
        PlatformConfig.Instance.ParentWindow = this;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
    }
}

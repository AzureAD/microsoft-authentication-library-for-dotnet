//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;
using Microsoft.Identity.Client;

namespace XForms.Droid
{
    [Activity(Label = "XForms", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());

            App.UIParent = new UIParent(this);

            #region Web browsers for MSAL.NET Android
            // To activate embedded webview, remove '//' from line 56 below, 
            // and comment out line 50 above -> App.UIParent = new UIParent(Xamarin.Forms.Forms.Context as Activity);

            //App.UIParent = new UIParent(this, true);

            // Use helper method to determine first if Chrome or Chrome Custom Tabs
            // are installed on the device. 
            // Remove /* */ below, and
            // make sure lines 50 and 56 above are commented out.
            // Return false, launch with embedded webview -> or developer could include a custom
            // error message here for the user
            // Return true, launch with system browser
            /*bool useEmbeddedWebview = UIParent.IsSystemWebviewAvailable();
            if (useEmbeddedWebview)
            {
                // Chrome present on device, use system browser
                App.UIParent = new UIParent(this);
            }
            else
            {
                // Chrome not present on device, use embedded webview
                App.UIParent = new UIParent(this, true);
            }*/
            #endregion
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
        }
    }
}


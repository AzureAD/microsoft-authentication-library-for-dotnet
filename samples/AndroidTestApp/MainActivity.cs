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

using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Microsoft.Identity.Client;
using TestApp.PCL;

namespace AndroidTestApp
{
    [Activity(Label = "AndroidTestApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private TextView accessTokenTextView;
        private MobileAppSts sts = new MobileAppSts();
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            Button acquireTokenInteractiveButton = FindViewById<Button>(Resource.Id.acquireTokenInteractiveButton);
            acquireTokenInteractiveButton.Click += acquireTokenInteractiveButton_Click;

            Button acquireTokenSilentButton = FindViewById<Button>(Resource.Id.acquireTokenSilentButton);
            acquireTokenSilentButton.Click += acquireTokenSilentButton_Click;

            Button clearCacheButton = FindViewById<Button>(Resource.Id.clearCacheButton);
            clearCacheButton.Click += clearCacheButton_Click;

            this.accessTokenTextView = FindViewById<TextView>(Resource.Id.accessTokenTextView);

            sts.Authority = "https://login.microsoftonline.com/common";
            sts.ValidClientId = "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc";
            sts.ValidScope = new [] { "User.Read"};
            sts.ValidUserName = "mam@msdevex.onmicrosoft.com";
            
            EditText email = FindViewById<EditText>(Resource.Id.email);
            email.Text = sts.ValidUserName;
        }
        
        private async void acquireTokenSilentButton_Click(object sender, EventArgs e)
        {
            this.accessTokenTextView.Text = string.Empty;
            TokenBroker tokenBroker = new TokenBroker();
            tokenBroker.Sts = sts;
            EditText email = FindViewById<EditText>(Resource.Id.email);
            tokenBroker.Sts.ValidUserName = email.Text;
            string value = null;
            try
            {
                value = await tokenBroker.GetTokenSilentAsync(new PlatformParameters(this)).ConfigureAwait(false);
            }
            catch (Java.Lang.Exception ex)
            {
                throw new Exception(ex.Message + "\n" + ex.StackTrace);
            }
            catch (Exception exc)
            {
                value = exc.Message;
            }

            this.accessTokenTextView.Text = value;

        }

        private async void acquireTokenInteractiveButton_Click(object sender, EventArgs e)
        {
            PublicClientApplication application = new PublicClientApplication("5a434691-ccb2-4fd1-b97b-b64bcfbc03fc");
            application.RedirectUri = "adaliosxformsapp://com.yourcompany.xformsapp";
            this.accessTokenTextView.Text = string.Empty;
            TokenBroker tokenBroker = new TokenBroker();
            tokenBroker.Sts = sts;
            EditText email = FindViewById<EditText>(Resource.Id.email);
            tokenBroker.Sts.ValidUserName = email.Text;
            string value = null;
            try
            {
                application.PlatformParameters = new PlatformParameters(this);
                var result = await application.AcquireTokenAsync(new string[] { "User.Read" });
                value = result.Token;
            }
            catch (Java.Lang.Exception ex)
            {
                throw new Exception(ex.Message + "\n" + ex.StackTrace);
            }
            catch (Exception exc)
            {
                value = exc.Message;
            }

            this.accessTokenTextView.Text = value;
        }

        private async void clearCacheButton_Click(object sender, EventArgs e)
        {
            await Task.Factory.StartNew(() =>
            {
                TokenCache.DefaultSharedUserTokenCache.Clear(sts.ValidClientId);
                this.accessTokenTextView.Text = "Cache cleared";
            }).ConfigureAwait(false);
        }
    }
}


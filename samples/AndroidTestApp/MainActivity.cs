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
using Android.Views;
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
            // creating LinearLayout
            var linLayout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters =
                           new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)

            };

            EditText email = new EditText(this)
            {
                Id = 1
            };
            linLayout.AddView(email);

            Button acquireTokenInteractiveButton = new Button(this)
            {
                Id = 2,
                Text = "Acquire Token Interactive"
            };

            acquireTokenInteractiveButton.Click += acquireTokenInteractiveButton_Click;
            linLayout.AddView(acquireTokenInteractiveButton);


            Button acquireTokenSilentButton = new Button(this)
            {
                Id = 3,
                Text = "Acquire Token Silent"
            };

            acquireTokenSilentButton.Click += acquireTokenSilentButton_Click;
            linLayout.AddView(acquireTokenSilentButton);


            Button clearCacheButton = new Button(this)
            {
                Id = 4,
                Text = "Clear Cache"
            };

            clearCacheButton.Click += clearCacheButton_Click;
            linLayout.AddView(clearCacheButton);


            this.accessTokenTextView = new TextView(this)
            {
                Id = 5
            };

            linLayout.AddView(accessTokenTextView);

            sts.Authority = "https://login.microsoftonline.com/common";
            sts.ValidClientId = "<client_id>";
            sts.ValidScope = new [] { "User.Read"};
            sts.ValidUserName = "<username>";
            email.Text = sts.ValidUserName;

            SetContentView(linLayout);
        }
        
        private async void acquireTokenSilentButton_Click(object sender, EventArgs e)
        {
            this.accessTokenTextView.Text = string.Empty;
            TokenBroker tokenBroker = new TokenBroker();
            tokenBroker.Sts = sts;
            EditText email = FindViewById<EditText>(1);
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
            PublicClientApplication application = new PublicClientApplication("<client_id>");
            application.RedirectUri = "<redirect_uri>";
            this.accessTokenTextView.Text = string.Empty;
            TokenBroker tokenBroker = new TokenBroker();
            tokenBroker.Sts = sts;
            EditText email = FindViewById<EditText>(1);
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


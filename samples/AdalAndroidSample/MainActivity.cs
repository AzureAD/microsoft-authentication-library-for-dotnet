//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

using Sample.PCL;

namespace AdalAndroidSample
{
    [Activity(Label = "AdalAndroidSample", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private TextView accessTokenTextView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            Button acquireTokenInteractiveButton = FindViewById<Button>(Resource.Id.acquireTokenInteractiveButton);
            acquireTokenInteractiveButton.Click += acquireTokenInteractiveButton_Click;

            Button acquireTokenUPButton = FindViewById<Button>(Resource.Id.acquireTokenUPButton);
            acquireTokenUPButton.Click += acquireTokenUPButton_Click;

            this.accessTokenTextView = FindViewById<TextView>(Resource.Id.accessTokenTextView);
        }

        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            WebAuthenticationBrokerContinuationHelper.SetWebAuthenticationBrokerContinuationEventArgs(requestCode, resultCode, data);

            base.OnActivityResult(requestCode, resultCode, data);
        }

        private async void acquireTokenUPButton_Click(object sender, EventArgs e)
        {
            this.accessTokenTextView.Text = string.Empty;
            TokenBroker tokenBroker = new TokenBroker();
            string token = await tokenBroker.GetTokenWithUsernamePasswordAsync();
            this.accessTokenTextView.Text = token;
        }

        private async void acquireTokenInteractiveButton_Click(object sender, EventArgs e)
        {
            this.accessTokenTextView.Text = string.Empty;
            TokenBroker tokenBroker = new TokenBroker();
            string token = await tokenBroker.GetTokenInteractiveAsync(new AuthorizationParameters(this));
            this.accessTokenTextView.Text = token;
        }
    }
}


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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client;
using Xamarin.Forms;

namespace XForms
{
    public partial class App : Application
    {
        public static PublicClientApplication MsalPublicClient;
        public static UIParent UIParent { get; set; }
        public const string DefaultClientId = "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc";
        public const string B2cClientId = "750a8822-a6d4-4127-bc0b-efbfacccbc28";

        public const string RedirectUriOnAndroid =
            "msauth-5a434691-ccb2-4fd1-b97b-b64bcfbc03fc://com.microsoft.identity.client.sample";
        public const string RedirectUriOnIos = "adaliosxformsapp://com.yourcompany.xformsapp";

        public const string DefaultAuthority = "https://login.microsoftonline.com/common";
        public const string B2cAuthority = "https://login.microsoftonline.com/tfp/panwariusb2c.onmicrosoft.com/B2C_1_signup_signin/";

        public static string[] DefaultScopes = {"User.Read"};
        public static string[] B2cScopes = { "https://panwariusb2c.onmicrosoft.com/fail/wtf" };

        public const bool DefaultValidateAuthority = true;

        public static string Authority = DefaultAuthority;
        public static bool ValidateAuthority = DefaultValidateAuthority;

        public static string ClientId = DefaultClientId;

        public static string[] Scopes = DefaultScopes;

        public App()
        {
            MainPage = new NavigationPage(new XForms.MainPage());

            InitPublicClient();

            Logger.LogCallback = delegate(Logger.LogLevel level, string message, bool containsPii)
            {
                Device.BeginInvokeOnMainThread(() => { LogPage.AddToLog("[" + level + "]" + " - " + message, containsPii); });
            };
            Logger.Level = Logger.LogLevel.Verbose;
            Logger.PiiLoggingEnabled = true;
        }

        public static void InitPublicClient()
        {
            MsalPublicClient = new PublicClientApplication(ClientId, Authority);
            switch (Device.RuntimePlatform)
            {
                case "iOS":
                    MsalPublicClient.RedirectUri = RedirectUriOnIos;
                    break;
                case "Android":
                    MsalPublicClient.RedirectUri = RedirectUriOnAndroid;
                    break;
            }
            
            MsalPublicClient.ValidateAuthority = ValidateAuthority;
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}

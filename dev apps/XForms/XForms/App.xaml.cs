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
        public const string ClientId = "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc";

        public const string RedirectUriOnAndroid =
            "msauth-5a434691-ccb2-4fd1-b97b-b64bcfbc03fc://com.microsoft.identity.client.sample";

        public const string RedirectUriOnIos = "adaliosxformsapp://com.yourcompany.xformsapp";
        public const string DefaultAuthority = "https://login.microsoftonline.com/common";
        public static string[] Scopes = {"User.Read"};
        public const bool DefaultValidateAuthority = true;

        public static string Authority = DefaultAuthority;
        public static bool ValidateAuthority = DefaultValidateAuthority;

        public App()
        {
            MainPage = new XForms.MainPage();
            InitPublicClient();

            Logger.LogCallback = delegate(Logger.LogLevel level, string message, bool containsPii)
            {
                Device.BeginInvokeOnMainThread(() => { LogPage.AddToLog("[" + level + "]" + " - " + message); });
            };

            Logger.Level = Logger.LogLevel.Verbose;
        }

        public static void InitPublicClient()
        {
            MsalPublicClient = new PublicClientApplication(ClientId, Authority);
            Device.OnPlatform(Android: () => { MsalPublicClient.RedirectUri = RedirectUriOnAndroid; });

            Device.OnPlatform(iOS: () => { MsalPublicClient.RedirectUri = RedirectUriOnIos; });

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

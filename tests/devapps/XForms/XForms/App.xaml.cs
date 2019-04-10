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
using Microsoft.Identity.Client;
using Xamarin.Forms;

namespace XForms
{
    public partial class App : Application
    {
        public static PublicClientApplication MsalPublicClient { get; set; }

        public static object AndroidActivity { get; set; }

        public const string DefaultClientId = "4b0db8c2-9f26-4417-8bde-3f0e3656f8e0";
        // For system browser
        //public const string DefaultClientId = "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc";

        public const string B2cClientId = "e3b9ad76-9763-4827-b088-80c7a7888f79";


        public static string RedirectUriOnAndroid = Microsoft.Identity.Client.Core.Constants.DefaultRedirectUri; // will not work with system browser
        // For system browser
        //public static string RedirectUriOnAndroid = "msauth-5a434691-ccb2-4fd1-b97b-b64bcfbc03fc://com.microsoft.identity.client.sample";
        
        public const string BrokerRedirectUriOnIos = "msauth.com.yourcompany.XForms://auth";

        public static string RedirectUriOnIos =  Microsoft.Identity.Client.Core.Constants.DefaultRedirectUri;
        // For system browser
        //public static string RedirectUriOnIos = "adaliosxformsapp://com.yourcompany.xformsapp";

        public const string RedirectUriB2C = "msale3b9ad76-9763-4827-b088-80c7a7888f79://auth";

        public const string DefaultAuthority = "https://login.microsoftonline.com/common";

        public const string B2CMicrosoftLoginAuthorityHost = "login.microsoftonline.com";
        public const string B2CAuthorityHost = "msidlabb2c.b2clogin.com";
        public const string B2CTenantId = "msidlabb2c.onmicrosoft.com";
        public const string B2CSiSuPolicy = "B2C_1_SISOPolicy";
        public const string B2CEditProfilePolicy = "B2C_1_ProfileEditPolicy";

        public static string[] DefaultScopes = { "User.Read" };
        public static string[] B2cScopes = { "https://msidlabb2c.onmicrosoft.com/msidlabb2capi/read" };

        public const bool DefaultValidateAuthority = true;

        public static string Authority = DefaultAuthority;
        public static bool ValidateAuthority = DefaultValidateAuthority;

        public static string ClientId = DefaultClientId;

        public static string[] Scopes = DefaultScopes;
        public static string B2CPolicy;
        public static bool UseBroker;
        public static bool UseB2CAuthorityHost = false;

        public App()
        {
            MainPage = new NavigationPage(new XForms.MainPage());

            InitPublicClient(null, ClientId);
        }

        public static void InitPublicClient(string b2cAuthorityHost, string clientId)
        {
            var builder = PublicClientApplicationBuilder
                .Create(clientId)
                .WithLogging((level, message, pii) =>
                {
                    Device.BeginInvokeOnMainThread(() => { LogPage.AddToLog("[" + level + "]" + " - " + message, pii); });
                },
                LogLevel.Verbose,
                true);

            if (UseBroker)
            {
                //builder.WithBroker(true);
                builder.WithIosKeychainSecurityGroup("com.microsoft.adalcache");
                builder.WithRedirectUri(BrokerRedirectUriOnIos);
            }

            else
            {
                 // Let Android set its own redirect uri
                switch (Device.RuntimePlatform)
                {
                    case "iOS":
                        builder.WithRedirectUri(RedirectUriOnIos);
                        break;
                    case "Android":
                        builder.WithRedirectUri(RedirectUriOnAndroid);
                        break;
                }

#if IS_APPCENTER_BUILD
            builder.WithIosKeychainSecurityGroup("*");
#endif
            }
            if (UseB2CAuthorityHost)
            {
                builder.WithB2CHost(b2cAuthorityHost, B2CTenantId);
                builder.WithRedirectUri(RedirectUriB2C);
            }
            else
            {
                builder.WithAuthority(new Uri(Authority), ValidateAuthority);
            }

            MsalPublicClient = builder.BuildConcrete();
        }

        public static void InitB2CPublicClient(string b2cAuthorityHost)
        {
            var builder = PublicClientApplicationBuilder
                .Create(B2cClientId)
                .WithB2CHost(b2cAuthorityHost, B2CTenantId)
                .WithLogging((level, message, pii) =>
                {
                    Device.BeginInvokeOnMainThread(() => { LogPage.AddToLog("[" + level + "]" + " - " + message, pii); });
                },
                LogLevel.Verbose,
                true)
                .WithRedirectUri(RedirectUriB2C);

            if (UseBroker)
            {
                //builder.WithBroker(true);
                builder.WithIosKeychainSecurityGroup("com.microsoft.adalcache");
                builder.WithRedirectUri(BrokerRedirectUriOnIos);
            }
            else
            { 

#if IS_APPCENTER_BUILD
            builder.WithIosKeychainSecurityGroup("*");
#endif
            }

            MsalPublicClient = builder.BuildConcrete();
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

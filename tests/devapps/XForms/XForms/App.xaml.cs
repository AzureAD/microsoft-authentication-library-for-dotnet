// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Xamarin.Forms;

namespace XForms
{
    public partial class App : Application
    {
        public static PublicClientApplication MsalPublicClient;

        public static object RootViewController { get; set; }

        public const string DefaultClientId = "4b0db8c2-9f26-4417-8bde-3f0e3656f8e0";

        public const string B2cClientId = "e3b9ad76-9763-4827-b088-80c7a7888f79";

        public static string s_redirectUriOnAndroid = Microsoft.Identity.Client.Core.Constants.DefaultRedirectUri;

        public static string s_redirectUriOnIos = Microsoft.Identity.Client.Core.Constants.DefaultRedirectUri;

        public const string RedirectUriB2C = "msale3b9ad76-9763-4827-b088-80c7a7888f79://auth";

        public const string DefaultAuthority = "https://login.microsoftonline.com/common";
        public const string B2cAuthority = "https://login.microsoftonline.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_SISOPolicy/";
        public const string B2CLoginAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_SISOPolicy/";
        public const string B2CEditProfilePolicyAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_ProfileEditPolicy/";
        public const string B2CROPCAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_ROPC_Auth";

        public static string[] s_defaultScopes = { "User.Read" };
        public static string[] s_b2cScopes = { "https://msidlabb2c.onmicrosoft.com/msidlabb2capi/read" };

        public const bool DefaultValidateAuthority = true;

        public static string s_authority = DefaultAuthority;
        public static bool s_validateAuthority = DefaultValidateAuthority;

        public static string s_clientId = DefaultClientId;

        public static string[] s_scopes = s_defaultScopes;

        public App()
        {
            MainPage = new NavigationPage(new XForms.MainPage());
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

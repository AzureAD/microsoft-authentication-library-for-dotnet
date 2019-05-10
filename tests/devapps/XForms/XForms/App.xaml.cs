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

        public static object AndroidActivity { get; set; }

        public const string DefaultClientId = "4b0db8c2-9f26-4417-8bde-3f0e3656f8e0";
        // For system browser
        //public const string DefaultClientId = "5a434691-ccb2-4fd1-b97b-b64bcfbc03fc";

        public const string B2cClientId = "e3b9ad76-9763-4827-b088-80c7a7888f79";


        public static string RedirectUriOnAndroid = Microsoft.Identity.Client.Core.Constants.DefaultRedirectUri; // will not work with system browser
        // For system browser
        //public static string RedirectUriOnAndroid = "msauth-5a434691-ccb2-4fd1-b97b-b64bcfbc03fc://com.microsoft.identity.client.sample";

        public const string BrokerRedirectUriOnIos = "msauth.com.yourcompany.XForms://auth";

        public static string RedirectUriOnIos = Microsoft.Identity.Client.Core.Constants.DefaultRedirectUri;
        // For system browser
        //public static string RedirectUriOnIos = "adaliosxformsapp://com.yourcompany.xformsapp";

        public const string RedirectUriB2C = "msale3b9ad76-9763-4827-b088-80c7a7888f79://auth";

        public const string DefaultAuthority = "https://login.microsoftonline.com/common";
        public const string B2cAuthority = "https://login.microsoftonline.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_SISOPolicy/";
        public const string B2CLoginAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_SISOPolicy/";
        public const string B2CEditProfilePolicyAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_ProfileEditPolicy/";
        public const string B2CROPCAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_ROPC_Auth";

        public static string[] DefaultScopes = { "User.Read" };
        public static string[] B2cScopes = { "https://msidlabb2c.onmicrosoft.com/msidlabb2capi/read" };

        public const bool DefaultValidateAuthority = true;

        public static string Authority = DefaultAuthority;
        public static bool ValidateAuthority = DefaultValidateAuthority;

        public static string ClientId = DefaultClientId;

        public static string[] Scopes = DefaultScopes;
        public static bool UseBroker;

        public App()
        {
            MainPage = new NavigationPage(new XForms.MainPage());

            InitPublicClient();
        }

        public static void InitPublicClient()
        {
            var builder = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(new Uri(Authority), ValidateAuthority)
                .WithLogging((level, message, pii) =>
                {
                    Device.BeginInvokeOnMainThread(() => { LogPage.AddToLog("[" + level + "]" + " - " + message, pii); });
                },
                LogLevel.Verbose,
                true);

            if (UseBroker)
            {
                //builder.WithBroker(true);
                builder = builder.WithIosKeychainSecurityGroup("com.microsoft.adalcache");
                builder = builder.WithRedirectUri(BrokerRedirectUriOnIos);
            }

            else
            {
                // Let Android set its own redirect uri
                switch (Device.RuntimePlatform)
                {
                case "iOS":
                    builder = builder.WithRedirectUri(RedirectUriOnIos);
                    builder = builder.WithIosKeychainSecurityGroup("com.microsoft.adalcache");
                    break;
                case "Android":
                    builder = builder.WithRedirectUri(RedirectUriOnAndroid);
                    break;
                }

#if IS_APPCENTER_BUILD
            builder = builder.WithIosKeychainSecurityGroup("*");
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Unit;
using Xamarin.Forms;

namespace XamarinDev
{
    public partial class App : Application
    {
        public static PublicClientApplication MsalPublicClient;

        public static object RootViewController { get; set; }
#if !IS_APPCENTER_BUILD
        public const string DefaultClientId = "4a1aa1d5-c567-49d0-ad0b-cd957a47f842"; // in msidentity-samples-testing tenant -> PublicClientSample
#else
        public const string DefaultClientId = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8"; // UI Automation Test App ID
#endif
        public const string B2cClientId = "e3b9ad76-9763-4827-b088-80c7a7888f79";

        public static string BrokerRedirectUri
        {
            get
            {
                switch (Device.RuntimePlatform)
                {
                    case Device.iOS:
                        return "msauth.com.companyname.XamarinDev://auth";
                    case Device.Android:
#if !IS_APPCENTER_BUILD
                        //This value needs to be calculated from the certificate used to sign the package. See https://aka.ms/MSAL_NET_Broker_Redirect_URI
                        return "Broker Redirect URI";
#else
                        return "msauth://com.companyname.xamarindev/Fy2zjTiLYs5sXM3sGy+PGcv8MaQ=";
#endif
                    default:
                        throw new InvalidOperationException("Broker only supported on ios and android");
                }
            }
        }
        public static string DefaultMobileRedirectUri = TestConstants.MobileDefaultRedirectUri;

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
            MainPage = new NavigationPage(new XamarinDev.MainPage());

            InitPublicClient();
        }

        public static void InitPublicClient()
        {
            var builder = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(new Uri(Authority), ValidateAuthority)
                .WithLogging((level, message, pii) =>
                {
                    //Device.BeginInvokeOnMainThread(() => { LogPage.AddToLog("[" + level + "]" + " - " + message, pii); });
                    Console.WriteLine("[" + level + "]" + " - " + message + " - " + pii);
                },
                LogLevel.Verbose,
                true);

            if (UseBroker)
            {
                builder.WithBroker();
                builder = builder.WithRedirectUri(BrokerRedirectUri);
            }
            else
            {
                builder.WithRedirectUri(DefaultMobileRedirectUri);

                if (Device.RuntimePlatform == Device.iOS)
                {
                    builder = builder.WithIosKeychainSecurityGroup("com.microsoft.adalcache");
                }
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

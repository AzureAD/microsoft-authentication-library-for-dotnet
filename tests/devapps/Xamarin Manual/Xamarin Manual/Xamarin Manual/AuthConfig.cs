using System;
using Xamarin.Forms;

namespace Xamarin_Manual
{
    public static class AuthConfig
    {
        public const string ClientID = "4a1aa1d5-c567-49d0-ad0b-cd957a47f842"; //msidentity-samples-testing tenant
        public static readonly string[] Scopes = { "User.Read" };


        public const string AndroidApkSignature = "t+Bk/nrTiK6yhmUDgd80TS5ZZT8=";
        public const string AndroidPackgeName = "com.companyname.xamarindev";

        public static string RedirectUri
        {
            get
            {
                switch (Device.RuntimePlatform)
                {
                    case Device.iOS:
                        return "msauth.com.companyname.XamarinDev://auth";
                    case Device.Android:
                        return $"msauth://{AndroidPackgeName}/{AndroidApkSignature}";
                    default:
                        throw new InvalidOperationException("Broker only supported on ios and android");
                }
            }
        }
    }
}

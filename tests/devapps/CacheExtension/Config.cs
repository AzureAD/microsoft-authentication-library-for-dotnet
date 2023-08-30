// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Extensions.Msal;

namespace ManualTestApp
{
    internal static class Config
    {
        // App settings
        public static readonly string[] Scopes = new[] { "user.read" };

        // Use "common" if you want to allow any "enterprise" (work or school) account AND any user account (live.com, outlook, hotmail) to log in.
        // Use an actual tenant ID to allow only your enterprise to log in.
        // Use "organizations" to allow only enterprise log-in, this is required for the Username / Password flow
        public const string Authority = "https://login.microsoftonline.com/organizations";

        // DO NOT USE THIS CLIENT ID IN YOUR APP. WE REGULARLY DELETE THEM. CREATE YOUR OWN!
        public const string ClientId = "1d18b3b0-251b-4714-a02a-9956cec86c2d"; 

        // Cache settings
        public const string CacheFileName = "myapp_msal_cache.txt";
        public readonly static string CacheDir = MsalCacheHelper.UserRootDirectory;

        public const string KeyChainServiceName = "myapp_msal_service";
        public const string KeyChainAccountName = "myapp_msal_account";

        public const string LinuxKeyRingSchema = "com.contoso.devtools.tokencache";
        public const string LinuxKeyRingCollection = MsalCacheHelper.LinuxKeyRingDefaultCollection;
        public const string LinuxKeyRingLabel = "MSAL token cache for all Contoso dev tool apps.";
        public static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", "1");
        public static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "MyApps");

        // For Username / Password flow - to be used only for testing!
        public const string Username = "liu.kang@bogavrilltd.onmicrosoft.com";
        public const string Password = "Foya2128";

    }
}

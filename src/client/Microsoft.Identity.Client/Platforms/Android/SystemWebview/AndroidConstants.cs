// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Platforms.Android.SystemWebview
{
    internal static class AndroidConstants
    {
        public const string RequestUrlKey = "com.microsoft.identity.request.url.key";
        public const string RequestId = "com.microsoft.identity.request.id";
        public const string CustomTabRedirect = "com.microsoft.identity.customtab.redirect";
        public const string AuthorizationFinalUrl = "com.microsoft.identity.client.finalUrl";
        public const int Cancel = 2001;
        public const int AuthCodeError = 2002;
        public const int AuthCodeReceived = 2003;
        public const int AuthCodeReceivedFromEmbeddedWebview = -1;
    }
}

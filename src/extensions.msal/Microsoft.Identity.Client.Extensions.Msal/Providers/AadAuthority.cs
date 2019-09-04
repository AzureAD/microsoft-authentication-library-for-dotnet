// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    internal static class AadAuthority
    {
        public const string DefaultTrustedHost = "login.microsoftonline.com";
        public const string AadCanonicalAuthorityTemplate = "https://{0}/{1}/";

        internal static string CreateFromAadCanonicalAuthorityTemplate(string authority, string tenantId)
        {
            return string.Format(CultureInfo.InvariantCulture, AadCanonicalAuthorityTemplate, authority, tenantId);
        }
    }
}

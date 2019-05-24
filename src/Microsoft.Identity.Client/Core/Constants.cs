// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;

namespace Microsoft.Identity.Client.Core
{
    internal static class Constants
    {
        public const string MsAppScheme = "ms-app";
        public const int ExpirationMarginInMinutes = 5;
        public const int CodeVerifierLength = 128;
        public const int CodeVerifierByteSize = 32;

        public const string UapWEBRedirectUri = "https://sso"; // for WEB
        public const string DefaultRedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        public const string DefaultConfidentialClientRedirectUri = "https://replyUrlNotSet";

        public const string DefaultRealm = "http://schemas.microsoft.com/rel/trusted-realm";

        public static string FormatEnterpriseRegistrationOnPremiseUri(string domain)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "https://enterpriseregistration.{0}/enrollmentserver/contract",
                domain);
        }

        public static string FormatEnterpriseRegistrationInternetUri(string domain)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "https://enterpriseregistration.windows.net/{0}/enrollmentserver/contract",
                domain);
        }

        public const string WellKnownOpenIdConfigurationPath = ".well-known/openid-configuration";
        public const string OpenIdConfigurationEndpoint = "v2.0/" + WellKnownOpenIdConfigurationPath;

        public static string FormatAdfsWebFingerUrl(string host, string resource)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "https://{0}/.well-known/webfinger?rel={1}&resource={2}",
                host,
                Constants.DefaultRealm,
                resource);
        }
    }
}

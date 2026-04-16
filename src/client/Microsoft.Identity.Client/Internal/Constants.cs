// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Internal
{
    internal static class Constants
    {
        public const string MsAppScheme = "ms-app";
        public const int ExpirationMarginInMinutes = 5;
        public const int CodeVerifierLength = 128;
        public const int CodeVerifierByteSize = 96;

        public const string DefaultRedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        public const string NativeClientRedirectUri = "https://login.microsoftonline.com/common/oauth2/nativeclient";
        public const string LocalHostRedirectUri = "http://localhost";
        public const string DefaultConfidentialClientRedirectUri = "https://replyUrlNotSet";

        public const string DefaultRealm = "http://schemas.microsoft.com/rel/trusted-realm";

        public const string MsaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";
        public const string Consumers = "consumers";
        public const string Organizations = "organizations";
        public const string Common = "common";

        public const string UserRealmMsaDomainName = "live.com";

        public const string CcsRoutingHintHeader = "x-anchormailbox";
        public const string AadThrottledErrorCode = "AADSTS50196";
        public const string AadAccountTypeAndResourceIncompatibleErrorCode = "AADSTS500207";
        public const string AadMissingScopeErrorCode = "AADSTS900144";

        //Represents 5 minutes in Unit time stamp
        public const int DefaultJitterRangeInSeconds = 300;
        public static readonly TimeSpan AccessTokenExpirationBuffer = TimeSpan.FromMinutes(5);
        public const string EnableSpaAuthCode = "1";
        public const string BearerTokenType = "bearer";
        public const string PoPTokenType = "pop";
        public const string MtlsPoPTokenType = "mtls_pop";
        public const string PoPAuthHeaderPrefix = "PoP";
        public const string MtlsPoPAuthHeaderPrefix = "mtls_pop";
        public const string RequestConfirmation = "req_cnf";
        public const string BearerAuthHeaderPrefix = "Bearer";
        public const string SshCertAuthHeaderPrefix = "SshCert";

        public const string ManagedIdentityClientId = "client_id";
        public const string ManagedIdentityClientId2017 = "clientid";
        public const string ManagedIdentityObjectId = "object_id";
        public const string ManagedIdentityResourceId = "mi_res_id";
        public const string ManagedIdentityResourceIdImds = "msi_res_id";
        public const string ManagedIdentityDefaultClientId = "system_assigned_managed_identity";
        public const string ManagedIdentityDefaultTenant = "managed_identity";
        public const string CiamAuthorityHostSuffix = ".ciamlogin.com";

        /// <summary>
        /// Well-known Microsoft authority hosts trusted for issuer validation.
        /// Aligned with Python MSAL's WELL_KNOWN_AUTHORITY_HOSTS.
        /// </summary>
        public static readonly HashSet<string> WellKnownAuthorityHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "login.microsoftonline.com",
            "login.microsoft.com",
            "login.windows.net",
            "sts.windows.net",
            "login.chinacloudapi.cn",
            "login.partner.microsoftonline.cn",
            "login.microsoftonline.de",
            "login-us.microsoftonline.com",
            "login.microsoftonline.us",
            "login.usgovcloudapi.net",
            "login.sovcloud-identity.fr",
            "login.sovcloud-identity.de",
            "login.sovcloud-identity.sg",
        };

        /// <summary>
        /// Well-known B2C host suffixes (without leading dot) for issuer validation.
        /// Aligned with Python MSAL's WELL_KNOWN_B2C_HOSTS.
        /// </summary>
        public static readonly string[] WellKnownB2CHostSuffixes = new[]
        {
            "b2clogin.com",
            "b2clogin.cn",
            "b2clogin.us",
            "b2clogin.de",
            "ciamlogin.com",
        };

        public const string CertSerialNumber = "cert_sn";
        public const string FmiNodeClientId = "urn:microsoft:identity:fmi";

        // Telemetry query parameter keys
        public const string CallerSdkIdKey = "caller-sdk-id";
        public const string CallerSdkVersionKey = "caller-sdk-ver";
        public const string ManagedCertKey = "ManagedCert";

        public const int CallerSdkIdMaxLength = 10;
        public const int CallerSdkVersionMaxLength = 20;

        public static string FormatEnterpriseRegistrationOnPremiseUri(string domain)
        {
            return $"https://enterpriseregistration.{domain}/enrollmentserver/contract";
        }

        public static string FormatEnterpriseRegistrationInternetUri(string domain)
        {
            return $"https://enterpriseregistration.windows.net/{domain}/enrollmentserver/contract";
        }

        public const string WellKnownOpenIdConfigurationPath = ".well-known/openid-configuration";
        public const string OpenIdConfigurationEndpoint = "v2.0/" + WellKnownOpenIdConfigurationPath;
        public const string Tenant = "{tenant}";
        public const string TenantId = "{tenantid}";
        public static string FormatAdfsWebFingerUrl(string host, string resource)
        {
            return $"https://{host}/.well-known/webfinger?rel={DefaultRealm}&resource={resource}";
        }

        public const int RsaKeySize = 2048;
    }
}

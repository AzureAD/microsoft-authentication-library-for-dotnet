// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Identity.Client.AppConfig
{
    internal class AuthorityInfo
    {
        public AuthorityInfo(
            AuthorityType authorityType,
            string authority,
            bool validateAuthority)
        {
            AuthorityType = authorityType;
            ValidateAuthority = validateAuthority;

            Host = new UriBuilder(authority).Host;

            // TODO: can we simplify this and/or move validation/configuration logic to AbstractApplicationBuilder
            // so that all authority mangling/management is in one place?

            UserRealmUriPrefix = string.Format(CultureInfo.InvariantCulture, "https://{0}/common/userrealm/", Host);

            if (AuthorityType == AuthorityType.B2C)
            {
                var authorityUri = new Uri(authority);
                string[] pathSegments = authorityUri.AbsolutePath.Substring(1).Split(
                    new[]
                    {
                        '/'
                    },
                    StringSplitOptions.RemoveEmptyEntries);
                if (pathSegments.Length < 3)
                {
                    throw new ArgumentException(CoreErrorMessages.B2cAuthorityUriInvalidPath);
                }

                CanonicalAuthority = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://{0}/{1}/{2}/{3}/",
                    authorityUri.Authority,
                    pathSegments[0],
                    pathSegments[1],
                    pathSegments[2]);
            }
            else
            {
                var authorityUri = new UriBuilder(authority);
                CanonicalAuthority = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://{0}/{1}/",
                    authorityUri.Uri.Authority,
                    GetFirstPathSegment(authority));
            }
        }

        public string Host { get; }
        public string CanonicalAuthority { get; set; }
        public AuthorityType AuthorityType { get; }
        public string UserRealmUriPrefix { get; }
        public bool ValidateAuthority { get; }

        internal static AuthorityInfo FromAuthorityUri(string authorityUri, bool validateAuthority)
        {
            string canonicalUri = CanonicalizeAuthorityUri(authorityUri);
            ValidateAuthorityUri(canonicalUri);

            var authorityType = Instance.Authority.GetAuthorityType(canonicalUri);

            // If the authority type is B2C, validateAuthority must be false.
            if (authorityType == AuthorityType.B2C)
            {
                validateAuthority = false;
            }

            return new AuthorityInfo(authorityType, canonicalUri, validateAuthority);
        }

        internal static AuthorityInfo FromAadAuthority(Uri cloudInstanceUri, Guid tenantId, bool validateAuthority)
        {
#pragma warning disable CA1305 // Specify IFormatProvider
            return FromAuthorityUri(string.Format(CultureInfo.InvariantCulture, "{0}/{1}/", cloudInstanceUri, tenantId.ToString("D")), validateAuthority);
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        internal static AuthorityInfo FromAadAuthority(Uri cloudInstanceUri, string tenant, bool validateAuthority)
        {
            if (Guid.TryParse(tenant, out Guid tenantId))
            {
                return FromAadAuthority(cloudInstanceUri, tenantId, validateAuthority);
            }
            return FromAuthorityUri(string.Format(CultureInfo.InvariantCulture, "{0}/{1}/", cloudInstanceUri, tenant), validateAuthority);
        }

        internal static AuthorityInfo FromAadAuthority(
            AzureCloudInstance azureCloudInstance,
            Guid tenantId,
            bool validateAuthority)
        {
#pragma warning disable CA1305 // Specify IFormatProvider
            string authorityUri = GetAuthorityUri(azureCloudInstance, AadAuthorityAudience.AzureAdMyOrg, tenantId.ToString("D"));
#pragma warning restore CA1305 // Specify IFormatProvider
            return new AuthorityInfo(AuthorityType.Aad, authorityUri, validateAuthority);
        }

        internal static AuthorityInfo FromAadAuthority(
            AzureCloudInstance azureCloudInstance,
            string tenant,
            bool validateAuthority)
        {
            if (Guid.TryParse(tenant, out Guid tenantIdGuid))
            {
                return FromAadAuthority(azureCloudInstance, tenantIdGuid, validateAuthority);
            }

            string authorityUri = GetAuthorityUri(azureCloudInstance, AadAuthorityAudience.AzureAdMyOrg, tenant);
            return new AuthorityInfo(AuthorityType.Aad, authorityUri, validateAuthority);
        }

        internal static AuthorityInfo FromAadAuthority(
            AzureCloudInstance azureCloudInstance,
            AadAuthorityAudience authorityAudience,
            bool validateAuthority)
        {
            string authorityUri = GetAuthorityUri(azureCloudInstance, authorityAudience);
            return new AuthorityInfo(AuthorityType.Aad, authorityUri, validateAuthority);
        }

        internal static AuthorityInfo FromAadAuthority(AadAuthorityAudience authorityAudience, bool validateAuthority)
        {
            string authorityUri = GetAuthorityUri(AzureCloudInstance.AzurePublic, authorityAudience);
            return new AuthorityInfo(AuthorityType.Aad, authorityUri, validateAuthority);
        }

        internal static AuthorityInfo FromAadAuthority(string authorityUri, bool validateAuthority)
        {
            return new AuthorityInfo(AuthorityType.Aad, authorityUri, validateAuthority);
        }

        internal static AuthorityInfo FromAdfsAuthority(string authorityUri, bool validateAuthority)
        {
            return new AuthorityInfo(AuthorityType.Adfs, authorityUri, validateAuthority);
        }

        internal static AuthorityInfo FromB2CAuthority(string authorityUri)
        {
            return new AuthorityInfo(AuthorityType.B2C, authorityUri, false);
        }

        internal static string GetCloudUrl(AzureCloudInstance azureCloudInstance)
        {
            switch (azureCloudInstance)
            {
            case AzureCloudInstance.AzurePublic:
                return "https://login.microsoftonline.com";
            case AzureCloudInstance.AzureChina:
                return "https://login.chinacloudapi.cn";
            case AzureCloudInstance.AzureGermany:
                return "https://login.microsoftonline.de";
            case AzureCloudInstance.AzureUsGovernment:
                return "https://login.microsoftonline.us";
            default:
                throw new ArgumentException(nameof(azureCloudInstance));
            }
        }

        internal static string GetAuthorityUri(
            AzureCloudInstance azureCloudInstance,
            AadAuthorityAudience authorityAudience,
            string tenantId = null)
        {
            string cloudUrl = GetCloudUrl(azureCloudInstance);
            string tenantValue = GetAadAuthorityAudienceValue(authorityAudience, tenantId);

            return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", cloudUrl, tenantValue);
        }

        internal static string GetAadAuthorityAudienceValue(AadAuthorityAudience authorityAudience, string tenantId)
        {
            switch (authorityAudience)
            {
            case AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount:
                return "common";
            case AadAuthorityAudience.AzureAdMultipleOrgs:
                return "organizations";
            case AadAuthorityAudience.PersonalMicrosoftAccount:
                return "consumers";
            case AadAuthorityAudience.AzureAdMyOrg:
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    throw new InvalidOperationException(CoreErrorMessages.AzureAdMyOrgRequiresSpecifyingATenant);
                }

                return tenantId;
            default:
                throw new ArgumentException(nameof(authorityAudience));
            }
        }


        // TODO: consolidate this with the same method in Authority.cs
        private static string GetFirstPathSegment(string authority)
        {
            var uri = new Uri(authority);
            if (uri.Segments.Length >= 2)
            {
                return new Uri(authority).Segments[1]
                                         .TrimEnd('/');
            }

            throw new InvalidOperationException(CoreErrorMessages.AuthorityDoesNotHaveTwoSegments);
        }

        internal static string CanonicalizeAuthorityUri(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri) && !uri.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri + "/";
            }

            return uri.ToLowerInvariant();
        }

        private static void ValidateAuthorityUri(string authority)
        {
            if (string.IsNullOrWhiteSpace(authority))
            {
                throw new ArgumentNullException(nameof(authority));
            }

            if (!Uri.IsWellFormedUriString(authority, UriKind.Absolute))
            {
                throw new ArgumentException(CoreErrorMessages.AuthorityInvalidUriFormat, nameof(authority));
            }

            var authorityUri = new Uri(authority);
            if (authorityUri.Scheme != "https")
            {
                throw new ArgumentException(CoreErrorMessages.AuthorityUriInsecure, nameof(authority));
            }

            string path = authorityUri.AbsolutePath.Substring(1);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(CoreErrorMessages.AuthorityUriInvalidPath, nameof(authority));
            }

            string[] pathSegments = authorityUri.AbsolutePath.Substring(1).Split('/');
            if (pathSegments == null || pathSegments.Length == 0)
            {
                throw new ArgumentException(CoreErrorMessages.AuthorityUriInvalidPath);
            }
        }
    }
}
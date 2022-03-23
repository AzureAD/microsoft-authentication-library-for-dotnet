// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <remarks>
    /// This class must be kept immutable
    /// </remarks>
    internal class AuthorityInfo
    {
        public AuthorityInfo(
            AuthorityType authorityType,
            string authority,
            bool validateAuthority)
        {
            AuthorityType = authorityType;
            ValidateAuthority = validateAuthority;

            var authorityBuilder = new UriBuilder(authority);
            Host = authorityBuilder.Host;

            // TODO: can we simplify this and/or move validation/configuration logic to AbstractApplicationBuilder
            // so that all authority mangling/management is in one place?

            UserRealmUriPrefix = UriBuilderExtensions.GetHttpsUriWithOptionalPort(string.Format(CultureInfo.InvariantCulture, "https://{0}/common/userrealm/", Host), authorityBuilder.Port);

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
                    throw new ArgumentException(MsalErrorMessage.B2cAuthorityUriInvalidPath);
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
                CanonicalAuthority = UriBuilderExtensions.GetHttpsUriWithOptionalPort(string.Format(
                    CultureInfo.InvariantCulture,
                    "https://{0}/{1}/",
                    authorityBuilder.Uri.Authority,
                    GetFirstPathSegment(authority)), authorityBuilder.Port);
            }
        }

        private AuthorityInfo(
            string host,
            string canonicalAuthority,
            AuthorityType authorityType,
            string userRealmUriPrefix,
            bool validateAuthority)
        {
            Host = host;
            CanonicalAuthority = canonicalAuthority;
            AuthorityType = authorityType;
            UserRealmUriPrefix = userRealmUriPrefix;
            ValidateAuthority = validateAuthority;
        }

        public AuthorityInfo(AuthorityInfo other) :
            this(
                other.Host,
                other.CanonicalAuthority,
                other.AuthorityType,
                other.UserRealmUriPrefix,
                other.ValidateAuthority)
        {
        }

        public string Host { get; }
        public string CanonicalAuthority { get; }
        public AuthorityType AuthorityType { get; }
        public string UserRealmUriPrefix { get; }
        public bool ValidateAuthority { get; }

        #region Builders
        internal static AuthorityInfo FromAuthorityUri(string authorityUri, bool validateAuthority)
        {
            string canonicalUri = CanonicalizeAuthorityUri(authorityUri);
            ValidateAuthorityUri(canonicalUri);

            var authorityType = GetAuthorityType(canonicalUri);

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
            return FromAuthorityUri(new Uri(cloudInstanceUri, tenantId.ToString("D")).AbsoluteUri, validateAuthority);
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        internal static AuthorityInfo FromAadAuthority(Uri cloudInstanceUri, string tenant, bool validateAuthority)
        {
            if (Guid.TryParse(tenant, out Guid tenantId))
            {
                return FromAadAuthority(cloudInstanceUri, tenantId, validateAuthority);
            }
            return FromAuthorityUri(new Uri(cloudInstanceUri, tenant).AbsoluteUri, validateAuthority);
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

        #endregion

        #region Helpers
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
                        throw new InvalidOperationException(MsalErrorMessage.AzureAdMyOrgRequiresSpecifyingATenant);
                    }

                    return tenantId;
                default:
                    throw new ArgumentException(nameof(authorityAudience));
            }
        }

        internal static string CanonicalizeAuthorityUri(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri) && !uri.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri + "/";
            }

            return uri.ToLowerInvariant();
        }

        internal bool IsDefaultAuthority
        {
            get
            {
                return string.Equals(
                    CanonicalAuthority,
                    ClientApplicationBase.DefaultAuthority,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        #endregion

        private static void ValidateAuthorityUri(string authority)
        {
            if (string.IsNullOrWhiteSpace(authority))
            {
                throw new ArgumentNullException(nameof(authority));
            }

            if (!Uri.IsWellFormedUriString(authority, UriKind.Absolute))
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityInvalidUriFormat, nameof(authority));
            }

            var authorityUri = new Uri(authority);
            if (authorityUri.Scheme != "https")
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityUriInsecure, nameof(authority));
            }

            string path = authorityUri.AbsolutePath.Substring(1);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityUriInvalidPath, nameof(authority));
            }

            string[] pathSegments = authorityUri.AbsolutePath.Substring(1).Split('/');
            if (pathSegments == null || pathSegments.Length == 0)
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityUriInvalidPath);
            }
        }

        private static string GetAuthorityUri(
            AzureCloudInstance azureCloudInstance,
            AadAuthorityAudience authorityAudience,
            string tenantId = null)
        {
            string cloudUrl = GetCloudUrl(azureCloudInstance);
            string tenantValue = GetAadAuthorityAudienceValue(authorityAudience, tenantId);

            return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", cloudUrl, tenantValue);
        }

        // TODO: consolidate this with the same method in Authority.cs
        internal static string GetFirstPathSegment(string authority)
        {
            var uri = new Uri(authority);
            if (uri.Segments.Length >= 2)
            {
                return new Uri(authority).Segments[1]
                                         .TrimEnd('/');
            }

            throw new InvalidOperationException(MsalErrorMessage.AuthorityDoesNotHaveTwoSegments);
        }

        private static AuthorityType GetAuthorityType(string authority) 
        {
            string firstPathSegment = GetFirstPathSegment(authority);

            if (string.Equals(firstPathSegment, "adfs", StringComparison.OrdinalIgnoreCase))
            {
                return AuthorityType.Adfs;
            }

            if (string.Equals(firstPathSegment, B2CAuthority.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return AuthorityType.B2C;
            }

            return AuthorityType.Aad;
        }      
    }
}

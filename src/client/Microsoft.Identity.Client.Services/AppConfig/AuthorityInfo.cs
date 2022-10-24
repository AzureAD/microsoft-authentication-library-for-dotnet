// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;
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
            : this(authorityType, new Uri(authority), validateAuthority)
        {

        }

        public AuthorityInfo(
            AuthorityType authorityType,
            Uri authorityUri,
            bool validateAuthority)
        {
            AuthorityType = authorityType;
            ValidateAuthority = validateAuthority;

            // TODO: can we simplify this and/or move validation/configuration logic to AbstractApplicationBuilder
            // so that all authority mangling/management is in one place?

            switch (AuthorityType)
            {
                case AuthorityType.B2C:
                    string[] pathSegments = AuthorityInfo.GetPathSegments(authorityUri.AbsolutePath);

                    if (pathSegments.Length < 3)
                    {
                        throw new ArgumentException(MsalErrorMessage.B2cAuthorityUriInvalidPath);
                    }

                    CanonicalAuthority = new Uri(string.Format(
                        CultureInfo.InvariantCulture,
                        "https://{0}/{1}/{2}/{3}/",
                        authorityUri.Authority,
                        pathSegments[0],
                        pathSegments[1],
                        pathSegments[2]));
                    break;
                case AuthorityType.Dsts:
                    pathSegments = GetPathSegments(authorityUri.AbsolutePath);

                    if (pathSegments.Length < 2)
                    {
                        throw new ArgumentException(MsalErrorMessage.DstsAuthorityUriInvalidPath);
                    }

                    CanonicalAuthority = new Uri(string.Format(
                        CultureInfo.InvariantCulture,
                        "https://{0}/{1}/{2}/",
                        authorityUri.Authority,
                        pathSegments[0],
                        pathSegments[1]));

                    UserRealmUriPrefix = UriBuilderExtensions.GetHttpsUriWithOptionalPort(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "https://{0}/{1}/common/userrealm/",
                            authorityUri.Authority,
                            pathSegments[0]),
                        authorityUri.Port);
                    break;
                default:
                    CanonicalAuthority = new Uri(
                        UriBuilderExtensions.GetHttpsUriWithOptionalPort(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "https://{0}/{1}/",
                                authorityUri.Authority,
                                GetFirstPathSegment(authorityUri)),
                            authorityUri.Port));

                    UserRealmUriPrefix = UriBuilderExtensions.GetHttpsUriWithOptionalPort(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "https://{0}/common/userrealm/",
                            Host),
                        authorityUri.Port);

                    break;
            }
        }

        public AuthorityInfo(AuthorityInfo other) :
            this(
                other.CanonicalAuthority,
                other.AuthorityType,
                other.UserRealmUriPrefix,
                other.ValidateAuthority)
        {
        }

        private AuthorityInfo(
            Uri canonicalAuthority,
            AuthorityType authorityType,
            string userRealmUriPrefix,
            bool validateAuthority)
        {
            CanonicalAuthority = canonicalAuthority;
            AuthorityType = authorityType;
            UserRealmUriPrefix = userRealmUriPrefix;
            ValidateAuthority = validateAuthority;
        }

        public string Host => CanonicalAuthority.Host;
        public Uri CanonicalAuthority { get; }

        // Please avoid the direct use of this property.
        // Ideally, this property should be removed. But due to
        // dependencies and time constraints, refactoring is done in steps.
        internal AuthorityType AuthorityType { get; }
        public string UserRealmUriPrefix { get; }
        public bool ValidateAuthority { get; }

        internal bool IsInstanceDiscoverySupported => (AuthorityType == AuthorityType.Aad);

        internal bool IsUserAssertionSupported => (AuthorityType != AuthorityType.Adfs && AuthorityType != AuthorityType.B2C);

        internal bool IsTenantOverrideSupported => (AuthorityType == AuthorityType.Aad);
        internal bool IsClientInfoSupported => (AuthorityType == AuthorityType.Aad || AuthorityType == AuthorityType.Dsts || AuthorityType == AuthorityType.B2C);

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

            return uri?.ToLowerInvariant() ?? string.Empty;
        }

        internal bool IsDefaultAuthority
        {
            get
            {
                return string.Equals(
                    CanonicalAuthority.ToString(),
                    ClientApplicationBase.DefaultAuthority,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        internal Authority CreateAuthority()
        {
            switch (AuthorityType)
            {
                case AuthorityType.Adfs:
                    return new AdfsAuthority(this);

                case AuthorityType.B2C:
                    return new B2CAuthority(this);

                case AuthorityType.Aad:
                    return new AadAuthority(this);

                case AuthorityType.Dsts:
                    return new DstsAuthority(this);

                default:
                    throw new MsalClientException(
                        MsalError.InvalidAuthorityType,
                        $"Unsupported authority type {AuthorityType}");
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

        internal static string GetFirstPathSegment(string authority)
        {
            var uri = new Uri(authority);
            return GetFirstPathSegment(uri);
        }

        internal static string GetFirstPathSegment(Uri authority)
        {
            if (authority.Segments.Length >= 2)
            {
                return authority.Segments[1]
                    .TrimEnd('/');
            }

            throw new InvalidOperationException(MsalErrorMessage.AuthorityDoesNotHaveTwoSegments);
        }

        internal static string GetSecondPathSegment(string authority)
        {
            var uri = new Uri(authority);
            return GetSecondPathSegment(uri);
        }

        internal static string GetSecondPathSegment(Uri authority)
        {
            if (authority.Segments.Length >= 3)
            {
                return authority.Segments[2]
                    .TrimEnd('/');
            }

            throw new InvalidOperationException(MsalErrorMessage.DstsAuthorityDoesNotHaveThreeSegments);
        }

        private static AuthorityType GetAuthorityType(string authority) 
        {
            string firstPathSegment = GetFirstPathSegment(authority);

            if (string.Equals(firstPathSegment, "adfs", StringComparison.OrdinalIgnoreCase))
            {
                return AuthorityType.Adfs;
            }

            if (string.Equals(firstPathSegment, "dstsv2", StringComparison.OrdinalIgnoreCase))
            {
                return AuthorityType.Dsts;
            }

            if (string.Equals(firstPathSegment, B2CAuthority.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return AuthorityType.B2C;
            }

            return AuthorityType.Aad;
        }
        
        private static string[] GetPathSegments(string absolutePath)
        {
            string[] pathSegments = absolutePath.Substring(1).Split(
                new[]
                {
                    '/'
                },
                StringSplitOptions.RemoveEmptyEntries);

            return pathSegments;
        }

        /// <summary>
        /// This is extension for AuthorityInfo
        /// </summary>
        internal class AuthorityInfoHelper
        {
            public static IAuthorityValidator CreateAuthorityValidator(AuthorityInfo authorityInfo, RequestContext requestContext)
            {
                switch (authorityInfo.AuthorityType)
                {
                    case AuthorityType.Adfs:
                        return new AdfsAuthorityValidator(requestContext);
                    case AuthorityType.Aad:
                        return new AadAuthorityValidator(requestContext);
                    case AuthorityType.B2C:
                    case AuthorityType.Dsts:
                        return new NullAuthorityValidator();
                    default:
                        throw new InvalidOperationException("Invalid AuthorityType");
                }
            }

            /// <summary>
            /// Figures out the authority based on the authority from the config and the authority from the request,
            /// and optionally the homeAccountTenantId, which has an impact on AcquireTokenSilent
            ///
            /// The algorithm is:
            ///
            /// 1. If there is no request authority (i.e. no authority override), use the config authority.
            ///     1.1. For AAD, if the config authority is "common" etc, try to use the tenanted version with the home account tenant ID
            /// 2. If there is a request authority, try to use it.
            ///     2.1. If the request authority is not "common", then use it
            ///     2.2  If the request authority is "common", ignore it, and use 1.1
            ///
            /// Special cases:
            ///
            /// - if the authority is not defined at the application level and the request level is not AAD, use the request authority
            /// - if the authority is defined at app level, and the request level authority of is of different type, throw an exception
            ///
            /// </summary>
            public static async Task<Authority> CreateAuthorityForRequestAsync(RequestContext requestContext,
                AuthorityInfo requestAuthorityInfo,
                IAccount account = null)
            {
                var configAuthorityInfo = requestContext.ServiceBundle.Config.Authority.AuthorityInfo;

                if (configAuthorityInfo == null)
                {
                    throw new ArgumentNullException(nameof(requestContext.ServiceBundle.Config.Authority.AuthorityInfo));
                }

                ValidateTypeMismatch(configAuthorityInfo, requestAuthorityInfo);

                await ValidateSameHostAsync(requestAuthorityInfo, requestContext).ConfigureAwait(false);

                AuthorityInfo nonNullAuthInfo = requestAuthorityInfo ?? configAuthorityInfo;

                switch (configAuthorityInfo.AuthorityType)
                {
                    // ADFS is tenant-less, no need to consider tenant
                    case AuthorityType.Adfs:
                        return new AdfsAuthority(nonNullAuthInfo);

                    case AuthorityType.Dsts:
                        return new DstsAuthority(nonNullAuthInfo);

                    case AuthorityType.B2C:
                        return new B2CAuthority(nonNullAuthInfo);

                    case AuthorityType.Aad:

                        bool updateEnvironment = requestContext.ServiceBundle.Config.MultiCloudSupportEnabled && account != null;

                        if (requestAuthorityInfo == null)
                        {
                            return updateEnvironment ?
                                CreateAuthorityWithTenant(CreateAuthorityWithEnvironment(configAuthorityInfo, account.Environment).AuthorityInfo, account?.HomeAccountId?.TenantId) :
                                CreateAuthorityWithTenant(configAuthorityInfo, account?.HomeAccountId?.TenantId);
                        }

                        // In case the authority is defined only at the request level
                        if (configAuthorityInfo.IsDefaultAuthority &&
                            requestAuthorityInfo.AuthorityType != AuthorityType.Aad)
                        {
                            return requestAuthorityInfo.CreateAuthority();
                        }

                        var requestAuthority = updateEnvironment ?
                            new AadAuthority(CreateAuthorityWithEnvironment(requestAuthorityInfo, account?.Environment).AuthorityInfo) :
                            new AadAuthority(requestAuthorityInfo);
                        if (!requestAuthority.IsCommonOrganizationsOrConsumersTenant())
                        {
                            return requestAuthority;
                        }

                        return updateEnvironment ?
                                CreateAuthorityWithTenant(CreateAuthorityWithEnvironment(configAuthorityInfo, account.Environment).AuthorityInfo, account?.HomeAccountId?.TenantId) :
                                CreateAuthorityWithTenant(configAuthorityInfo, account?.HomeAccountId?.TenantId);

                    default:
                        throw new MsalClientException(
                            MsalError.InvalidAuthorityType,
                            "Unsupported authority type");
                }
            }

            internal static Authority CreateAuthorityWithTenant(AuthorityInfo authorityInfo, string tenantId)
            {
                Authority initialAuthority = authorityInfo.CreateAuthority();

                if (string.IsNullOrEmpty(tenantId))
                {
                    return initialAuthority;
                }

                string tenantedAuthority = initialAuthority.GetTenantedAuthority(tenantId);

                return Authority.CreateAuthority(tenantedAuthority, authorityInfo.ValidateAuthority);
            }

            internal static Authority CreateAuthorityWithEnvironment(AuthorityInfo authorityInfo, string environment)
            {
                var uriBuilder = new UriBuilder(authorityInfo.CanonicalAuthority)
                {
                    Host = environment
                };

                return Authority.CreateAuthority(uriBuilder.Uri.AbsoluteUri, authorityInfo.ValidateAuthority);
            }

            private static void ValidateTypeMismatch(AuthorityInfo configAuthorityInfo, AuthorityInfo requestAuthorityInfo)
            {
                if (!configAuthorityInfo.IsDefaultAuthority &&
                                requestAuthorityInfo != null &&
                                configAuthorityInfo.AuthorityType != requestAuthorityInfo.AuthorityType)
                {
                    throw new MsalClientException(
                        MsalError.AuthorityTypeMismatch,
                        MsalErrorMessage.AuthorityTypeMismatch(
                            configAuthorityInfo.AuthorityType,
                            requestAuthorityInfo.AuthorityType));
                }
            }

            private static async Task ValidateSameHostAsync(AuthorityInfo requestAuthorityInfo, RequestContext requestContext)
            {
                var configAuthorityInfo = requestContext.ServiceBundle.Config.Authority.AuthorityInfo;

                if (!requestContext.ServiceBundle.Config.MultiCloudSupportEnabled &&
                    requestAuthorityInfo != null &&
                    !string.Equals(requestAuthorityInfo.Host, configAuthorityInfo.Host, StringComparison.OrdinalIgnoreCase))
                {
                    if (requestAuthorityInfo.AuthorityType == AuthorityType.B2C)
                    {
                        throw new MsalClientException(MsalError.B2CAuthorityHostMismatch, MsalErrorMessage.B2CAuthorityHostMisMatch);
                    }

                    // This check should be done when validating the request parameters, however we've allowed
                    // this configuration to run for a while, so this is the better place for it.
                    bool usesRegional = !string.IsNullOrEmpty(requestContext.ServiceBundle.Config.AzureRegion);
                    if (usesRegional)
                    {
                        throw new MsalClientException(MsalError.RegionalAndAuthorityOverride, MsalErrorMessage.RegionalAndAuthorityOverride);
                    }

                    var authorityAliased = await IsAuthorityAliasedAsync(requestContext, requestAuthorityInfo).ConfigureAwait(false);
                    if (authorityAliased)
                    {
                        return;
                    }

                    if (configAuthorityInfo.IsDefaultAuthority)
                    {
                        throw new MsalClientException(
                            MsalError.AuthorityHostMismatch,
                            $"You did not define an authority at the application level, so it defaults to the https://login.microsoftonline.com/common. " +
                            $"\n\rHowever, the request is for a different cloud {requestAuthorityInfo.Host}. This is not supported - the app and the request must target the same cloud. " +
                            $"\n\r\n\r Add .WithAuthority(\"https://{requestAuthorityInfo.Host}/common\") in the app builder. " +
                            $"\n\rSee https://aka.ms/msal-net-authority-override for details");
                    }

                    throw new MsalClientException(
                        MsalError.AuthorityHostMismatch,
                        $"\n\r The application is configured for cloud {configAuthorityInfo.Host} and the request for a different cloud - {requestAuthorityInfo.Host}. This is not supported - the app and the request must target the same cloud. " +
                        $"\n\rSee https://aka.ms/msal-net-authority-override for details");
                }
            }

            private static async Task<bool> IsAuthorityAliasedAsync(RequestContext requestContext, AuthorityInfo requestAuthorityInfo)
            {
                var instanceDiscoveryManager = requestContext.ServiceBundle.InstanceDiscoveryManager;
                var result = await instanceDiscoveryManager.GetMetadataEntryAsync(requestContext.ServiceBundle.Config.Authority.AuthorityInfo, requestContext).ConfigureAwait(false);

                return result.Aliases.Any(alias => alias.Equals(requestAuthorityInfo.Host));
            }
        }
    }
}

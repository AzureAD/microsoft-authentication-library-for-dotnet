// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    ///
    /// </summary>
    internal abstract class Authority
    {
        protected Authority(AuthorityInfo authorityInfo)
        {
            if (authorityInfo == null)
            {
                throw new ArgumentNullException(nameof(authorityInfo));
            }

            // Don't reuse the same authority info, instead copy it
            // to prevent objects updating each other's details
            AuthorityInfo = new AuthorityInfo(authorityInfo);
        }

        public AuthorityInfo AuthorityInfo { get; }

        #region Builders

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
        /// </summary>
        public static Authority CreateAuthorityForRequest(
            AuthorityInfo configAuthorityInfo,
            AuthorityInfo requestAuthorityInfo,
            string requestHomeAccountTenantId = null)
        {
            if (configAuthorityInfo == null)
            {
                throw new ArgumentNullException(nameof(configAuthorityInfo));
            }

            switch (configAuthorityInfo.AuthorityType)
            {
                // ADFS and B2C are tenantless, no need to consider tenant
                case AuthorityType.Adfs:
                    return requestAuthorityInfo == null ?
                        new AdfsAuthority(configAuthorityInfo) :
                        new AdfsAuthority(requestAuthorityInfo);

                case AuthorityType.B2C:

                    if (requestAuthorityInfo != null)
                    {
                        CheckB2CAuthorityHost(requestAuthorityInfo, configAuthorityInfo);
                        return new B2CAuthority(requestAuthorityInfo);
                    }
                    return new B2CAuthority(configAuthorityInfo);

                case AuthorityType.Aad:

                    if (requestAuthorityInfo == null)
                    {
                        return CreateAuthorityWithTenant(configAuthorityInfo, requestHomeAccountTenantId);
                    }

                    var requestAuthority = new AadAuthority(requestAuthorityInfo);
                    if (!requestAuthority.IsCommonOrganizationsOrConsumersTenant())
                    {
                        return requestAuthority;
                    }

                    return CreateAuthorityWithTenant(configAuthorityInfo, requestHomeAccountTenantId);

                default:
                    throw new MsalClientException(
                        MsalError.InvalidAuthorityType,
                        "Unsupported authority type");
            }
        }

        public static Authority CreateAuthority(string authority, bool validateAuthority = false)
        {
            return CreateAuthority(AuthorityInfo.FromAuthorityUri(authority, validateAuthority));
        }

        public static Authority CreateAuthority(AuthorityInfo authorityInfo)
        {
            switch (authorityInfo.AuthorityType)
            {
                case AuthorityType.Adfs:
                    return new AdfsAuthority(authorityInfo);

                case AuthorityType.B2C:
                    return new B2CAuthority(authorityInfo);

                case AuthorityType.Aad:
                    return new AadAuthority(authorityInfo);

                default:
                    throw new MsalClientException(
                        MsalError.InvalidAuthorityType,
                        "Unsupported authority type " + authorityInfo.AuthorityType);
            }
        }

        internal static Authority CreateAuthorityWithTenant(AuthorityInfo authorityInfo, string tenantId)
        {
            var initialAuthority = CreateAuthority(authorityInfo);

            if (string.IsNullOrEmpty(tenantId))
            {
                return initialAuthority;
            }

            string tenantedAuthority = initialAuthority.GetTenantedAuthority(tenantId);

            return CreateAuthority(tenantedAuthority, authorityInfo.ValidateAuthority);
        }

        internal static Authority CreateAuthorityWithEnvironment(AuthorityInfo authorityInfo, string environment)
        {
            var uriBuilder = new UriBuilder(authorityInfo.CanonicalAuthority)
            {
                Host = environment
            };

            return CreateAuthority(uriBuilder.Uri.AbsoluteUri, authorityInfo.ValidateAuthority);
        }

        #endregion Builders

        internal static string GetFirstPathSegment(string authority)
        {
            var uri = new Uri(authority);
            if (uri.Segments.Length > 1)
            {
                return uri.Segments[1].TrimEnd('/');
            }
            return string.Empty;
        }

        internal static AuthorityType GetAuthorityType(string authority)
        {
            string firstPathSegment = GetFirstPathSegment(authority);

            if (string.Equals(firstPathSegment, "adfs", StringComparison.OrdinalIgnoreCase))
            {
                return AuthorityType.Adfs;
            }
            else if (string.Equals(firstPathSegment, B2CAuthority.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return AuthorityType.B2C;
            }
            else
            {
                return AuthorityType.Aad;
            }
        }

        internal abstract string GetTenantId();

        /// <summary>
        /// Gets a tenanted authority if the current authority is tenantless.
        /// Returns the original authority on B2C and ADFS
        /// </summary>
        internal abstract string GetTenantedAuthority(string tenantId);

        private static void CheckB2CAuthorityHost(AuthorityInfo requestAuthorityInfo, AuthorityInfo configAuthorityInfo)
        {
            if (configAuthorityInfo.Host != requestAuthorityInfo.Host)
            {
                throw new MsalClientException(MsalError.B2CAuthorityHostMismatch, MsalErrorMessage.B2CAuthorityHostMisMatch);
            }
        }

        internal static string GetEnviroment(string authority)
        {
            return new Uri(authority).Host;
        }
    }
}

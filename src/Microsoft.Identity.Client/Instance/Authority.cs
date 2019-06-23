// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    internal abstract class Authority
    {
        protected static readonly HashSet<string> TenantlessTenantNames = new HashSet<string>(
            new[]
            {
                "common",
                "organizations",
                "consumers"
            },
            StringComparer.OrdinalIgnoreCase);

        protected Authority(IServiceBundle serviceBundle, AuthorityInfo authorityInfo)
        {
            ServiceBundle = serviceBundle;
            AuthorityInfo = authorityInfo;
        }

        public AuthorityInfo AuthorityInfo { get; }

        protected IServiceBundle ServiceBundle { get; }

        public static Authority CreateAuthorityWithOverride(IServiceBundle serviceBundle, AuthorityInfo authorityInfo)
        {
            switch (authorityInfo.AuthorityType)
            {
                case AuthorityType.Adfs:
                    return new AdfsAuthority(serviceBundle, authorityInfo);

                case AuthorityType.B2C:
                    CheckB2CAuthorityHost(serviceBundle, authorityInfo);
                    return new B2CAuthority(serviceBundle, authorityInfo);

                case AuthorityType.Aad:
                    return new AadAuthority(serviceBundle, authorityInfo);

                default:
                    throw new MsalClientException(
                        MsalError.InvalidAuthorityType,
                        "Unsupported authority type");
            }
        }

        public static Authority CreateAuthority(IServiceBundle serviceBundle, string authority, bool validateAuthority = false)
        {
            return CreateAuthorityWithOverride(
                serviceBundle,
                AuthorityInfo.FromAuthorityUri(authority, validateAuthority));
        }

        public static Authority CreateAuthority(IServiceBundle serviceBundle)
        {
            return CreateAuthorityWithOverride(serviceBundle, serviceBundle.Config.AuthorityInfo);
        }

        /// <summary>
        /// Creates a tenanted authority, using account tenantId, if the one from the service bundle is tenantless
        /// </summary>
        public static Authority CreateAuthorityWithAccountTenant(IServiceBundle serviceBundle, IAccount account)
        {
            var authority = CreateAuthority(serviceBundle);
            authority.UpdateWithTenant(account?.HomeAccountId?.TenantId);

            return authority;
        }

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

        /// <summary>
        /// Updates a tenantless authority with a tenant Id. NO-OP for B2C and ADFS.
        /// </summary>
        /// <param name="tenantId"></param>
        internal void UpdateWithTenant(string tenantId)
        {
            AuthorityInfo.CanonicalAuthority = GetTenantedAuthority(tenantId);
        }

        internal static string CreateAuthorityWithEnvironment(string authority, string environment)
        {
            var uriBuilder = new UriBuilder(authority)
            {
                Host = environment
            };

            return uriBuilder.Uri.AbsoluteUri;
        }

        private static void CheckB2CAuthorityHost(IServiceBundle serviceBundle, AuthorityInfo authorityInfo)
        {
            if (serviceBundle.Config.AuthorityInfo.Host != authorityInfo.Host)
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// 
    /// </summary>
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

        protected Authority(AuthorityInfo authorityInfo)
        {
            if (authorityInfo==null)
            {
                throw new ArgumentNullException(nameof(authorityInfo));
            }

            // Don't reuse the same authority info, instead copy it
            // to prevent objects updating each other's details
            AuthorityInfo = new AuthorityInfo(authorityInfo);
        }

        public AuthorityInfo AuthorityInfo { get; }

        #region Builders
        public static Authority CreateAuthorityWithOverride(AuthorityInfo requestAuthorityInfo, AuthorityInfo configAuthorityInfo)
        {
            switch (requestAuthorityInfo.AuthorityType)
            {
                case AuthorityType.Adfs:
                    return new AdfsAuthority(requestAuthorityInfo);

                case AuthorityType.B2C:
                    if (configAuthorityInfo != null)
                    {
                        CheckB2CAuthorityHost(requestAuthorityInfo, configAuthorityInfo);
                    }
                    return new B2CAuthority(requestAuthorityInfo);

                case AuthorityType.Aad:
                    return new AadAuthority(requestAuthorityInfo);

                default:
                    throw new MsalClientException(
                        MsalError.InvalidAuthorityType,
                        "Unsupported authority type");
            }
        }

        public static Authority CreateAuthority(string authority, bool validateAuthority = false)
        {
            return CreateAuthorityWithOverride(AuthorityInfo.FromAuthorityUri(authority, validateAuthority), null);
        }

        public static Authority CreateAuthority(AuthorityInfo authorityInfo)
        {
            return CreateAuthorityWithOverride(authorityInfo, null);
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

        //internal static string CreateAuthorityWithEnvironment(string authority, string environment)
        //{
        //    var uriBuilder = new UriBuilder(authority)
        //    {
        //        Host = environment
        //    };

        //    return uriBuilder.Uri.AbsoluteUri;
        //}


        #endregion

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

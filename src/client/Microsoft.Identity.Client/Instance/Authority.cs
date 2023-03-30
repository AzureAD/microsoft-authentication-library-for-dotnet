// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    /// <remarks>
    /// Must be kept immutable
    /// </remarks>
    [DebuggerDisplay("{AuthorityInfo.CanonicalAuthority}")]
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
        /// Special cases:
        ///
        /// - if the authority is not defined at the application level and the request level is not AAD, use the request authority
        /// - if the authority is defined at app level, and the request level authority of is of different type, throw an exception
        ///
        /// </summary>
        public static async Task<Authority> CreateAuthorityForRequestAsync(
            RequestContext requestContext,
            AuthorityInfo requestAuthorityInfo,
            IAccount account = null)
        {
            return await AuthorityInfo.AuthorityInfoHelper.CreateAuthorityForRequestAsync(requestContext, requestAuthorityInfo, account).ConfigureAwait(false);
        }

        public static Authority CreateAuthority(string authority, bool validateAuthority = false)
        {
            return AuthorityInfo.FromAuthorityUri(authority, validateAuthority).CreateAuthority();
        }

        public static Authority CreateAuthority(AuthorityInfo authorityInfo)
        {
            return authorityInfo.CreateAuthority();
        }

        internal static Authority CreateAuthorityWithTenant(AuthorityInfo authorityInfo, string tenantId)
        {
            Authority initialAuthority = CreateAuthority(authorityInfo);

            if (string.IsNullOrEmpty(tenantId))
            {
                return initialAuthority;
            }

            string tenantedAuthority = initialAuthority.GetTenantedAuthority(tenantId);

            return CreateAuthority(tenantedAuthority, authorityInfo.ValidateAuthority);
        }

        internal static Authority CreateAuthorityWithEnvironment(AuthorityInfo authorityInfo, string environment)
        {
            if (authorityInfo.AuthorityType == AuthorityType.Generic)
            {
                return CreateAuthority(authorityInfo);
            }

            var uriBuilder = new UriBuilder(authorityInfo.CanonicalAuthority)
            {
                Host = environment
            };

            return CreateAuthority(uriBuilder.Uri.AbsoluteUri, authorityInfo.ValidateAuthority);
        }

        #endregion Builders

        #region Abstract
        internal abstract string TenantId { get; }

        /// <summary>
        /// Gets a tenanted authority if the current authority is tenant-less.
        /// Returns the original authority on B2C and ADFS
        /// </summary>
        internal abstract string GetTenantedAuthority(string tenantId, bool forceTenantless = false);

        internal abstract Task<string> GetTokenEndpointAsync(RequestContext requestContext);

        internal abstract Task<string> GetAuthorizationEndpointAsync(RequestContext requestContext);

        internal abstract Task<string> GetDeviceCodeEndpointAsync(RequestContext requestContext);
        #endregion

        internal static string GetEnvironment(string authority)
        {
            return new Uri(authority).Host;
        }
    }
}

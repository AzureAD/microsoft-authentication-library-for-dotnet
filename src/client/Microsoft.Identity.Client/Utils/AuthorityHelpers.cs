// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Utils
{
    internal static class AuthorityHelpers
    {
        /// <summary>
        /// Retrieve the TenantId for an Authority URL.
        /// </summary>
        /// <param name="authorityUri">The Authority URL to parse.</param>
        /// <returns>The Tenant Id</returns>
        /// <remarks>
        /// The Tenant Id can be NULL if the Authority Type is ADFS
        /// </remarks>
        public static string GetTenantId(Uri authorityUri)
        {
            var authorityInfo = AuthorityInfo.FromAuthorityUri(authorityUri.ToString(), false);
            var authority = authorityInfo.CreateAuthority();
            return authority.TenantId;
        }
    }
}

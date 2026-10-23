// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Oidc;
using Microsoft.Identity.Client.Internal;

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

        internal static async Task<string> ResolveTenantIdAsync(
            AuthorityInfo authorityInfo,
            RequestContext requestContext,
            bool allowNetwork)
        {
            if (authorityInfo.AuthorityType != AuthorityType.Aad)
            {
                return null;
            }

            string tenant = GetTenantId(authorityInfo.CanonicalAuthority);
            if (Guid.TryParse(tenant, out Guid tenantGuid))
            {
                return tenantGuid.ToString("D");
            }

            if (string.IsNullOrEmpty(tenant) || !tenant.Contains("."))
            {
                return null;
            }

            var builder = new UriBuilder(authorityInfo.CanonicalAuthority)
            {
                Path = authorityInfo.CanonicalAuthority.AbsolutePath.TrimEnd('/') + "/v2.0"
            };

            try
            {
                if (!OidcRetrieverWithCache.TryGetCachedOidc(builder.Uri.AbsoluteUri, out OidcMetadata metadata))
                {
                    if (!allowNetwork)
                    {
                        return null;
                    }

                    metadata = await OidcRetrieverWithCache.GetOidcAsync(
                        builder.Uri.AbsoluteUri, requestContext).ConfigureAwait(false);
                }

                if (Uri.TryCreate(metadata.Issuer, UriKind.Absolute, out Uri issuer) && issuer.Segments.Length > 1 &&
                    Guid.TryParse(GetTenantId(issuer), out tenantGuid))
                {
                    return tenantGuid.ToString("D");
                }
            }
            catch (MsalException) when (requestContext.UserCancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(requestContext.UserCancellationToken);
            }
            catch (MsalException)
            {
                requestContext.Logger.Warning("Could not resolve the tenant ID from OIDC metadata. Using the available cache tenant.");
            }

            return null;
        }
    }
}

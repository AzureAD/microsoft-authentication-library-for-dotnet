// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance.Oidc;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    internal class GenericAuthority : Authority
    {

        internal GenericAuthority(AuthorityInfo authorityInfo)
            : base(authorityInfo)
        {

        }

        internal override string TenantId => null;

        internal override string GetTenantedAuthority(string tenantId, bool forceTenantless = false)
        {
            throw new NotImplementedException();
        }

        internal override async Task<string> GetTokenEndpointAsync(RequestContext requestContext)
        {
            var configuration = await OidcRetrieverWithCache.GetOidcAsync(
                AuthorityInfo.CanonicalAuthority.AbsoluteUri, requestContext)
                    .ConfigureAwait(false);

            return configuration.TokenEndpoint;
        }

        internal override async Task<string> GetAuthorizationEndpointAsync(
            RequestContext requestContext)
        {
            OidcMetadata configuration = await OidcRetrieverWithCache.GetOidcAsync(
               AuthorityInfo.CanonicalAuthority.AbsoluteUri, requestContext)
                .ConfigureAwait(false);

            return configuration.AuthorizationEndpoint;
        }

        internal override Task<string> GetDeviceCodeEndpointAsync(RequestContext requestContext)
        {
            // prevents device_code flow which requires knowledge of the device_authorization_endpoint.
            throw new NotImplementedException();
        }
    }
}

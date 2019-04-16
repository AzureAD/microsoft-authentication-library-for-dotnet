// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    internal class B2COpenIdConfigurationEndpointManager : IOpenIdConfigurationEndpointManager
    {
        /// <inheritdoc />
        public Task<string> ValidateAuthorityAndGetOpenIdDiscoveryEndpointAsync(
            AuthorityInfo authorityInfo,
            string userPrincipalName,
            RequestContext requestContext)
        {
                string defaultEndpoint = string.Format(
                    CultureInfo.InvariantCulture,
                    new Uri(authorityInfo.CanonicalAuthority).AbsoluteUri + Constants.OpenIdConfigurationEndpoint);
                return Task.FromResult(defaultEndpoint);
        }
    }
}

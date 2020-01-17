// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    internal interface IOpenIdConfigurationEndpointManager
    {
        /// <summary>
        /// Validates the authority if required and returns the OpenId discovery endpoint
        /// for the given tenant. This is specific to each authority type.
        /// </summary>
        Task<string> ValidateAuthorityAndGetOpenIdDiscoveryEndpointAsync(
            AuthorityInfo authorityInfo,
            string userPrincipalName,
            RequestContext requestContext);
    }
}

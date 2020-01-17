// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    internal interface IAuthorityEndpointResolutionManager
    {
        Task<AuthorityEndpoints> ResolveEndpointsAsync(
            AuthorityInfo authorityInfo,
            string userPrincipalName,
            RequestContext requestContext);
    }
}

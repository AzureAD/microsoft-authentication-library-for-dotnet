// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    internal interface IAuthorityResolutionManager
    {
        AuthorityEndpoints ResolveEndpoints(
          Authority authority,
          string userPrincipalName,
          RequestContext requestContext);

        Task ValidateAuthorityAsync(Authority authority, RequestContext context);
    }
}

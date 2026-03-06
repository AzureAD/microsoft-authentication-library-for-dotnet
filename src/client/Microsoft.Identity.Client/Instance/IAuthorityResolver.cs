// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// Resolves the final environment and endpoint information for an authority, for example
    /// by performing instance discovery or applying a cloud-specific override.
    /// Each authority registration supplies its own implementation.
    /// </summary>
    internal interface IAuthorityResolver
    {
        /// <summary>
        /// Resolves the authority information, optionally performing network calls.
        /// </summary>
        /// <param name="authorityInfo">The normalized authority info to resolve.</param>
        /// <param name="requestContext">The current request context, providing access to configuration and network.</param>
        /// <returns>The resolved <see cref="AuthorityInfo"/>.</returns>
        Task<AuthorityInfo> ResolveAsync(AuthorityInfo authorityInfo, RequestContext requestContext);
    }
}

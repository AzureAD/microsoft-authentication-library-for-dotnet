// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// Performs authority-type-specific normalization of a raw URI into a canonical <see cref="AuthorityInfo"/>.
    /// Each authority registration supplies its own implementation.
    /// </summary>
    internal interface IAuthorityNormalizer
    {
        /// <summary>
        /// Normalizes the given URI and returns the corresponding <see cref="AuthorityInfo"/>.
        /// </summary>
        /// <param name="authorityUri">The authority URI to normalize.</param>
        /// <returns>A normalized <see cref="AuthorityInfo"/> instance.</returns>
        AuthorityInfo Normalize(System.Uri authorityUri);
    }
}

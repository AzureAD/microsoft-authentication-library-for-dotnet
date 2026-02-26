// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Registrations
{
    /// <summary>
    /// Pass-through <see cref="IAuthorityResolver"/> that returns the authority info unchanged.
    /// Used by authority types that do not require environment or endpoint resolution.
    /// </summary>
    internal sealed class PassThroughAuthorityResolver : IAuthorityResolver
    {
        /// <inheritdoc/>
        public Task<AuthorityInfo> ResolveAsync(AuthorityInfo authorityInfo, RequestContext requestContext)
        {
            return Task.FromResult(authorityInfo);
        }
    }

    /// <summary>
    /// Pass-through <see cref="IAuthorityNormalizer"/> that creates an <see cref="AuthorityInfo"/>
    /// using <see cref="AuthorityInfo.FromAuthorityUri"/> with no additional transformation.
    /// </summary>
    internal sealed class DefaultAuthorityNormalizer : IAuthorityNormalizer
    {
        private readonly AuthorityType _authorityType;
        private readonly bool _validateAuthority;

        /// <summary>
        /// Initializes a new <see cref="DefaultAuthorityNormalizer"/>.
        /// </summary>
        /// <param name="authorityType">The authority type to use when constructing the <see cref="AuthorityInfo"/>.</param>
        /// <param name="validateAuthority">Whether authority validation should be enabled.</param>
        public DefaultAuthorityNormalizer(AuthorityType authorityType, bool validateAuthority = false)
        {
            _authorityType = authorityType;
            _validateAuthority = validateAuthority;
        }

        /// <inheritdoc/>
        public AuthorityInfo Normalize(Uri authorityUri)
        {
            return new AuthorityInfo(_authorityType, authorityUri, _validateAuthority);
        }
    }
}

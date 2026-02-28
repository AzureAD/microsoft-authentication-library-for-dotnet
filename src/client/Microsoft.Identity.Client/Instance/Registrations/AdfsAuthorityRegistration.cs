// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Registrations
{
    /// <summary>
    /// <see cref="AuthorityRegistration"/> for Active Directory Federation Services (ADFS) authorities.
    /// </summary>
    /// <remarks>
    /// Detection: the authority path starts with "/adfs" (case-insensitive).
    /// Factory:   <see cref="AdfsAuthority"/>
    /// Validator: <see cref="AdfsAuthorityValidator"/> (performs web-finger validation when ValidateAuthority is true)
    /// Resolver:  Pass-through
    /// Normalizer: <see cref="DefaultAuthorityNormalizer"/> with validateAuthority = true
    /// </remarks>
    internal static class AdfsAuthorityRegistration
    {
        /// <summary>
        /// Creates the <see cref="AuthorityRegistration"/> for ADFS authorities.
        /// </summary>
        /// <returns>A configured <see cref="AuthorityRegistration"/>.</returns>
        public static AuthorityRegistration Create()
        {
            return new AuthorityRegistration(
                type: AuthorityType.Adfs,
                detectionPredicate: IsAdfs,
                factory: info => new AdfsAuthority(info),
                validatorFactory: ctx => ctx != null
                    ? (IAuthorityValidator)new AdfsAuthorityValidator(ctx)
                    : new NullAuthorityValidator(),
                resolver: new PassThroughAuthorityResolver(),
                normalizer: new DefaultAuthorityNormalizer(AuthorityType.Adfs, validateAuthority: true));
        }

        /// <summary>
        /// Detects ADFS authorities.
        /// An authority is considered ADFS if its path starts with "/adfs".
        /// </summary>
        private static bool IsAdfs(Uri uri)
        {
            return uri.AbsolutePath.StartsWith("/adfs", StringComparison.OrdinalIgnoreCase);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Registrations
{
    /// <summary>
    /// <see cref="AuthorityRegistration"/> for Azure AD for Customers (CIAM) authorities.
    /// </summary>
    /// <remarks>
    /// Detection: host ends with ".ciamlogin.com" (case-insensitive).
    /// Factory:   <see cref="CiamAuthority"/>
    /// Validator: <see cref="NullAuthorityValidator"/> (CIAM does not support authority validation)
    /// Resolver:  Pass-through
    /// Normalizer: <see cref="DefaultAuthorityNormalizer"/> with validateAuthority = false
    /// </remarks>
    internal static class CiamAuthorityRegistration
    {
        /// <summary>
        /// Creates the <see cref="AuthorityRegistration"/> for CIAM authorities.
        /// </summary>
        /// <returns>A configured <see cref="AuthorityRegistration"/>.</returns>
        public static AuthorityRegistration Create()
        {
            return new AuthorityRegistration(
                type: AuthorityType.Ciam,
                detectionPredicate: IsCiam,
                factory: info => new CiamAuthority(info),
                validatorFactory: _ => new NullAuthorityValidator(),
                resolver: new PassThroughAuthorityResolver(),
                normalizer: new DefaultAuthorityNormalizer(AuthorityType.Ciam, validateAuthority: false));
        }

        /// <summary>
        /// Detects CIAM authorities.
        /// An authority is considered CIAM if its host ends with ".ciamlogin.com".
        /// </summary>
        private static bool IsCiam(Uri uri)
        {
            return uri.Host.EndsWith(Internal.Constants.CiamAuthorityHostSuffix, StringComparison.OrdinalIgnoreCase);
        }
    }
}

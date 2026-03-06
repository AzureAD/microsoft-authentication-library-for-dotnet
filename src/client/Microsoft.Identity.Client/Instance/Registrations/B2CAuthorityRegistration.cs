// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Registrations
{
    /// <summary>
    /// <see cref="AuthorityRegistration"/> for Azure AD B2C authorities.
    /// </summary>
    /// <remarks>
    /// Detection: host contains ".b2clogin.com" OR the path contains "b2c_1_".
    /// Factory:   <see cref="B2CAuthority"/>
    /// Validator: <see cref="NullAuthorityValidator"/> (B2C does not support authority validation)
    /// Resolver:  Pass-through
    /// Normalizer: <see cref="DefaultAuthorityNormalizer"/> with validateAuthority = false
    /// </remarks>
    internal static class B2CAuthorityRegistration
    {
        /// <summary>
        /// Creates the <see cref="AuthorityRegistration"/> for B2C authorities.
        /// </summary>
        /// <returns>A configured <see cref="AuthorityRegistration"/>.</returns>
        public static AuthorityRegistration Create()
        {
            return new AuthorityRegistration(
                type: AuthorityType.B2C,
                detectionPredicate: IsB2C,
                factory: info => new B2CAuthority(info),
                validatorFactory: _ => new NullAuthorityValidator(),
                resolver: new PassThroughAuthorityResolver(),
                normalizer: new DefaultAuthorityNormalizer(AuthorityType.B2C, validateAuthority: false));
        }

        /// <summary>
        /// Detects B2C authorities.
        /// An authority is considered B2C if its host contains ".b2clogin.com"
        /// or its path contains the "b2c_1_" policy prefix.
        /// </summary>
        private static bool IsB2C(Uri uri)
        {
            return AuthorityDetectionHelpers.IsB2CUri(uri);
        }
    }
}

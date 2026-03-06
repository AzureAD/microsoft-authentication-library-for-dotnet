// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Registrations
{
    /// <summary>
    /// <see cref="AuthorityRegistration"/> for Generic (OIDC-compliant) authorities.
    /// This is the catch-all registration and is always evaluated last.
    /// </summary>
    /// <remarks>
    /// Detection: always returns <see langword="true"/> (catch-all).
    /// Factory:   <see cref="GenericAuthority"/>
    /// Validator: <see cref="NullAuthorityValidator"/> (generic authorities do not validate)
    /// Resolver:  Pass-through
    /// Normalizer: <see cref="DefaultAuthorityNormalizer"/> with validateAuthority = false
    /// </remarks>
    internal static class GenericAuthorityRegistration
    {
        /// <summary>
        /// Creates the catch-all <see cref="AuthorityRegistration"/> for Generic authorities.
        /// </summary>
        /// <returns>A configured <see cref="AuthorityRegistration"/>.</returns>
        public static AuthorityRegistration Create()
        {
            return new AuthorityRegistration(
                type: AuthorityType.Generic,
                detectionPredicate: _ => true,
                factory: info => new GenericAuthority(info),
                validatorFactory: _ => new NullAuthorityValidator(),
                resolver: new PassThroughAuthorityResolver(),
                normalizer: new DefaultAuthorityNormalizer(AuthorityType.Generic, validateAuthority: false));
        }
    }
}

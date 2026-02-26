// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Registrations
{
    /// <summary>
    /// <see cref="AuthorityRegistration"/> for DSTS (Distributed STS) authorities.
    /// </summary>
    /// <remarks>
    /// Detection: host contains "dstsv2" OR host ends with ".dsts.core.windows.net".
    /// Factory:   <see cref="DstsAuthority"/>
    /// Validator: <see cref="NullAuthorityValidator"/> (DSTS does not support authority validation)
    /// Resolver:  Pass-through
    /// Normalizer: <see cref="DefaultAuthorityNormalizer"/> with validateAuthority = false
    /// </remarks>
    internal static class DstsAuthorityRegistration
    {
        /// <summary>
        /// Creates the <see cref="AuthorityRegistration"/> for DSTS authorities.
        /// </summary>
        /// <returns>A configured <see cref="AuthorityRegistration"/>.</returns>
        public static AuthorityRegistration Create()
        {
            return new AuthorityRegistration(
                type: AuthorityType.Dsts,
                detectionPredicate: IsDsts,
                factory: info => new DstsAuthority(info),
                validatorFactory: _ => new NullAuthorityValidator(),
                resolver: new PassThroughAuthorityResolver(),
                normalizer: new DefaultAuthorityNormalizer(AuthorityType.Dsts, validateAuthority: false));
        }

        /// <summary>
        /// Detects DSTS authorities.
        /// An authority is considered DSTS if its host contains "dstsv2" or ends with ".dsts.core.windows.net".
        /// </summary>
        private static bool IsDsts(Uri uri)
        {
            string host = uri.Host;
            return host.Contains("dstsv2") ||
                   host.EndsWith(".dsts.core.windows.net", StringComparison.OrdinalIgnoreCase);
        }
    }
}

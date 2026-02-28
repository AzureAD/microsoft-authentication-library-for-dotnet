// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Registrations
{
    /// <summary>
    /// <see cref="AuthorityRegistration"/> for Azure Active Directory (AAD) authorities.
    /// </summary>
    /// <remarks>
    /// Detection: the authority host is a known AAD host (e.g. login.microsoftonline.com) or
    /// does not match any other authority type (catch-all for AAD).
    /// Factory:   <see cref="AadAuthority"/>
    /// Validator: <see cref="AadAuthorityValidator"/> (performs instance discovery when ValidateAuthority is true)
    /// Resolver:  Pass-through (environment override is handled by <see cref="AuthorityMerger"/>)
    /// Normalizer: <see cref="DefaultAuthorityNormalizer"/> with validateAuthority = true
    /// </remarks>
    internal static class AadAuthorityRegistration
    {
        /// <summary>
        /// Creates the <see cref="AuthorityRegistration"/> for AAD authorities.
        /// </summary>
        /// <returns>A configured <see cref="AuthorityRegistration"/>.</returns>
        public static AuthorityRegistration Create()
        {
            return new AuthorityRegistration(
                type: AuthorityType.Aad,
                detectionPredicate: IsAad,
                factory: info => new AadAuthority(info),
                validatorFactory: ctx => ctx != null
                    ? (IAuthorityValidator)new AadAuthorityValidator(ctx)
                    : new NullAuthorityValidator(),
                resolver: new PassThroughAuthorityResolver(),
                normalizer: new DefaultAuthorityNormalizer(AuthorityType.Aad, validateAuthority: true));
        }

        /// <summary>
        /// Detects AAD authorities.
        /// An authority is considered AAD if its host ends with a known AAD domain or
        /// it does not match any of the more-specific authority types.
        /// This predicate is intentionally broad; it is registered after all specific
        /// types so it acts as the final AAD catch-all before <see cref="GenericAuthorityRegistration"/>.
        /// </summary>
        private static bool IsAad(Uri uri)
        {
            // Exclude authorities handled by more-specific registrations
            if (uri.Host.EndsWith(Internal.Constants.CiamAuthorityHostSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (uri.AbsolutePath.StartsWith("/adfs", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (AuthorityDetectionHelpers.IsDstsUri(uri))
            {
                return false;
            }

            if (AuthorityDetectionHelpers.IsB2CUri(uri))
            {
                return false;
            }

            return true;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Instance.Registrations;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// Central registry of all supported authority types.
    /// Provides detection (URI → registration) and lookup (type → registration) operations.
    /// </summary>
    /// <remarks>
    /// Registration order is significant for <see cref="Detect"/>: the first matching
    /// <see cref="AuthorityRegistration.DetectionPredicate"/> wins.
    /// Canonical order: DSTS → B2C → ADFS → CIAM → AAD → Generic (catch-all).
    ///
    /// Note: registrations that depend on a <see cref="RequestContext"/> (AAD, ADFS validators)
    /// are created lazily per-request via the <see cref="AuthorityCreationPipeline"/>.
    /// The static registry stores context-free registrations and is used for type detection.
    /// </remarks>
    internal static class AuthorityRegistry
    {
        /// <summary>
        /// Context-free registrations used for type detection only.
        /// Registered in detection-priority order.
        /// </summary>
        private static readonly IReadOnlyList<AuthorityRegistration> s_registrations =
            new List<AuthorityRegistration>
            {
                DstsAuthorityRegistration.Create(),
                B2CAuthorityRegistration.Create(),
                AdfsAuthorityRegistration.Create(),
                CiamAuthorityRegistration.Create(),
                // AAD is evaluated before the Generic catch-all
                AadAuthorityRegistration.Create(),
                GenericAuthorityRegistration.Create(),
            };

        /// <summary>
        /// Returns the first <see cref="AuthorityRegistration"/> whose
        /// <see cref="AuthorityRegistration.DetectionPredicate"/> matches <paramref name="authorityUri"/>.
        /// </summary>
        /// <param name="authorityUri">The authority URI to classify.</param>
        /// <returns>
        /// The matching <see cref="AuthorityRegistration"/>, or <see langword="null"/> if no
        /// registration matches (in practice the Generic catch-all always matches).
        /// </returns>
        internal static AuthorityRegistration Detect(Uri authorityUri)
        {
            if (authorityUri == null)
            {
                throw new ArgumentNullException(nameof(authorityUri));
            }

            return s_registrations.FirstOrDefault(r => r.DetectionPredicate(authorityUri));
        }

        /// <summary>
        /// Returns the <see cref="AuthorityRegistration"/> for the specified <paramref name="authorityType"/>.
        /// </summary>
        /// <param name="authorityType">The authority type to retrieve.</param>
        /// <returns>The matching <see cref="AuthorityRegistration"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no registration is found for the specified type (should not happen in practice).
        /// </exception>
        internal static AuthorityRegistration Get(AuthorityType authorityType)
        {
            var registration = s_registrations.FirstOrDefault(r => r.Type == authorityType);
            if (registration == null)
            {
                throw new InvalidOperationException($"No authority registration found for type '{authorityType}'.");
            }

            return registration;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// Immutable record that holds all per-authority-type metadata used by
    /// <see cref="AuthorityRegistry"/> and <see cref="AuthorityCreationPipeline"/>.
    /// </summary>
    /// <remarks>
    /// See AUTHORITY_REFACTORING_DESIGN.md – "Proposed Architecture" section.
    /// </remarks>
    internal sealed class AuthorityRegistration
    {
        /// <summary>
        /// Initializes a new <see cref="AuthorityRegistration"/>.
        /// </summary>
        /// <param name="type">The authority type this registration describes.</param>
        /// <param name="detectionPredicate">
        /// A predicate evaluated against a candidate URI to determine whether the URI
        /// belongs to this authority type.  Evaluated in registration order; first match wins.
        /// </param>
        /// <param name="factory">Creates the concrete <see cref="Authority"/> from a normalized <see cref="AuthorityInfo"/>.</param>
        /// <param name="validatorFactory">
        /// Factory that creates an <see cref="IAuthorityValidator"/> for a given <see cref="RequestContext"/>.
        /// The factory receives a <see cref="RequestContext"/> which may be <see langword="null"/> when
        /// the registration is used only for type detection (not validation).
        /// </param>
        /// <param name="resolver">Resolves environment/endpoint information (e.g. instance discovery).</param>
        /// <param name="normalizer">Converts a raw URI into a canonical <see cref="AuthorityInfo"/>.</param>
        public AuthorityRegistration(
            AuthorityType type,
            Func<Uri, bool> detectionPredicate,
            Func<AuthorityInfo, Authority> factory,
            Func<RequestContext, IAuthorityValidator> validatorFactory,
            IAuthorityResolver resolver,
            IAuthorityNormalizer normalizer)
        {
            Type = type;
            DetectionPredicate = detectionPredicate ?? throw new ArgumentNullException(nameof(detectionPredicate));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            ValidatorFactory = validatorFactory ?? throw new ArgumentNullException(nameof(validatorFactory));
            Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            Normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
        }

        /// <summary>Gets the authority type described by this registration.</summary>
        public AuthorityType Type { get; }

        /// <summary>
        /// Gets the predicate used to detect whether a URI belongs to this authority type.
        /// Evaluated in registration order by <see cref="AuthorityRegistry.Detect"/>.
        /// </summary>
        public Func<Uri, bool> DetectionPredicate { get; }

        /// <summary>
        /// Gets the factory that creates the concrete <see cref="Authority"/> subclass
        /// from a normalized and merged <see cref="AuthorityInfo"/>.
        /// </summary>
        public Func<AuthorityInfo, Authority> Factory { get; }

        /// <summary>
        /// Gets the factory that creates an <see cref="IAuthorityValidator"/> for a given
        /// <see cref="RequestContext"/>. Some validators (e.g. <see cref="Validation.AadAuthorityValidator"/>
        /// and <see cref="Validation.AdfsAuthorityValidator"/>) require a non-null context to perform
        /// network-based validation.
        /// </summary>
        public Func<RequestContext, IAuthorityValidator> ValidatorFactory { get; }

        /// <summary>
        /// Gets the resolver that performs environment/endpoint resolution after normalization
        /// (e.g. AAD instance discovery for aliasing).
        /// </summary>
        public IAuthorityResolver Resolver { get; }

        /// <summary>
        /// Gets the normalizer that converts a raw URI into a canonical <see cref="AuthorityInfo"/>
        /// for this authority type.
        /// </summary>
        public IAuthorityNormalizer Normalizer { get; }
    }
}

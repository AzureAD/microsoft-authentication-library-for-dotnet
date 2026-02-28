// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// Implements the 6-step pipeline for creating a fully resolved <see cref="Authority"/> from
    /// a raw URI string, optional config-level authority, and optional request-level override.
    /// </summary>
    /// <remarks>
    /// Pipeline steps:
    /// <list type="number">
    ///   <item><b>Parse</b>   – validate and parse the raw URI string.</item>
    ///   <item><b>Detect</b>  – classify the URI using <see cref="AuthorityRegistry.Detect"/>.</item>
    ///   <item><b>Normalize</b> – apply authority-type-specific normalization rules.</item>
    ///   <item><b>Merge</b>   – merge config, request-override, and account information using <see cref="AuthorityMerger"/>.</item>
    ///   <item><b>Validate</b> – perform authority-type-specific validation (e.g. instance discovery).</item>
    ///   <item><b>Construct</b> – instantiate the concrete <see cref="Authority"/> subclass.</item>
    /// </list>
    ///
    /// This class is the foundation for Phase 3 wire-up.  In the current phase (Phase 2) it is
    /// infrastructure only; no existing callers are modified.
    /// </remarks>
    internal sealed class AuthorityCreationPipeline
    {
        private readonly IInstanceDiscoveryManager _instanceDiscoveryManager;
        private readonly RequestContext _requestContext;

        /// <summary>
        /// Initializes a new <see cref="AuthorityCreationPipeline"/>.
        /// </summary>
        /// <param name="instanceDiscoveryManager">
        /// The instance discovery manager used by validators and resolvers.
        /// </param>
        /// <param name="requestContext">
        /// The current request context, providing access to configuration and network.
        /// </param>
        public AuthorityCreationPipeline(
            IInstanceDiscoveryManager instanceDiscoveryManager,
            RequestContext requestContext)
        {
            _instanceDiscoveryManager = instanceDiscoveryManager ?? throw new ArgumentNullException(nameof(instanceDiscoveryManager));
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        }

        /// <summary>
        /// Executes the 6-step pipeline and returns the fully resolved <see cref="Authority"/>.
        /// </summary>
        /// <param name="rawAuthorityUri">
        /// The raw authority URI string to parse and process.  Must be a valid HTTPS URI.
        /// </param>
        /// <param name="configAuthority">
        /// The authority configured at the application level.  Used in the Merge step.
        /// May be <see langword="null"/> when the pipeline is invoked without an application context.
        /// </param>
        /// <param name="requestOverride">
        /// The authority override provided at the request level.  May be <see langword="null"/>.
        /// </param>
        /// <param name="account">
        /// The account for the current request, used for tenant and environment merging.
        /// May be <see langword="null"/>.
        /// </param>
        /// <returns>A fully resolved <see cref="Authority"/> instance.</returns>
        /// <exception cref="MsalClientException">
        /// Thrown when the URI is invalid, the type cannot be detected, or validation fails.
        /// </exception>
        public async Task<Authority> CreateAsync(
            string rawAuthorityUri,
            Authority configAuthority,
            AuthorityInfo requestOverride,
            IAccount account = null)
        {
            // Step 1: Parse
            Uri authorityUri = ParseUri(rawAuthorityUri);

            // Step 2: Detect
            AuthorityRegistration registration = AuthorityRegistry.Detect(authorityUri);
            if (registration == null)
            {
                throw new MsalClientException(
                    MsalError.InvalidAuthorityType,
                    $"Could not detect the authority type for '{rawAuthorityUri}'.");
            }

            // Step 3: Normalize
            AuthorityInfo normalizedInfo = registration.Normalizer.Normalize(authorityUri);

            // Step 4: Merge
            AuthorityInfo mergedInfo;
            if (configAuthority != null)
            {
                var appConfig = _requestContext.ServiceBundle?.Config;
                bool isMsaPassthrough = appConfig?.IsBrokerEnabled == true
                    && appConfig?.BrokerOptions?.MsaPassthrough == true;

                bool multiCloudSupportEnabled = appConfig?.MultiCloudSupportEnabled == true;

                mergedInfo = await AuthorityMerger.MergeAsync(
                    configAuthority,
                    requestOverride,
                    account,
                    isMsaPassthrough,
                    multiCloudSupportEnabled,
                    _requestContext).ConfigureAwait(false);
            }
            else
            {
                mergedInfo = normalizedInfo;
            }

            // Step 5: Validate
            // Create a context-aware validator using the registration's factory
            var validator = registration.ValidatorFactory(_requestContext);
            await validator.ValidateAuthorityAsync(mergedInfo).ConfigureAwait(false);

            // Step 6: Construct
            return registration.Factory(mergedInfo);
        }

        /// <summary>
        /// Validates and parses <paramref name="rawUri"/> into a <see cref="Uri"/>.
        /// </summary>
        private static Uri ParseUri(string rawUri)
        {
            if (string.IsNullOrWhiteSpace(rawUri))
            {
                throw new MsalClientException(
                    MsalError.InvalidAuthority,
                    MsalErrorMessage.AuthorityInvalidUriFormat);
            }

            if (!Uri.TryCreate(rawUri, UriKind.Absolute, out Uri authorityUri))
            {
                throw new MsalClientException(
                    MsalError.InvalidAuthority,
                    MsalErrorMessage.AuthorityInvalidUriFormat);
            }

            if (!string.Equals(authorityUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                throw new MsalClientException(
                    MsalError.InvalidAuthority,
                    MsalErrorMessage.AuthorityUriInsecure);
            }

            return authorityUri;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Orchestrator responsible for:
    /// - Invoking IClientCredential.GetCredentialMaterialAsync() exactly once per logical token request
    /// - Validating the returned material (ensuring cert exists if mTLS required)
    /// - Enforcing constraints (e.g., AAD + mTLS PoP requires region)
    /// - Returning validated material for use in the request pipeline
    /// </summary>
    internal sealed class CredentialMaterialOrchestrator
    {
        private readonly IClientCredential _credential;
        private readonly ILoggerAdapter _logger;

        public CredentialMaterialOrchestrator(IClientCredential credential, ILoggerAdapter logger)
        {
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Resolves and validates credential material for the given request context.
        /// This method should be invoked exactly once per logical token request.
        /// </summary>
        /// <param name="requestContext">The credential request context containing input parameters</param>
        /// <param name="mtlsContext">The mTLS validation context containing authority and region info</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validated credential material ready for use</returns>
        /// <exception cref="MsalClientException">Thrown if validation fails</exception>
        public async Task<CredentialMaterial> GetValidatedMaterialAsync(
            CredentialRequestContext requestContext,
            MtlsValidationContext mtlsContext,
            CancellationToken cancellationToken)
        {
            _logger.Verbose(() => "[CredentialMaterialOrchestrator] Resolving credential material.");

            // Invoke the credential provider exactly once
            CredentialMaterial material = await _credential.GetCredentialMaterialAsync(
                requestContext,
                cancellationToken).ConfigureAwait(false);

            // Validate material is not null
            if (material is null)
            {
                throw new MsalClientException(
                    MsalError.InternalError,
                    "Credential provider returned null material.");
            }

            // Validate that cert exists if mTLS is required
            if (requestContext.MtlsRequired && material.MtlsCertificate is null)
            {
                _logger.Error("[CredentialMaterialOrchestrator] mTLS is required but no certificate was provided by the credential.");
                throw new MsalClientException(
                    MsalError.MtlsCertificateNotProvided,
                    "mTLS Proof of Possession (mTLS PoP) is configured but a certificate was not provided by the credential. " +
                    "Ensure that a valid certificate is provided when using mTLS PoP.");
            }

            // Validate that AAD + mTLS PoP requires a region
            if (mtlsContext.AuthorityType == AuthorityType.Aad &&
                requestContext.MtlsRequired &&
                string.IsNullOrEmpty(mtlsContext.AzureRegion))
            {
                _logger.Error("[CredentialMaterialOrchestrator] mTLS PoP with AAD requires a region to be configured.");
                throw new MsalClientException(
                    MsalError.RegionRequiredForMtlsPop,
                    "mTLS Proof of Possession (mTLS PoP) requires a specific Azure region to be specified when using AAD authority. " +
                    "Ensure that the AzureRegion configuration is set.");
            }

            _logger.Verbose(() => "[CredentialMaterialOrchestrator] Credential material resolved and validated successfully.");
            return material;
        }
    }
}

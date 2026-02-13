// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Handles client assertions supplied via a delegate that returns an
    /// <see cref="ClientSignedAssertion"/> (JWT + optional certificate bound for mTLS‑PoP).
    /// </summary>
    internal sealed class ClientAssertionDelegateCredential : IClientCredential, IClientSignedAssertionProvider
    {
        private readonly Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> _provider;

        internal ClientAssertionDelegateCredential(
            Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        // Private helper for internal readability
        private Task<ClientSignedAssertion> GetAssertionAsync(
            AssertionRequestOptions options,
            CancellationToken cancellationToken) =>
            _provider(options, cancellationToken);

        // Capability interface (only used where we intentionally cast to check the capability)
        Task<ClientSignedAssertion> IClientSignedAssertionProvider.GetAssertionAsync(
            AssertionRequestOptions options,
            CancellationToken cancellationToken) =>
            GetAssertionAsync(options, cancellationToken);

        public AssertionType AssertionType => AssertionType.ClientAssertion;

        public async Task<CredentialMaterial> GetCredentialMaterialAsync(
            CredentialContext context,
            CancellationToken cancellationToken)
        {
            var opts = new AssertionRequestOptions
            {
                CancellationToken = cancellationToken,
                ClientID = context.ClientId,
                TokenEndpoint = context.TokenEndpoint,
                ClientCapabilities = context.ClientCapabilities,
                Claims = context.Claims,
                ClientAssertionFmiPath = context.ClientAssertionFmiPath
            };

            ClientSignedAssertion resp = await GetAssertionAsync(opts, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(resp?.Assertion))
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            bool hasCertificate = resp.TokenBindingCertificate != null;

            // Per canonical matrix: enforce supported combinations
            if (context.Mode == ClientAuthMode.Regular && hasCertificate)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    "Client assertion with TokenBindingCertificate (jwt+cert) is only supported in mTLS mode. Use .WithMtlsProofOfPossession() or don't return a certificate in your callback.");
            }

            if (context.Mode == ClientAuthMode.MtlsMode && !hasCertificate)
            {
                throw new MsalClientException(
                    MsalError.MtlsCertificateNotProvided,
                    "mTLS mode requires TokenBindingCertificate in ClientSignedAssertion. Your callback must return a certificate.");
            }

            // Use jwt-pop if TokenBindingCertificate is present (assertion contains confirmation claim)
            // AAD requires jwt-pop when confirmation claim exists
            string assertionType = hasCertificate
                ? OAuth2AssertionType.JwtPop
                : OAuth2AssertionType.JwtBearer;

            var tokenParameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, assertionType },
                { OAuth2Parameter.ClientAssertion, resp.Assertion }
            };

            return new CredentialMaterial(
                tokenRequestParameters: tokenParameters,
                source: CredentialSource.Callback,
                resolvedCertificate: resp.TokenBindingCertificate);
        }
    }
}

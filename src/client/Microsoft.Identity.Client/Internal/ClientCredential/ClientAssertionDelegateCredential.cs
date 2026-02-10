// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            CredentialRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            var opts = new AssertionRequestOptions
            {
                CancellationToken = cancellationToken,
                ClientID = requestContext.ClientId,
                TokenEndpoint = requestContext.TokenEndpoint,
                ClientCapabilities = requestContext.ClientCapabilities,
                Claims = requestContext.Claims,
                ClientAssertionFmiPath = requestContext.ClientAssertionFmiPath
            };

            ClientSignedAssertion resp = await GetAssertionAsync(opts, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(resp?.Assertion))
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            sw.Stop();

            // JWT-PoP ONLY if explicitly requested, not inferred from cert presence
            string assertionType = requestContext.MtlsRequired && resp.TokenBindingCertificate != null
                ? OAuth2AssertionType.JwtPop
                : OAuth2AssertionType.JwtBearer;

            var tokenParameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, assertionType },
                { OAuth2Parameter.ClientAssertion, resp.Assertion }
            };

            return new CredentialMaterial(
                tokenRequestParameters: tokenParameters,
                mtlsCertificate: resp.TokenBindingCertificate,
                metadata: new CredentialMaterialMetadata(
                    credentialType: CredentialType.ClientAssertion,
                    credentialSource: "callback",
                    mtlsCertificateIdHashPrefix: resp.TokenBindingCertificate != null
                        ? CredentialMaterialHelper.GetCertificateIdHashPrefix(resp.TokenBindingCertificate)
                        : null,
                    mtlsCertificateRequested: requestContext.MtlsRequired,
                    resolutionTimeMs: sw.ElapsedMilliseconds));
        }
    }
}

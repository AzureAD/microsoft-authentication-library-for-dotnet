// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Handles client assertions supplied via a delegate that returns a
    /// <see cref="ClientSignedAssertion"/> (JWT + optional certificate bound for mTLS-PoP).
    /// </summary>
    internal sealed class ClientAssertionDelegateCredential : IClientCredential, IClientSignedAssertionProvider
    {
        private readonly Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> _provider;

        internal ClientAssertionDelegateCredential(
            Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        // Capability interface (only used where we intentionally cast to check the capability)
        Task<ClientSignedAssertion> IClientSignedAssertionProvider.GetAssertionAsync(
            AssertionRequestOptions options,
            CancellationToken cancellationToken) =>
            _provider(options, cancellationToken);

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

            ClientSignedAssertion resp = await _provider(opts, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(resp?.Assertion))
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            bool hasCert = resp.TokenBindingCertificate != null;

            if (context.Mode == ClientAuthMode.MtlsMode && !hasCert)
            {
                throw new MsalClientException(
                    MsalError.MtlsCertificateNotProvided,
                    MsalErrorMessage.MtlsCertificateNotProvidedMessage);
            }

            // Use JWT-PoP when in MtlsMode or when the callback returned a certificate (implicit bearer-over-mTLS).
            bool useJwtPop = context.Mode == ClientAuthMode.MtlsMode || hasCert;

            var parameters = new Dictionary<string, string>
            {
                {
                    OAuth2Parameter.ClientAssertionType,
                    useJwtPop ? OAuth2AssertionType.JwtPop : OAuth2AssertionType.JwtBearer
                },
                { OAuth2Parameter.ClientAssertion, resp.Assertion }
            };

            return new CredentialMaterial(
                parameters,
                CredentialSource.Callback,
                hasCert ? resp.TokenBindingCertificate : null);
        }
    }
}


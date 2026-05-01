// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Handles client assertions supplied via a delegate that returns a
    /// <see cref="ClientSignedAssertion"/> (JWT + optional certificate bound for mTLS-PoP).
    /// </summary>
    internal sealed class ClientAssertionDelegateCredential : IClientCredential, IMtlsBindingCertificateProvider
    {
        private readonly Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> _provider;

        internal ClientAssertionDelegateCredential(
            Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public AssertionType AssertionType => AssertionType.ClientAssertion;

        /// <summary>
        /// Returns the binding certificate for mTLS transport setup.
        /// Invokes the delegate to discover the certificate; the assertion itself is discarded
        /// because it must be regenerated at send time with the correct resolved token endpoint.
        /// </summary>
        async Task<X509Certificate2> IMtlsBindingCertificateProvider.GetBindingCertificateAsync(
            AssertionRequestOptions options,
            CancellationToken cancellationToken)
        {
            ClientSignedAssertion result = await _provider(options, cancellationToken).ConfigureAwait(false);
            return result?.TokenBindingCertificate;
        }

        public async Task<CredentialMaterial> GetCredentialMaterialAsync(
            CredentialContext context,
            CancellationToken cancellationToken)
        {
            context.Logger.Verbose(() => $"[ClientAssertionDelegateCredential] Mode={context.Mode}");

            var opts = context.ToAssertionRequestOptions(cancellationToken);

            // Always call the delegate fresh — the assertion must be generated with
            // the correct resolved token endpoint (audience claim).
            ClientSignedAssertion resp = await _provider(opts, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(resp?.Assertion))
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            bool hasCert = resp.TokenBindingCertificate != null;

            if (context.Mode == CredentialTransportProtocol.Mtls && !hasCert)
            {
                throw new MsalClientException(
                    MsalError.MtlsCertificateNotProvided,
                    MsalErrorMessage.MtlsCertificateNotProvidedMessage);
            }

            // Select the appropriate assertion type based on the presence of a certificate and the OAuth mode.
            string assertionType =
                (context.Mode == CredentialTransportProtocol.Mtls || hasCert)
                    ? OAuth2AssertionType.JwtPop
                    : OAuth2AssertionType.JwtBearer;

            var parameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, assertionType },
                { OAuth2Parameter.ClientAssertion, resp.Assertion }
            };

            return new CredentialMaterial(
                parameters,
                hasCert ? resp.TokenBindingCertificate : null);
        }
    }
}

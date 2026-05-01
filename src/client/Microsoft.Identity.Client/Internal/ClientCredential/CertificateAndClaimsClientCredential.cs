// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal class CertificateAndClaimsClientCredential : IClientCredential
    {
        private readonly IDictionary<string, string> _claimsToSign;
        private readonly bool _appendDefaultClaims = true;
        private readonly Func<AssertionRequestOptions, Task<X509Certificate2>> _certificateProvider;

        public AssertionType AssertionType => AssertionType.CertificateWithoutSni;

        /// <summary>
        /// The static certificate if one was provided directly; otherwise null.
        /// This is used for backward compatibility with the Certificate property on ConfidentialClientApplication.
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Constructor that accepts a certificate provider delegate.
        /// This allows both static certificates (via a simple delegate) and dynamic certificate resolution.
        /// </summary>
        /// <param name="certificateProvider">Async delegate that provides the certificate</param>
        /// <param name="claimsToSign">Additional claims to include in the client assertion</param>
        /// <param name="appendDefaultClaims">Whether to append default claims</param>
        /// <param name="certificate">Optional static certificate for backward compatibility</param>
        public CertificateAndClaimsClientCredential(
            Func<AssertionRequestOptions, Task<X509Certificate2>> certificateProvider,
            IDictionary<string, string> claimsToSign,
            bool appendDefaultClaims,
            X509Certificate2 certificate = null)
        {
            _certificateProvider = certificateProvider;
            _claimsToSign = claimsToSign;
            _appendDefaultClaims = appendDefaultClaims;
            Certificate = certificate;
        }

        public async Task<CredentialMaterial> GetCredentialMaterialAsync(
            CredentialContext context,
            CancellationToken cancellationToken)
        {
            context.Logger.Verbose(() => $"[CertificateAndClaimsClientCredential] Mode={context.Mode}");

            // Resolve the certificate via the provider (used both for Regular and MtlsMode paths).
            X509Certificate2 certificate = await ResolveCertificateAsync(context, cancellationToken)
                .ConfigureAwait(false);

            if (context.Mode == CredentialTransportProtocol.Mtls)
            {
                // mTLS path: the certificate authenticates the client at the TLS layer.
                // No client_assertion is needed; return an empty parameter set.
                return new CredentialMaterial(
                    CollectionHelpers.GetEmptyDictionary<string, string>(),
                    certificate);
            }

            // Regular path: build a JWT-bearer client assertion.
            JsonWebToken jwtToken;
            if (string.IsNullOrEmpty(context.ExtraClientAssertionClaims))
            {
                jwtToken = new JsonWebToken(
                    context.CryptographyManager,
                    context.ClientId,
                    context.TokenEndpoint,
                    _claimsToSign,
                    _appendDefaultClaims);
            }
            else
            {
                jwtToken = new JsonWebToken(
                    context.CryptographyManager,
                    context.ClientId,
                    context.TokenEndpoint,
                    context.ExtraClientAssertionClaims,
                    _appendDefaultClaims);
            }

            string assertion = jwtToken.Sign(certificate, context.SendX5C, context.UseSha2);

            var parameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                { OAuth2Parameter.ClientAssertion, assertion }
            };

            return new CredentialMaterial(parameters, certificate);
        }

        /// <summary>
        /// Resolves the certificate for use as an mTLS transport credential, without building a full
        /// JWT client assertion. Invokes the provider delegate (which may be a static lambda or a
        /// true async callback) and validates the result.
        /// Called by <see cref="Microsoft.Identity.Client.ApiConfig.Parameters.MtlsPopParametersInitializer"/>
        /// for the implicit Bearer-over-mTLS path when
        /// <see cref="AppConfig.CertificateOptions.SendCertificateOverMtls"/> is <see langword="true"/>.
        /// </summary>
        internal async Task<X509Certificate2> ResolveCertificateForMtlsAsync(
            AssertionRequestOptions options)
        {
            X509Certificate2 certificate = await _certificateProvider(options).ConfigureAwait(false);

            ValidateCertificate(certificate);

            return certificate;
        }

        /// <summary>
        /// Resolves the certificate to use for signing the client assertion.
        /// Invokes the certificate provider delegate to get the certificate.
        /// </summary>
        /// <param name="context">Immutable context describing the current request.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The X509Certificate2 to use for signing.</returns>
        /// <exception cref="MsalClientException">
        /// Thrown if the certificate provider returns null or a certificate without a private key.
        /// </exception>
        private async Task<X509Certificate2> ResolveCertificateAsync(
            CredentialContext context,
            CancellationToken cancellationToken)
        {
            context.Logger.Verbose(
                () => "[CertificateAndClaimsClientCredential] Resolving certificate from provider.");

            // Create AssertionRequestOptions from the credential context for the callback
            var options = context.ToAssertionRequestOptions(cancellationToken);

            // Invoke the provider to get the certificate
            X509Certificate2 certificate = await _certificateProvider(options).ConfigureAwait(false);

            ValidateCertificate(certificate);

            context.Logger.Verbose(
                () => $"[CertificateAndClaimsClientCredential] Certificate resolved. " +
                      $"Thumbprint: {certificate.Thumbprint}");

            return certificate;
        }

        /// <summary>
        /// Validates that the certificate is non-null and has a private key.
        /// </summary>
        private static void ValidateCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    "The certificate provider callback returned null. Ensure the callback returns a valid X509Certificate2 instance.");
            }

            try
            {
                if (!certificate.HasPrivateKey)
                {
                    throw new MsalClientException(
                        MsalError.CertWithoutPrivateKey,
                        MsalErrorMessage.CertMustHavePrivateKey(certificate.FriendlyName));
                }
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                throw new MsalClientException(
                    MsalError.CryptographicError,
                    MsalErrorMessage.CryptographicError,
                    ex);
            }
        }
    }
}

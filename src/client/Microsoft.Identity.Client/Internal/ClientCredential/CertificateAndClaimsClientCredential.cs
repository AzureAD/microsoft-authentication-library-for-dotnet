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

            // Cert reuse (single-invocation per request, issue #5943) is handled in
            // CredentialMaterialResolver.ResolveAsync, which short-circuits this method when
            // requestParams.MtlsCertificate is set (i.e., the mTLS PoP preflight already
            // resolved a binding cert). Subclasses must not rely on this method being invoked
            // in mTLS mode and must keep mTLS-mode output equal to (empty params, cert) —
            // any future subclass that needs different mTLS-mode behaviour (e.g. additional
            // token-request headers) must update the resolver short-circuit, not just override
            // here, or those additions will be silently dropped at runtime.
            X509Certificate2 certificate = await ResolveCertificateAsync(context, cancellationToken)
                .ConfigureAwait(false);

            if (context.Mode == CredentialTransportProtocol.Mtls)
            {
                // Custom client-claims credentials (WithClientClaims) are JWT-bearer only:
                // their cert is intended to sign the assertion, not to bind the TLS transport.
                // This guard fires for every mTLS-mode resolution path — explicit PoP
                // (.WithMtlsProofOfPossession()) AND implicit Bearer-over-mTLS
                // (CertificateOptions.SendCertificateOverMtls = true) — so the message must
                // cover both transports rather than naming Proof-of-Possession alone.
                // Subclasses (CertificateClientCredential, DynamicCertificateClientCredential)
                // construct the base with claimsToSign: null and are unaffected by this guard.
                if (_claimsToSign is not null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsNotSupportedWithClientClaimsMessage);
                }

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
        /// Resolves the certificate to use for signing the client assertion or binding the TLS
        /// transport. Invokes the certificate provider delegate and validates the result.
        /// </summary>
        /// <param name="context">Immutable context describing the current request.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The X509Certificate2 to use for signing or transport binding.</returns>
        /// <exception cref="MsalClientException">
        /// Thrown if the certificate provider returns null or a certificate without a private key.
        /// In <see cref="CredentialTransportProtocol.Mtls"/> mode, a null certificate is reported as
        /// <see cref="MsalError.MtlsCertificateNotProvided"/>; otherwise as
        /// <see cref="MsalError.InvalidClientAssertion"/>.
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

            // In mTLS mode, surface a null cert as MtlsCertificateNotProvided to match the public
            // mTLS PoP API contract. In Regular (JWT-bearer) mode, keep InvalidClientAssertion.
            if (context.Mode == CredentialTransportProtocol.Mtls)
            {
                ValidateCertificate(
                    certificate,
                    MsalError.MtlsCertificateNotProvided,
                    MsalErrorMessage.MtlsCertificateNotProvidedMessage);
            }
            else
            {
                ValidateCertificate(certificate);
            }

            context.Logger.Verbose(
                () => $"[CertificateAndClaimsClientCredential] Certificate resolved. " +
                      $"Thumbprint: {certificate.Thumbprint}");

            return certificate;
        }

        /// <summary>
        /// Validates that the certificate is non-null and has an accessible private key.
        /// </summary>
        /// <param name="certificate">The certificate returned by the provider.</param>
        /// <param name="nullErrorCode">
        /// The <see cref="MsalError"/> code to throw when the certificate is null. Defaults to
        /// <see cref="MsalError.InvalidClientAssertion"/> for the credential-material path. The
        /// preflight path passes <see cref="MsalError.MtlsCertificateNotProvided"/> instead.
        /// </param>
        /// <param name="nullErrorMessage">
        /// Optional message to use when the certificate is null. If omitted, a generic
        /// provider-returned-null message is used.
        /// </param>
        private static void ValidateCertificate(
            X509Certificate2 certificate,
            string nullErrorCode = MsalError.InvalidClientAssertion,
            string nullErrorMessage = null)
        {
            if (certificate == null)
            {
                throw new MsalClientException(
                    nullErrorCode,
                    nullErrorMessage ?? "The certificate provider callback returned null. Ensure the callback returns a valid X509Certificate2 instance.");
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

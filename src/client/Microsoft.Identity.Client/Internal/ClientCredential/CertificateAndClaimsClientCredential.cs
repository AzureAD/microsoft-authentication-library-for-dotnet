// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal class CertificateAndClaimsClientCredential : IClientCredential, IClientCertificateProvider
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

        public async Task AddConfidentialClientParametersAsync(
            OAuth2Client oAuth2Client,
            AuthenticationRequestParameters requestParameters, 
            ICryptographyManager cryptographyManager, 
            string tokenEndpoint,
            CancellationToken cancellationToken)
        {
            string clientId = requestParameters.AppConfig.ClientId;

            // Log the incoming request parameters for diagnostic purposes
            requestParameters.RequestContext.Logger.Verbose(() => $"Building assertion from certificate with clientId: {clientId} at endpoint: {tokenEndpoint}");

            // Resolve certificate if not already resolved by orchestrator
            // Orchestrator sets CertificateContext when MTLS is requested, but this method is also called
            // in non-MTLS scenarios where cert needs to be resolved for JWT signing
            if (requestParameters.CertificateContext == null)
            {
                requestParameters.RequestContext.Logger.Verbose(() => "Certificate not yet resolved. Resolving from provider for JWT signing.");

                // Build options for certificate resolution
                var options = new AssertionRequestOptions(
                    requestParameters.AppConfig, 
                    tokenEndpoint,
                    requestParameters.AuthorityManager.Authority.TenantId)
                {
                    Claims = requestParameters.Claims,
                    ClientCapabilities = requestParameters.AppConfig.ClientCapabilities,
                    IsMtlsPopRequested = false,  // Non-MTLS path - cert used for JWT signing
                    CancellationToken = cancellationToken
                };

                // Resolve certificate context via interface method
                ClientCertificateContext certContext = await GetCertificateAsync(options, cancellationToken).ConfigureAwait(false);

                // Validate the resolved certificate (GetCertificateAsync returns null if provider returns null)
                ValidateCertificate(certContext?.Certificate, requestParameters.RequestContext.Logger);

                requestParameters.CertificateContext = certContext;
            }

            // Check if MTLS PoP is being used (cert already in context with MTLS usage)
            bool isMtlsPoP = requestParameters.CertificateContext.Usage == ClientCertificateUsage.MtlsBinding;

            if (!isMtlsPoP)
            {
                requestParameters.RequestContext.Logger.Verbose(() => "Proceeding with JWT token creation and adding client assertion.");

                X509Certificate2 certificate = requestParameters.CertificateContext.Certificate;
                bool useSha2 = requestParameters.AuthorityManager.Authority.AuthorityInfo.IsSha2CredentialSupported;

                JsonWebToken jwtToken;
                if (string.IsNullOrEmpty(requestParameters.ExtraClientAssertionClaims))
                {
                    jwtToken = new JsonWebToken(
                        cryptographyManager,
                        clientId,
                        tokenEndpoint,
                        _claimsToSign,
                        _appendDefaultClaims);
                }
                else
                {
                    jwtToken = new JsonWebToken(
                        cryptographyManager,
                        clientId,
                        tokenEndpoint,
                        requestParameters.ExtraClientAssertionClaims,
                        _appendDefaultClaims);
                }

                string assertion = jwtToken.Sign(certificate, requestParameters.SendX5C, useSha2);

                oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
                oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, assertion);
            }
            else
            {
                // Log that MTLS PoP is required and JWT token creation is skipped
                requestParameters.RequestContext.Logger.Verbose(() => "MTLS PoP Client credential request. Skipping client assertion.");
            }
        }

        /// <summary>
        /// Validates that a certificate is suitable for use in authentication.
        /// </summary>
        /// <param name="certificate">The certificate to validate</param>
        /// <param name="logger">Logger for diagnostic messages</param>
        /// <exception cref="MsalClientException">Thrown if certificate is null or invalid</exception>
        private static void ValidateCertificate(X509Certificate2 certificate, ILoggerAdapter logger)
        {
            // Validate the certificate returned by the provider
            if (certificate == null)
            {
                logger.Error("[CertificateAndClaimsClientCredential] Certificate provider returned null.");
 
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    "The certificate provider callback returned null. Ensure the callback returns a valid X509Certificate2 instance.");
            }

            try
            {
                if (!certificate.HasPrivateKey)
                {
                    logger.Error("[CertificateAndClaimsClientCredential] Certificate does not have a private key.");
     
                    throw new MsalClientException(
                        MsalError.CertWithoutPrivateKey,
                        MsalErrorMessage.CertMustHavePrivateKey(certificate.FriendlyName));
                }
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                logger.Error("[CertificateAndClaimsClientCredential] A cryptographic error occurred while accessing the certificate.");
 
                throw new MsalClientException(
                    MsalError.CryptographicError,
                    MsalErrorMessage.CryptographicError,
                    ex);
            }

            logger.Info(() => $"[CertificateAndClaimsClientCredential] Successfully validated certificate. Thumbprint: {certificate.Thumbprint}");
        }

        public async Task<ClientCertificateContext> GetCertificateAsync(
            AssertionRequestOptions options,
            CancellationToken cancellationToken)
        {
            // Resolve the certificate via the provider delegate
            X509Certificate2 certificate = await _certificateProvider(options).ConfigureAwait(false);

            if (certificate == null)
            {
                return null;
            }

            // Determine usage based on whether MTLS PoP is requested
            // Same certificate can be used for either JWT signing OR MTLS binding
            var usage = options.IsMtlsPopRequested
                ? ClientCertificateUsage.MtlsBinding
                : ClientCertificateUsage.Assertion;

            return new ClientCertificateContext
            {
                Certificate = certificate,
                Usage = usage
            };
        }
    }
}

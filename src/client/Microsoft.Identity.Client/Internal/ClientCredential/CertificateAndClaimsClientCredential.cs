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
    internal class CertificateAndClaimsClientCredential : IClientCredential
    {
        private readonly IDictionary<string, string> _claimsToSign;
        private readonly bool _appendDefaultClaims;
        private readonly Func<AssertionRequestOptions, Task<X509Certificate2>> _certificateProvider;

        public AssertionType AssertionType => AssertionType.CertificateWithoutSni;

        /// <summary>
        /// Constructor that accepts a certificate provider delegate.
        /// This allows both static certificates (via a simple delegate) and dynamic certificate resolution.
        /// </summary>
        /// <param name="certificateProvider">Async delegate that provides the certificate</param>
        /// <param name="claimsToSign">Additional claims to include in the client assertion</param>
        /// <param name="appendDefaultClaims">Whether to append default claims</param>
        public CertificateAndClaimsClientCredential(
            Func<AssertionRequestOptions, Task<X509Certificate2>> certificateProvider,
            IDictionary<string, string> claimsToSign, 
            bool appendDefaultClaims)
        {
            _certificateProvider = certificateProvider;
            _claimsToSign = claimsToSign;
            _appendDefaultClaims = appendDefaultClaims;
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

            if (requestParameters.MtlsCertificate == null)
            {
                requestParameters.RequestContext.Logger.Verbose(() => "Proceeding with JWT token creation and adding client assertion.");

                // Resolve the certificate via the provider
                X509Certificate2 certificate = await ResolveCertificateAsync(requestParameters, tokenEndpoint, cancellationToken).ConfigureAwait(false);

                // Store the resolved certificate in request parameters for later use (e.g., ExecutionResult)
                requestParameters.ResolvedCertificate = certificate;

                bool useSha2 = requestParameters.AuthorityManager.Authority.AuthorityInfo.IsSha2CredentialSupported;

                var jwtToken = new JsonWebToken(
                cryptographyManager,
                clientId,
                tokenEndpoint,
                _claimsToSign,
                _appendDefaultClaims);

                string assertion = jwtToken.Sign(certificate, requestParameters.SendX5C, useSha2);

                oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
                oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, assertion);
            }
            else
            {
                // Log that MTLS PoP is required and JWT token creation is skipped
                requestParameters.RequestContext.Logger.Verbose(() => "MTLS PoP Client credential request. Skipping client assertion.");
                
                // Store the mTLS certificate in request parameters for later use (e.g., ExecutionResult)
                requestParameters.ResolvedCertificate = requestParameters.MtlsCertificate;
            }
        }

        /// <summary>
        /// Resolves the certificate to use for signing the client assertion.
        /// Invokes the certificate provider delegate to get the certificate.
        /// </summary>
        /// <param name="requestParameters">The authentication request parameters containing app config</param>
        /// <param name="tokenEndpoint">The token endpoint URL</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>The X509Certificate2 to use for signing</returns>
        /// <exception cref="MsalClientException">Thrown if the certificate provider returns null or an invalid certificate</exception>
        private async Task<X509Certificate2> ResolveCertificateAsync(
            AuthenticationRequestParameters requestParameters,
            string tokenEndpoint,
            CancellationToken cancellationToken)
        {
            requestParameters.RequestContext.Logger.Verbose(
                () => "[CertificateAndClaimsClientCredential] Resolving certificate from provider.");

            // Create AssertionRequestOptions for the callback
            var options = new AssertionRequestOptions(
                requestParameters.AppConfig, 
                tokenEndpoint,
                requestParameters.AuthorityManager.Authority.TenantId)
            {
                Claims = requestParameters.Claims,
                ClientCapabilities = requestParameters.AppConfig.ClientCapabilities,
                CancellationToken = cancellationToken
            };

            // Invoke the provider to get the certificate
            X509Certificate2 certificate = await _certificateProvider(options).ConfigureAwait(false);

            // Validate the certificate returned by the provider
            if (certificate == null)
            {
                requestParameters.RequestContext.Logger.Error(
                    "[CertificateAndClaimsClientCredential] Certificate provider returned null.");
 
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    "The certificate provider callback returned null. Ensure the callback returns a valid X509Certificate2 instance.");
            }

            try
            {
                if (!certificate.HasPrivateKey)
                {
                    requestParameters.RequestContext.Logger.Error(
                        "[CertificateAndClaimsClientCredential] Certificate from provider does not have a private key.");
     
                    throw new MsalClientException(
                        MsalError.CertWithoutPrivateKey,
                        MsalErrorMessage.CertMustHavePrivateKey(certificate.FriendlyName));
                }
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                requestParameters.RequestContext.Logger.Error(
                    "[CertificateAndClaimsClientCredential] A cryptographic error occurred while accessing the certificate.");
 
                throw new MsalClientException(
                    MsalError.CryptographicError,
                    MsalErrorMessage.CryptographicError,
                    ex);
            }

            requestParameters.RequestContext.Logger.Info(
                () => $"[CertificateAndClaimsClientCredential] Successfully resolved certificate from provider. " +
                      $"Thumbprint: {certificate.Thumbprint}");

            return certificate;
        }
    }
}

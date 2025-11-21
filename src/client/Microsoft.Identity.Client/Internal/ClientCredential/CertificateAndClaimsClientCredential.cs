// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal class CertificateAndClaimsClientCredential : IClientCredential
    {
        private readonly IDictionary<string, string> _claimsToSign;
        private readonly bool _appendDefaultClaims;
        private readonly string _base64EncodedThumbprint; // x5t

        public X509Certificate2 Certificate { get; }

        public AssertionType AssertionType => AssertionType.CertificateWithoutSni;

        public CertificateAndClaimsClientCredential(
            X509Certificate2 certificate,
            IDictionary<string, string> claimsToSign, 
            bool appendDefaultClaims)
        {
            Certificate = certificate;
            _claimsToSign = claimsToSign;
            _appendDefaultClaims = appendDefaultClaims;
 
            // Certificate can be null when using dynamic certificate provider
            if (certificate != null)
            {
                _base64EncodedThumbprint = Base64UrlHelpers.Encode(certificate.GetCertHash());
            }
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

                // Resolve the certificate - either from static config or dynamic provider
                X509Certificate2 effectiveCertificate = await ResolveCertificateAsync(requestParameters, cancellationToken).ConfigureAwait(false);

                bool useSha2 = requestParameters.AuthorityManager.Authority.AuthorityInfo.IsSha2CredentialSupported;

                var jwtToken = new JsonWebToken(
                cryptographyManager,
                clientId,
                tokenEndpoint,
                _claimsToSign,
                _appendDefaultClaims);

                string assertion = jwtToken.Sign(effectiveCertificate, requestParameters.SendX5C, useSha2);

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
        /// Resolves the certificate to use for signing the client assertion.
        /// If a dynamic certificate provider is configured, it will be invoked to get the certificate.
        /// Otherwise, the static certificate configured at build time is used.
        /// </summary>
        /// <param name="requestParameters">The authentication request parameters containing app config</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>The X509Certificate2 to use for signing</returns>
        /// <exception cref="MsalClientException">Thrown if the certificate provider returns null or an invalid certificate</exception>
        private async Task<X509Certificate2> ResolveCertificateAsync(
            AuthenticationRequestParameters requestParameters,
            CancellationToken cancellationToken)
        {
            // Check if dynamic certificate provider is configured
            if (requestParameters.AppConfig.ClientCredentialCertificateProvider != null)
            {
                requestParameters.RequestContext.Logger.Verbose(
                    () => "[CertificateAndClaimsClientCredential] Resolving certificate from dynamic provider.");

                // Create parameters for the callback
                var parameters = new Extensibility.ClientCredentialExtensionParameters(
                    requestParameters.AppConfig);

                // Invoke the provider to get the certificate
                X509Certificate2 providedCertificate = await requestParameters.AppConfig
                    .ClientCredentialCertificateProvider(parameters)
                    .ConfigureAwait(false);

                // Validate the certificate returned by the provider
                if (providedCertificate == null)
                {
                    requestParameters.RequestContext.Logger.Error(
                        "[CertificateAndClaimsClientCredential] Certificate provider returned null.");
     
                    throw new MsalClientException(
                        MsalError.InvalidClientAssertion,
                        "The certificate provider callback returned null. Ensure the callback returns a valid X509Certificate2 instance.");
                }

                if (!providedCertificate.HasPrivateKey)
                {
                    requestParameters.RequestContext.Logger.Error(
                        "[CertificateAndClaimsClientCredential] Certificate from provider does not have a private key.");
         
                    throw new MsalClientException(
                        MsalError.CertWithoutPrivateKey,
                        "The certificate returned by the provider does not have a private key. " +
                        "Ensure the certificate has a private key for signing operations.");
                }

                requestParameters.RequestContext.Logger.Info(
                    () => $"[CertificateAndClaimsClientCredential] Successfully resolved certificate from provider. " +
                          $"Thumbprint: {providedCertificate.Thumbprint}");

                return providedCertificate;
            }

            // Use the static certificate configured at build time
            if (Certificate == null)
            {
                requestParameters.RequestContext.Logger.Error(
                    "[CertificateAndClaimsClientCredential] No certificate configured (static or dynamic).");
      
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    "No certificate is configured. Use WithCertificate() to provide a certificate.");
            }

            requestParameters.RequestContext.Logger.Verbose(
                () => $"[CertificateAndClaimsClientCredential] Using static certificate. Thumbprint: {Certificate.Thumbprint}");

            return Certificate;
        }
    }
}

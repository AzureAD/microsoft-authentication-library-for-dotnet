// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
        private readonly bool _appendDefaultClaims = true;
        private readonly Func<AssertionRequestOptions, Task<X509Certificate2>> _certificateProvider;

        public AssertionType AssertionType => AssertionType.CertificateWithoutSni;

        /// <summary>
        /// The static certificate if one was provided directly; otherwise null.
        /// This is used for backward compatibility with the Certificate property on ConfidentialClientApplication.
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Gets credential material (auth parameters + optional mTLS certificate).
        /// Certificate credential ALWAYS produces client_assertion + client_assertion_type (for OAuth2 auth).
        /// Additionally returns the certificate for TLS binding when available and appropriate.
        /// 
        /// NOTE: Phase 1 implementation uses adapter pattern - delegates to existing logic
        /// until full refactoring is complete in later phases.
        /// </summary>
        public async Task<CredentialMaterial> GetCredentialMaterialAsync(
            CredentialRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Resolve certificate via provider
            var options = new AssertionRequestOptions
            {
                ClientID = requestContext.ClientId,
                TokenEndpoint = requestContext.TokenEndpoint,
                Claims = requestContext.Claims,
                ClientCapabilities = requestContext.ClientCapabilities,
                CancellationToken = cancellationToken
            };

            X509Certificate2 certificate = await _certificateProvider(options).ConfigureAwait(false);

            if (certificate == null)
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    "The certificate provider callback returned null. Ensure the callback returns a valid X509Certificate2 instance.");
            }

            if (!certificate.HasPrivateKey)
            {
                throw new MsalClientException(
                    MsalError.CertWithoutPrivateKey,
                    MsalErrorMessage.CertMustHavePrivateKey(certificate.FriendlyName));
            }

            // For Phase 1, we'll create assertion using inline crypto
            // This will be refactored in later phases
            var jwtToken = CreateJwtToken(requestContext, certificate);
            string assertion = SignJwt(jwtToken, certificate);

            stopwatch.Stop();

            // Build OAuth2 parameters - certificate credential ALWAYS returns auth params
            var parameters = new Dictionary<string, string>
            {
                [OAuth2Parameter.ClientAssertionType] = OAuth2AssertionType.JwtBearer,
                [OAuth2Parameter.ClientAssertion] = assertion
            };

            // Build metadata
            var metadata = new CredentialMaterialMetadata(
                assertionType: AssertionType.CertificateWithoutSni,
                credentialSource: Certificate != null ? "static" : "provider",
                mtlsCertificateIdHashPrefix: CredentialMaterialHelper.GetCertificateIdHashPrefix(certificate),
                mtlsCertificateRequested: requestContext.MtlsRequired,
                resolutionTimeMs: stopwatch.ElapsedMilliseconds);

            // Return material with BOTH auth params AND certificate
            // Auth params are used for OAuth2 client authentication
            // Certificate is additionally used for TLS channel binding
            return new CredentialMaterial(
                tokenRequestParameters: parameters,
                mtlsCertificate: certificate,
                metadata: metadata);
        }

        private string CreateJwtToken(CredentialRequestContext requestContext, X509Certificate2 certificate)
        {
            long validFrom = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long validTo = validFrom + (60 * 10); // 10 minutes

            var payload = new Dictionary<string, object>
            {
                ["aud"] = requestContext.TokenEndpoint,
                ["iss"] = requestContext.ClientId,
                ["sub"] = requestContext.ClientId,
                ["nbf"] = validFrom,
                ["exp"] = validTo,
                ["jti"] = Guid.NewGuid().ToString()
            };

            // Add custom claims if provided
            if (_claimsToSign != null && _appendDefaultClaims)
            {
                foreach (var claim in _claimsToSign)
                {
                    payload[claim.Key] = claim.Value;
                }
            }

            return JsonHelper.SerializeToJson(payload);
        }

        private string SignJwt(string payload, X509Certificate2 certificate)
        {
            // Create JWT header
            var header = new Dictionary<string, object>
            {
                ["alg"] = "RS256",
                ["typ"] = "JWT"
            };

            // Base64URL encode header and payload
            string encodedHeader = Base64UrlHelpers.Encode(JsonHelper.SerializeToJson(header));
            string encodedPayload = Base64UrlHelpers.Encode(payload);
            string message = $"{encodedHeader}.{encodedPayload}";

            // Sign using RSA
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using (RSA rsa = certificate.GetRSAPrivateKey())
            {
                byte[] signatureBytes = rsa.SignData(messageBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                string encodedSignature = Base64UrlHelpers.Encode(signatureBytes);
                
                return $"{message}.{encodedSignature}";
            }
        }

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

        public async Task<ClientCredentialApplicationResult> AddConfidentialClientParametersAsync(
            OAuth2Client oAuth2Client,
            AuthenticationRequestParameters requestParameters,
            ICryptographyManager cryptographyManager,
            string tokenEndpoint,
            CancellationToken cancellationToken)
        {
            string clientId = requestParameters.AppConfig.ClientId;

            // Log the incoming request parameters for diagnostic purposes
            requestParameters.RequestContext.Logger.Verbose(
                () => $"Building assertion from certificate with clientId: {clientId} at endpoint: {tokenEndpoint}");

            // If mTLS cert is not already set for the request, proceed with JWT bearer client assertion.
            if (requestParameters.MtlsCertificate == null)
            {
                requestParameters.RequestContext.Logger.Verbose(
                    () => "Proceeding with JWT token creation and adding client assertion.");

                // Resolve the certificate via the provider
                X509Certificate2 certificate =
                    await ResolveCertificateAsync(requestParameters, tokenEndpoint, cancellationToken)
                        .ConfigureAwait(false);

                // Store the resolved certificate in request parameters for later use (e.g., ExecutionResult)
                requestParameters.ResolvedCertificate = certificate;

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

                // No extra outputs for the common case.
                return ClientCredentialApplicationResult.None;
            }

            // mTLS path: a certificate is already set on the request (e.g., mTLS/PoP transport).
            requestParameters.RequestContext.Logger.Verbose(
                () => "mTLS certificate is set for this request. Skipping JWT client assertion generation.");

            requestParameters.ResolvedCertificate = requestParameters.MtlsCertificate;

            // Return the mTLS certificate via the result object so the pipeline can use it
            // (HTTP handler + policy/region checks).
            return new ClientCredentialApplicationResult
            {
                MtlsCertificate = requestParameters.MtlsCertificate,
                UseJwtPopClientAssertion = false // no client assertion set here
            };
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

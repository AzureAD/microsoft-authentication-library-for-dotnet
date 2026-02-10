// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            CredentialRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            // Resolve the certificate via the provider
            var opts = new AssertionRequestOptions
            {
                CancellationToken = cancellationToken,
                ClientID = requestContext.ClientId,
                TokenEndpoint = requestContext.TokenEndpoint,
                ClientCapabilities = requestContext.ClientCapabilities,
                Claims = requestContext.Claims
            };

            X509Certificate2 cert = await _certificateProvider(opts).ConfigureAwait(false);

            if (cert == null)
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    "The certificate provider callback returned null. Ensure the callback returns a valid X509Certificate2 instance.");
            }

            if (!cert.HasPrivateKey)
            {
                throw new MsalClientException(
                    MsalError.CertWithoutPrivateKey,
                    MsalErrorMessage.CertMustHavePrivateKey(cert.FriendlyName));
            }

            // Build JWT assertion
            var jwtToken = new JsonWebToken(
                requestContext.CryptographyManager,
                requestContext.ClientId,
                requestContext.TokenEndpoint,
                _claimsToSign,
                _appendDefaultClaims);

            string assertion = jwtToken.Sign(cert, requestContext.SendX5C, requestContext.UseSha2);

            sw.Stop();

            var tokenParameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                { OAuth2Parameter.ClientAssertion, assertion }
            };

            return new CredentialMaterial(
                tokenRequestParameters: tokenParameters,
                mtlsCertificate: cert,
                metadata: new CredentialMaterialMetadata(
                    credentialType: CredentialType.ClientCertificate,
                    credentialSource: Certificate == null ? "dynamic" : "static",
                    mtlsCertificateIdHashPrefix: CredentialMaterialHelper.GetCertificateIdHashPrefix(cert),
                    mtlsCertificateRequested: requestContext.MtlsRequired,
                    resolutionTimeMs: sw.ElapsedMilliseconds));
        }
    }
}

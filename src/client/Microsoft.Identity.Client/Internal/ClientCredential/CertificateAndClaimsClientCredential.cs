// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
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
            _base64EncodedThumbprint = Base64UrlHelpers.Encode(certificate.GetCertHash());
        }

        public Task AddConfidentialClientParametersAsync(
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

                bool useSha2 = requestParameters.AuthorityManager.Authority.AuthorityInfo.IsSha2CredentialSupported;

                var jwtToken = new JsonWebToken(
                cryptographyManager,
                clientId,
                tokenEndpoint,
                _claimsToSign,
                _appendDefaultClaims);

                string assertion = jwtToken.Sign(Certificate, requestParameters.SendX5C, useSha2);

                oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
                oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, assertion);
            }
            else
            {
                // Log that MTLS PoP is required and JWT token creation is skipped
                requestParameters.RequestContext.Logger.Verbose(() => "MTLS PoP Client credential request. Skipping client assertion.");
            }

            return Task.CompletedTask;
        }
    }
}

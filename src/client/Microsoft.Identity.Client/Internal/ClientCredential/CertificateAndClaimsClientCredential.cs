// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
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

        public CertificateAndClaimsClientCredential(X509Certificate2 certificate, IDictionary<string, string> claimsToSign, bool appendDefaultClaims)
        {
            Certificate = certificate;
            _claimsToSign = claimsToSign;
            _appendDefaultClaims = appendDefaultClaims;
            _base64EncodedThumbprint = Base64UrlHelpers.Encode(certificate.GetCertHash());
        }

        public Task AddConfidentialClientParametersAsync(
            OAuth2Client oAuth2Client, 
            ILoggerAdapter logger, 
            ICryptographyManager cryptographyManager, 
            string clientId, 
            string tokenEndpoint, 
            bool sendX5C, 
            CancellationToken cancellationToken)
        {
            var jwtToken = new JsonWebToken(
                cryptographyManager,
                clientId,
                tokenEndpoint,
                _claimsToSign,
                _appendDefaultClaims);

            string assertion = jwtToken.Sign(Certificate, _base64EncodedThumbprint, sendX5C);

            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, assertion);

            return Task.CompletedTask;
        }
    }
}

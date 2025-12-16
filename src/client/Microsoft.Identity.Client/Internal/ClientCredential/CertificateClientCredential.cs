// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal class CertificateClientCredential : CertificateAndClaimsClientCredential
    {
        /// <summary>
        /// Gets the static certificate when using WithCertificate(X509Certificate2).
        /// This is needed for mTLS scenarios where we need synchronous access to the certificate.
        /// Returns null when using dynamic certificate providers.
        /// </summary>
        public new X509Certificate2 Certificate { get; }

        public CertificateClientCredential(X509Certificate2 certificate) 
            : base(certificateProvider: _ => Task.FromResult(certificate), claimsToSign: null, appendDefaultClaims: true, certificate: certificate) 
        { 
            Certificate = certificate;
        }
    }
}

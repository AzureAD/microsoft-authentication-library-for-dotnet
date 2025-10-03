// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.V2;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenForManagedIdentityParameters : IAcquireTokenParameters
    {
        public bool ForceRefresh { get; set; }

        public string Resource { get; set; }

        public string Claims { get; set; }

        public string RevokedTokenHash { get; set; }

        public bool IsMtlsPopRequested { get; set; }

        // When the MI source produced / resolved an mTLS binding certificate, we attach it here
        // so the request layer can apply a cache-correct IAuthenticationOperation.
        public X509Certificate2 MtlsCertificate { get; set; }

        // CSR response we get back when IMDSv2 minted the certificate.
        internal CertificateRequestResponse CertificateRequestResponse { get; set; }

        internal Func<AttestationTokenInput, CancellationToken, Task<AttestationTokenResponse>> AttestationTokenProvider { get; set; }

        public void LogParameters(ILoggerAdapter logger)
        {
            if (logger.IsLoggingEnabled(LogLevel.Info))
            {
                logger.Info(
                    $"""
                     === AcquireTokenForManagedIdentityParameters ===
                     ForceRefresh: {ForceRefresh}
                     Resource: {Resource}
                     Claims: {!string.IsNullOrEmpty(Claims)}
                     RevokedTokenHash: {!string.IsNullOrEmpty(RevokedTokenHash)}
                     """);

                logger.Info(() =>
                    $"[AcquireTokenForManagedIdentityParameters] IsMtlsPopRequested={IsMtlsPopRequested}, " +
                    $"MtlsCert={(MtlsCertificate != null ? MtlsCertificate.Thumbprint : "null")}");
            }
        }
    }
}

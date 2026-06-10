// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenForManagedIdentityParameters : IAcquireTokenParameters
    {
        public bool ForceRefresh { get; set; }

        public string Resource { get; set; }

        public string Claims { get; set; }

        /// <summary>
        /// Client-originated claims to be sent to the identity endpoint.
        /// Unlike <see cref="Claims"/> (server-issued), these are cached and keyed on the claims value.
        /// </summary>
        public string ClientClaims { get; set; }

        public string RevokedTokenHash { get; set; }

        public bool IsMtlsPopRequested { get; set; }

        /// <summary>
        /// The minimum mTLS binding strength the host must support for the request to succeed.
        /// Defaults to <see cref="MtlsBindingStrength.None"/> (no floor).
        /// </summary>
        public MtlsBindingStrength MtlsPopMinStrength { get; set; } = MtlsBindingStrength.None;

        internal X509Certificate2 MtlsCertificate { get; set; }

        /// <summary>
        /// Optional delegate for obtaining attestation JWT for Credential Guard keys.
        /// Set by the KeyAttestation package via .WithAttestationSupport().
        /// Signature: (endpoint, keyHandle, clientId, keyId, logger, cancellationToken) → JWT or null.
        /// </summary>
        public Func<string, SafeHandle, string, string, ILoggerAdapter, CancellationToken, Task<string>> AttestationTokenProvider { get; set; }

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
                     ClientClaims: {!string.IsNullOrEmpty(ClientClaims)}
                     RevokedTokenHash: {!string.IsNullOrEmpty(RevokedTokenHash)}
                     IsMtlsPopRequested: {IsMtlsPopRequested}
                     MtlsPopMinStrength: {MtlsPopMinStrength}
                     """);
            }
        }
    }
}

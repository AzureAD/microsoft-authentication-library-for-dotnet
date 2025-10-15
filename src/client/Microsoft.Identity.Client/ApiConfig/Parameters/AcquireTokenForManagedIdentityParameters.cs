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

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenForManagedIdentityParameters : IAcquireTokenParameters
    {
        public bool ForceRefresh { get; set; }

        public string Resource { get; set; }

        public string Claims { get; set; }

        public string RevokedTokenHash { get; set; }

        public bool IsMtlsPopRequested { get; set; }

        internal Func<AttestationTokenInput, CancellationToken, Task<AttestationTokenResponse>> AttestationTokenProvider { get; set; }

        internal X509Certificate2 MtlsCertificate { get; set; }

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
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal
{
    internal class RequestContext
    {
        public Guid CorrelationId { get; }
        public ILoggerAdapter Logger { get; }
        public IServiceBundle ServiceBundle { get; }

        /// <summary>
        /// One and only one ApiEvent is associated with each request.
        /// </summary>
        public ApiEvent ApiEvent { get; set; }

        public CancellationToken UserCancellationToken { get; }

        public X509Certificate2 MtlsCertificate { get; }

        public bool IsAttestationRequested { get; set; }
        
        public bool IsMtlsRequested { get; set; }

        public RequestContext(IServiceBundle serviceBundle, Guid correlationId, X509Certificate2 mtlsCertificate, CancellationToken cancellationToken = default)
        {
            ServiceBundle = serviceBundle ?? throw new ArgumentNullException(nameof(serviceBundle));
            Logger = LoggerHelper.CreateLogger(correlationId, ServiceBundle.Config);
            CorrelationId = correlationId;
            UserCancellationToken = cancellationToken;
            IsMtlsRequested = mtlsCertificate != null;
        }
    }
}

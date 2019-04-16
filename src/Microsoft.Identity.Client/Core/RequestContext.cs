// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Mats.Internal;

namespace Microsoft.Identity.Client.Core
{
    internal class RequestContext
    {
        public RequestContext(string clientId, ICoreLogger logger, Guid telemetryCorrelationId)
        {
            ClientId = string.IsNullOrWhiteSpace(clientId) ? "unset_client_id" : clientId;
            Logger = logger;
            TelemetryCorrelationId = telemetryCorrelationId.AsMatsCorrelationId();
        }

        public string TelemetryCorrelationId { get; }
        public string ClientId { get; set; }

        public ICoreLogger Logger { get; set; }

        public static RequestContext CreateForTest(IServiceBundle serviceBundle = null)
        {
            var telemetryCorrelationId = Guid.NewGuid();

            var logger = serviceBundle?.DefaultLogger ?? MsalLogger.Create(
                             telemetryCorrelationId,
                             null,
                             isDefaultPlatformLoggingEnabled: true);

            return new RequestContext(null, logger, telemetryCorrelationId);
        }
    }
}

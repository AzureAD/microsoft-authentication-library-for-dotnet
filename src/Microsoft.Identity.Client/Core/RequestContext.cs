// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal;

namespace Microsoft.Identity.Client.Core
{
    internal class RequestContext
    {
        public string TelemetryCorrelationId { get; }
        public ICoreLogger Logger { get;  }
        public IServiceBundle ServiceBundle { get; }

        public RequestContext(IServiceBundle serviceBundle, Guid telemetryCorrelationId)
        {
            ServiceBundle = serviceBundle ?? throw new ArgumentNullException(nameof(serviceBundle));
            Logger = MsalLogger.Create(telemetryCorrelationId, ServiceBundle.Config);
            TelemetryCorrelationId = telemetryCorrelationId.AsMatsCorrelationId();
        }
    }
}

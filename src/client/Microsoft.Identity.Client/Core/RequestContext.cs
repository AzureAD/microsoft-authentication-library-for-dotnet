// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.Core
{
    internal class RequestContext
    {
        public Guid CorrelationId { get; }
        public ICoreLogger Logger { get;  }
        public IServiceBundle ServiceBundle { get; }

        /// <summary>
        /// One and only one ApiEvent is associated with each request.
        /// </summary>
        public ApiEvent ApiEvent { get; set; }

        public RequestContext(IServiceBundle serviceBundle, Guid correlationId)
        {
            ServiceBundle = serviceBundle ?? throw new ArgumentNullException(nameof(serviceBundle));
            Logger = MsalLogger.Create(correlationId, ServiceBundle.Config);
            CorrelationId = correlationId;
        }
    }
}

﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
#if !ANDROID && !iOS
using Microsoft.IdentityModel.Abstractions;
#endif

namespace Microsoft.Identity.Client.Internal
{
    internal class RequestContext
    {
        public Guid CorrelationId { get; }
        public ILoggerAdapter Logger { get; }
#if !ANDROID && !iOS
        public IIdentityLogger ExternalCacheLogger { get; }
#endif
        public IServiceBundle ServiceBundle { get; }

        /// <summary>
        /// One and only one ApiEvent is associated with each request.
        /// </summary>
        public ApiEvent ApiEvent { get; set; }

        public CancellationToken UserCancellationToken { get; }

        public RequestContext(IServiceBundle serviceBundle, Guid correlationId, CancellationToken cancellationToken = default)
        {
            ServiceBundle = serviceBundle ?? throw new ArgumentNullException(nameof(serviceBundle));
            Logger = LoggerAdapterHelper.CreateLogger(correlationId, ServiceBundle.Config);
#if !ANDROID && !iOS
            ExternalCacheLogger = LoggerAdapterHelper.CreateExternalCacheLogger(correlationId, ServiceBundle.Config);
#endif
            CorrelationId = correlationId;
            UserCancellationToken = cancellationToken;
        }

        public TelemetryHelper CreateTelemetryHelper(ApiEvent eventToStart)
        {
            return new TelemetryHelper(
                ServiceBundle.HttpTelemetryManager,
                eventToStart);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore.OpenTelemetry
{
    internal interface IOtelInstrumentation
    {
        internal void LogSuccessMetrics(
            string platform,
            ApiEvent.ApiIds apiId,
            CacheLevel cacheLevel,
            long totalDurationInUs,
            AuthenticationResultMetadata authResultMetadata,
            ILoggerAdapter logger);

        internal void IncrementSuccessCounter(string platform,
            ApiEvent.ApiIds apiId,
            TokenSource tokenSource,
            CacheRefreshReason cacheRefreshReason,
            CacheLevel cacheLevel,
            ILoggerAdapter logger);

        internal void LogFailureMetrics(string platform, 
            string errorCode, 
            ApiEvent.ApiIds apiId, 
            CacheRefreshReason cacheRefreshReason);
    }
}

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
            string callerSdkId,
            string callerSdkVersion,
            CacheLevel cacheLevel,
            long totalDurationInUs,
            AuthenticationResultMetadata authResultMetadata,
            ILoggerAdapter logger,
            bool isExtendedMetricsEnabled);

        internal void IncrementSuccessCounter(string platform,
            ApiEvent.ApiIds apiId,
            string callerSdkId,
            string callerSdkVersion,
            TokenSource tokenSource,
            CacheRefreshReason cacheRefreshReason,
            CacheLevel cacheLevel,
            ILoggerAdapter logger,
            int TokenType);

        internal void LogSuccessHttpDuration(
            string platform,
            ApiEvent.ApiIds apiId,
            AuthenticationResultMetadata authResultMetadata,
            bool isExtendedMetricsEnabled);

        internal void LogFailureMetrics(
            string platform,
            ApiEvent apiEvent,
            string errorCode,
            int httpStatusCode,
            long totalDurationInMs,
            bool isExtendedMetricsEnabled);
    }
}

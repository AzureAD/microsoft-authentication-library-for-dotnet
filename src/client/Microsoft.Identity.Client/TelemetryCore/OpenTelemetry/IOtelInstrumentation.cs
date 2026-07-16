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
            DateTimeOffset expiresOn,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null);

        internal void IncrementSuccessCounter(
            string platform,
            ApiEvent.ApiIds apiId,
            string callerSdkId,
            string callerSdkVersion,
            TokenSource tokenSource,
            CacheRefreshReason cacheRefreshReason,
            CacheLevel cacheLevel,
            ILoggerAdapter logger,
            int TokenType,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null);

        internal void LogFailureMetrics(
            string platform,
            string errorCode,
            ApiEvent apiEvent,
            string callerSdkId,
            string callerSdkVersion,
            CacheRefreshReason cacheRefreshReason,
            int tokenType,
            int httpStatusCode,
            long totalDurationInMs,
            string rawStsErrorCode = null,
            ILoggerAdapter logger = null,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null);

        internal void IncrementFailureCounter(
            string platform,
            string errorCode,
            ApiEvent.ApiIds apiId,
            string callerSdkId,
            string callerSdkVersion,
            CacheRefreshReason cacheRefreshReason,
            int tokenType,
            string rawStsErrorCode = null,
            ILoggerAdapter logger = null,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null);

        internal void LogFailureHttpDuration(
            string platform,
            ApiEvent apiEvent,
            int httpStatusCode,
            ILoggerAdapter logger = null,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null);

        internal void LogRemainingTokenLifetime(
            string platform,
            ApiEvent.ApiIds apiId,
            TokenSource tokenSource,
            CacheLevel cacheLevel,
            CacheRefreshReason cacheRefreshReason,
            int tokenType,
            DateTimeOffset expiresOn,
            ILoggerAdapter logger = null,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null);

        internal void LogSuccessHttpDuration(
            string platform,
            ApiEvent.ApiIds apiId,
            AuthenticationResultMetadata authResultMetadata,
            ILoggerAdapter logger = null,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null);
    }
}

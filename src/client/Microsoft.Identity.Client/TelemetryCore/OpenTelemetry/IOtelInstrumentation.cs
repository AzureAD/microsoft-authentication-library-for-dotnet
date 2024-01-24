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
        internal void LogFailedMetrics(string platform, string errorCode, ApiEvent.ApiIds apiId, bool isProactiveTokenRefresh);
    }
}

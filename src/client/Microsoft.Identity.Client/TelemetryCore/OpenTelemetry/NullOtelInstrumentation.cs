// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore.OpenTelemetry
{
    internal class NullOtelInstrumentation : IOtelInstrumentation
    {
        public void LogSuccessMetrics(
            string platform,
            ApiEvent.ApiIds apiId,
            CacheLevel cacheLevel,
            long totalDurationInUs,
            AuthenticationResultMetadata authResultMetadata,
            ILoggerAdapter logger)
        {
            // No op
        }

        public void LogFailedMetrics(string platform, string errorCode, ApiEvent.ApiIds apiId, CacheRefreshReason cacheRefreshReason)
        {
            // No op
        }

        void IOtelInstrumentation.IncrementToSuccessCounter(string platform, ApiEvent.ApiIds apiId, TokenSource tokenSource, CacheRefreshReason cacheRefreshReason, CacheLevel cacheLevel, ILoggerAdapter logger)
        {
            // No op
        }
    }
}

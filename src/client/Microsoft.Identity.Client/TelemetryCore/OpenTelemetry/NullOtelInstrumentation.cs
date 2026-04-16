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
            string callerSdkId,
            string callerSdkVersion,
            CacheLevel cacheLevel,
            long totalDurationInUs,
            TokenAcquisitionResult context,
            Action<TokenAcquisitionResult, IList<KeyValuePair<string, object>>> tagsEnricher,
            ILoggerAdapter logger)
        {
            // No op
        }

        public void LogFailureMetrics(
            string platform,
            ApiEvent.ApiIds apiId,
            string callerSdkId,
            string callerSdkVersion,
            CacheRefreshReason cacheRefreshReason,
            int tokenType,
            TokenAcquisitionResult context,
            Action<TokenAcquisitionResult, IList<KeyValuePair<string, object>>> tagsEnricher)
        {
            // No op
        }

        void IOtelInstrumentation.IncrementSuccessCounter(
            string platform,
            ApiEvent.ApiIds apiId,
            string callerSdkId,
            string callerSdkVersion,
            TokenSource tokenSource,
            CacheRefreshReason cacheRefreshReason,
            CacheLevel cacheLevel,
            ILoggerAdapter logger,
            int tokenType,
            IList<KeyValuePair<string, object>> extraTags)
        {
            // No op
        }
    }
}

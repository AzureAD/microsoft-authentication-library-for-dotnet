// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.TelemetryCore.OpenTelemetry
{
    internal class NullOtelInstrumentation : IOtelInstrumentation
    {
        bool IOtelInstrumentation.IsTracingEnabled => false;

        public void LogSuccessMetrics(
            string platform,
            string apiId,
            string cacheLevel,
            long totalDurationInUs,
            AuthenticationResultMetadata authResultMetadata,
            ILoggerAdapter logger)
        {
            // No op
        }

        public void LogFailedMetrics(string platform, string errorCode)
        {
            // No op
        }

        void IOtelInstrumentation.LogActivity(Dictionary<string, object> tags)
        {
            // No op
        }

        void IOtelInstrumentation.LogActivityStatus(bool IsSuccessful)
        {
            // No op
        }

        void IOtelInstrumentation.StartActivity()
        {
            // No op
        }

        void IOtelInstrumentation.StopActivity()
        {
            // No op
        }
    }
}

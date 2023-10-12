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
        public void LogActivity(Dictionary<string, object> tags)
        {
            // No op
        }

        public void LogActivityStatus(bool success)
        {
            // No op
        }

        public void StopActivity()
        {
            // No op
        }

        public void LogSuccessMetrics(
            string platform,
            AuthenticationResultMetadata authResultMetadata,
            string apiId,
            string cacheLevel,
            ILoggerAdapter logger)
        {
            // No op
        }

        public void LogFailedMetrics(string platform, string errorCode)
        {
            // No op
        }
    }
}

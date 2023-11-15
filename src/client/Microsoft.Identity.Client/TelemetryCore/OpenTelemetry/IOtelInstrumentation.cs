// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.TelemetryCore.OpenTelemetry
{
    internal interface IOtelInstrumentation
    {
        internal void LogActivity(Dictionary<string, object> tags);
        internal void LogActivityStatus(bool success);
        internal void StopActivity();
        internal void LogSuccessMetrics(
            string platform,
            AuthenticationResultMetadata authResultMetadata,
            string apiId,
            string cacheLevel,
            ILoggerAdapter logger, 
            long totalDurationInUs);
        internal void LogFailedMetrics(string platform, string errorCode);

    }
}

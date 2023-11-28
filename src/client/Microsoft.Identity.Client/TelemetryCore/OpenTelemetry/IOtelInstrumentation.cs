﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.TelemetryCore.OpenTelemetry
{
    internal interface IOtelInstrumentation
    {
        internal void LogSuccessMetrics(
            string platform,
            string apiId,
            string cacheLevel,
            long totalDurationInUs,
            AuthenticationResultMetadata authResultMetadata,
            ILoggerAdapter logger);
        internal void LogFailedMetrics(string platform, string errorCode);

        internal void LogActivity(Dictionary<string, object> tags);
        internal void LogActivityStatus(bool IsSuccessful);
        internal void StartActivity();
        internal void StopActivity();
        internal bool IsTracingEnabled { get; }
    }
}

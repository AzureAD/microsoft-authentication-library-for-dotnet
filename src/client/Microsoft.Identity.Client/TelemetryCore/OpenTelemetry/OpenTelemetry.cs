// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if NET6_0_OR_GREATER
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Identity.Client.TelemetryCore.OpenTelemetry
{
    internal class OpenTelemetry
    {
        public const string MeterName = "MSAL.Net.Meter";

        public static readonly Meter Meter = new Meter(MeterName, "1.0.0");

        public static readonly Counter<long> SuccessCounter = Meter.CreateCounter<long>(
            "acquire_token_success",
            description: "Number of successful token acquisition calls");

        public static readonly Counter<long> FailureCounter = Meter.CreateCounter<long>(
            "acquire_token_failed",
            description: "Number of failed token acquisition calls");
    }
}
#endif

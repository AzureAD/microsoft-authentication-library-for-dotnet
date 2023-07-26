// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//#if NET6_0_OR_GREATER || NET461_OR_GREATER || NET20_OR_GREATER
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
//#endif

namespace Microsoft.Identity.Client.TelemetryCore.OpenTelemetry
{
    /// <summary>
    /// Class to hold the OpenTelemetry objects used by MSAL.
    /// </summary>
    public class OpenTelemetryClient

    {
        /// <summary>
        /// Constant to hold the name of the Meter.
        /// </summary>
        public const string MeterName = "MSAL.Net.Meter";

        /// <summary>
        /// Constant to holt the name of the ActivitySource.
        /// </summary>
        public const string ActivitySourceName = "MSAL.Net";

//#if NET6_0_OR_GREATER || NET462_OR_GREATER || NET20_OR_GREATER
        /// <summary>
        /// Meter to hold the MSAL metrics.
        /// </summary>
        public static readonly Meter Meter = new Meter(MeterName, "1.0.0");

        /// <summary>
        /// Counter to hold the number of successful token acquisition calls.
        /// </summary>
        public static readonly Counter<long> SuccessCounter = Meter.CreateCounter<long>(
            "acquire_token_success",
            description: "Number of successful token acquisition calls");

        /// <summary>
        /// Counter to hold the number of failed token acquisition calls.
        /// </summary>
        public static readonly Counter<long> FailureCounter = Meter.CreateCounter<long>(
            "acquire_token_failed",
            description: "Number of failed token acquisition calls");

        /// <summary>
        /// Histogram to record total duration of token acquisition calls.
        /// </summary>
        public static readonly Histogram<long> DurationTotal = Meter.CreateHistogram<long>(
            "acquire_token_duration_total",
            unit: "ms",
            description: "Performance of token acquisition calls total latency");

        /// <summary>
        /// Histogram to record duration in cache of token acquisition calls.
        /// </summary>
        public static readonly Histogram<long> DurationInCache = Meter.CreateHistogram<long>(
            "acquire_token_duration_in_cache",
            unit: "ms",
            description: "Performance of token acquisition calls cache latency");

        /// <summary>
        /// Histogram to record duration in http of token acquisition calls.
        /// </summary>
        public static readonly Histogram<long> DurationInHttp = Meter.CreateHistogram<long>(
            "acquire_token_duration_in_http",
            unit: "ms",
            description: "Performance of token acquisition calls network latency");

        internal static readonly ActivitySource AcquireTokenActivity = new ActivitySource(ActivitySourceName, "1.0.0");
//#endif

        // Aggregates the successful requests based on client id, token source and cache refresh reason.
        internal static void LogSuccessMetrics(
            string platform, 
            string clientId, 
            AuthenticationResultMetadata authResultMetadata,
            string cacheLevel)
        {
//#if NET6_0_OR_GREATER || NET462_OR_GREATER || NET20_OR_GREATER
            SuccessCounter.Add(1,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ClientId, clientId),
                new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                new(TelemetryConstants.CacheInfoTelemetry, authResultMetadata.CacheRefreshReason), 
                new(TelemetryConstants.CacheLevel, cacheLevel));

            DurationTotal.Record(authResultMetadata.DurationTotalInMs);
            DurationInCache.Record(authResultMetadata.DurationInCacheInMs);
            DurationInHttp.Record(authResultMetadata.DurationInHttpInMs);
//#endif
        }

        // Aggregates the failure requests based on client id, and MSAL's error code or exception name if the exception is not an MSAL exception.
        internal static void LogToFailureCounter(string platform, string clientId, string errorCode)
        {
//#if NET6_0_OR_GREATER || NET462_OR_GREATER || NET20_OR_GREATER
            FailureCounter.Add(1,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ClientId, clientId),
                new(TelemetryConstants.ErrorCode, errorCode));
//#endif
        }
    }
}

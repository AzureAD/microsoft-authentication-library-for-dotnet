// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//#if NET6_0_OR_GREATER || NET461_OR_GREATER || NET20_OR_GREATER
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Identity.Client.Core;
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
        public const string MeterName = "ID4S_MSAL";

        /// <summary>
        /// Constant to holt the name of the ActivitySource.
        /// </summary>
        public const string ActivitySourceName = "MSAL_Activity";

        /// <summary>
        /// Meter to hold the MSAL metrics.
        /// </summary>
        internal static readonly Meter Meter = new Meter(MeterName, "1.0.0");

        /// <summary>
        /// Counter to hold the number of successful token acquisition calls.
        /// </summary>
        internal static readonly Lazy<Counter<long>> s_successCounter = new(() => Meter.CreateCounter<long>(
            "MsalSuccess",
            description: "Number of successful token acquisition calls"));

        /// <summary>
        /// Counter to hold the number of failed token acquisition calls.
        /// </summary>
        internal static readonly Lazy<Counter<long>> s_failureCounter = new(() => Meter.CreateCounter<long>(
            "MsalFailed",
            description: "Number of failed token acquisition calls"));

        /// <summary>
        /// Histogram to record total duration of token acquisition calls.
        /// </summary>
        internal static readonly Histogram<long> s_durationTotal = Meter.CreateHistogram<long>(
            "MsalTotalDurationHistogram",
            unit: "ms",
            description: "Performance of token acquisition calls total latency");

        /// <summary>
        /// Histogram to record duration in cache of token acquisition calls.
        /// </summary>
        internal static readonly Histogram<long> s_durationInCache = Meter.CreateHistogram<long>(
            "MsalDurationInCacheHistogram",
            unit: "ms",
            description: "Performance of token acquisition calls cache latency");

        /// <summary>
        /// Histogram to record duration in http of token acquisition calls.
        /// </summary>
        internal static readonly Histogram<long> s_durationInHttp = Meter.CreateHistogram<long>(
            "MsalDurationInHttpHistogram",
            unit: "ms",
            description: "Performance of token acquisition calls network latency");

        internal static readonly ActivitySource s_acquireTokenActivity = new ActivitySource(ActivitySourceName, "1.0.0");

        // Aggregates the successful requests based on client id, token source and cache refresh reason.
        internal static void LogSuccessMetrics(
            string platform, 
            string clientId, 
            AuthenticationResultMetadata authResultMetadata,
            string apiId,
            string cacheLevel, 
            ILoggerAdapter logger)
        {
            logger.Info("[OpenTelemetry] Incrementing success counter.");
            s_successCounter.Value.Add(1,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ClientId, clientId),
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                new(TelemetryConstants.CacheInfoTelemetry, authResultMetadata.CacheRefreshReason), 
                new(TelemetryConstants.CacheLevel, cacheLevel));
            logger.Info("[OpenTelemetry] Completed incrementing to success counter.");

            s_durationTotal.Record(authResultMetadata.DurationTotalInMs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.ClientId, clientId));
            s_durationInCache.Record(authResultMetadata.DurationInCacheInMs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.ClientId, clientId));
            s_durationInHttp.Record(authResultMetadata.DurationInHttpInMs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.ClientId, clientId));
        }

        // Aggregates the failure requests based on client id, and MSAL's error code or exception name if the exception is not an MSAL exception.
        internal static void LogToFailureCounter(string platform, string clientId, string errorCode)
        {
            s_failureCounter.Value.Add(1,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ClientId, clientId),
                new(TelemetryConstants.ErrorCode, errorCode));
        }
    }
}

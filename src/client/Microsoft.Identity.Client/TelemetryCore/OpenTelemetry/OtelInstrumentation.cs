// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Cache;
#if SUPPORTS_OTEL
using System.Diagnostics.Metrics;
#endif
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.TelemetryCore.OpenTelemetry
{
    /// <summary>
    /// Class to hold the OpenTelemetry objects used by MSAL.
    /// </summary>
    internal class OtelInstrumentation : IOtelInstrumentation
    {
        /// <summary>
        /// Constant to hold the name of the Meter.
        /// </summary>
        public const string MeterName = "MicrosoftIdentityClient_Common_Meter";

        /// <summary>
        /// Constant to holt the name of the ActivitySource.
        /// </summary>
        public const string ActivitySourceName = "MicrosoftIdentityClient_Activity";

#if SUPPORTS_OTEL
        /// <summary>
        /// Meter to hold the MSAL metrics.
        /// </summary>
        internal static readonly Meter Meter = new Meter(MeterName, "1.0.0");

        /// <summary>
        /// ActivitySource to hold the MSAL activities.
        /// </summary>
        internal static readonly ActivitySource s_acquireTokenActivity = new ActivitySource(ActivitySourceName, "1.0.0");

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
            "MsalTotalDuration",
            unit: "ms",
            description: "Performance of token acquisition calls total latency");

        /// <summary>
        /// Histogram to record duration in cache of token acquisition calls.
        /// </summary>
        internal static readonly Histogram<long> s_durationInCache = Meter.CreateHistogram<long>(
            "MsalDurationInCache",
            unit: "ms",
            description: "Performance of token acquisition calls cache latency");

        /// <summary>
        /// Histogram to record duration in http of token acquisition calls.
        /// </summary>
        internal static readonly Histogram<long> s_durationInHttp = Meter.CreateHistogram<long>(
            "MsalDurationInHttp",
            unit: "ms",
            description: "Performance of token acquisition calls network latency");

        internal static readonly Lazy<Activity> s_activity = new Lazy<Activity>(() => s_acquireTokenActivity.StartActivity("Token Acquisition", ActivityKind.Internal));
#endif

        void IOtelInstrumentation.LogActivity(Dictionary<string, object> tags)
        {
#if SUPPORTS_OTEL
            foreach (KeyValuePair<string, object> tag in tags)
            {
                s_activity.Value?.AddTag(tag.Key, tag.Value);
            }
#endif
        }

        void IOtelInstrumentation.LogActivityStatus(bool success)
        {
#if SUPPORTS_OTEL
            if (success)
            {
                s_activity.Value?.SetStatus(ActivityStatusCode.Ok, "Success");
            }
            else
            {
                s_activity.Value?.SetStatus(ActivityStatusCode.Error, "Request failed");
            }
#endif
        }

        void IOtelInstrumentation.StopActivity()
        {
#if SUPPORTS_OTEL
            s_activity.Value?.Stop();
#endif
        }

        // Aggregates the successful requests based on token source and cache refresh reason.
        void IOtelInstrumentation.LogSuccessMetrics(
            string platform, 
            AuthenticationResultMetadata authResultMetadata,
            string apiId,
            string cacheLevel, 
            ILoggerAdapter logger)
        {
#if SUPPORTS_OTEL
            s_successCounter.Value.Add(1,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                new(TelemetryConstants.CacheInfoTelemetry, authResultMetadata.CacheRefreshReason),
                new(TelemetryConstants.CacheLevel, cacheLevel));
            logger.Info("[OpenTelemetry] Completed incrementing to success counter.");

            s_durationTotal.Record(authResultMetadata.DurationTotalInMs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId));

            // Only log cache duration if L2 cache was used.
            if (cacheLevel.Equals(CacheLevel.L2Cache))
            {
                s_durationInCache.Record(authResultMetadata.DurationInCacheInMs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId));
            }
            
            s_durationInHttp.Record(authResultMetadata.DurationInHttpInMs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId));
#endif
        }

        void IOtelInstrumentation.LogFailedMetrics(string platform, string errorCode)
        {
#if SUPPORTS_OTEL
            s_failureCounter.Value.Add(1,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ErrorCode, errorCode));
#endif
        }
    }
}

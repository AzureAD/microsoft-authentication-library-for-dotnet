// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.OpenTelemetry;
using System.Diagnostics.Metrics;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Platforms.Features.OpenTelemetry
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
        /// Constant to hold the name of the ActivitySource.
        /// </summary>
        public const string ActivitySourceName = "MicrosoftIdentityClient_Activity";

        private const string SuccessCounterName = "MsalSuccess";
        private const string FailedCounterName = "MsalFailure";
        private const string TotalDurationHistogramName = "MsalTotalDuration.1A";
        private const string DurationInL1CacheHistogramName = "MsalDurationInL1CacheInUs.1B";
        private const string DurationInL2CacheHistogramName = "MsalDurationInL2Cache.1A";
        private const string DurationInHttpHistogramName = "MsalDurationInHttp.1A";

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
            SuccessCounterName,
            description: "Number of successful token acquisition calls"));

        /// <summary>
        /// Counter to hold the number of failed token acquisition calls.
        /// </summary>
        internal static readonly Lazy<Counter<long>> s_failureCounter = new(() => Meter.CreateCounter<long>(
            FailedCounterName,
            description: "Number of failed token acquisition calls"));

        /// <summary>
        /// Histogram to record total duration in milliseconds of token acquisition calls.
        /// </summary>
        internal static readonly Histogram<long> s_durationTotal = Meter.CreateHistogram<long>(
            TotalDurationHistogramName,
            unit: "ms",
            description: "Performance of token acquisition calls total latency");

        /// <summary>
        /// Histogram to record total duration of token acquisition calls in microseconds(us) when token is fetched from L1 cache.
        /// </summary>
        internal static readonly Histogram<long> s_durationInL1CacheInUs = Meter.CreateHistogram<long>(
            DurationInL1CacheHistogramName,
            unit: "us",
            description: "Performance of token acquisition calls total latency in microseconds when L1 cache is used.");

        /// <summary>
        /// Histogram to record duration in L2 cache for token acquisition calls.
        /// </summary>
        internal static readonly Histogram<long> s_durationInL2Cache = Meter.CreateHistogram<long>(
            DurationInL2CacheHistogramName,
            unit: "ms",
            description: "Performance of token acquisition calls cache latency");

        /// <summary>
        /// Histogram to record duration in milliseconds in http when the token is fetched from identity provider.
        /// </summary>
        internal static readonly Histogram<long> s_durationInHttp = Meter.CreateHistogram<long>(
            DurationInHttpHistogramName,
            unit: "ms",
            description: "Performance of token acquisition calls network latency");

        internal static readonly Lazy<Activity> s_activity = new Lazy<Activity>(() => s_acquireTokenActivity.StartActivity("Token acquisition", ActivityKind.Internal));

        void IOtelInstrumentation.LogActivity(Dictionary<string, object> tags)
        {
            foreach (KeyValuePair<string, object> tag in tags)
            {
                s_activity.Value?.AddTag(tag.Key, tag.Value);
            }
        }

        void IOtelInstrumentation.LogActivityStatus(bool success)
        {
            if (success)
            {
                s_activity.Value?.SetStatus(ActivityStatusCode.Ok, "Success");
            }
            else
            {
                s_activity.Value?.SetStatus(ActivityStatusCode.Error, "Request failed");
            }
        }

        void IOtelInstrumentation.StopActivity()
        {
            s_activity.Value?.Stop();
        }

        // Aggregates the successful requests based on token source and cache refresh reason.
        void IOtelInstrumentation.LogSuccessMetrics(
            string platform,
            string apiId,
            string cacheLevel,
            long totalDurationInUs,
            AuthenticationResultMetadata authResultMetadata,
            ILoggerAdapter logger)
        {
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
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                new(TelemetryConstants.CacheLevel, cacheLevel));

            // Only log cache duration if L2 cache was used.
            if (cacheLevel.Equals(CacheLevel.L2Cache))
            {
                s_durationInL2Cache.Record(authResultMetadata.DurationInCacheInMs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId));
            }

            // Only log duration in HTTP when token is fetched from IDP
            if (!authResultMetadata.TokenSource.Equals(TokenSource.Cache))
            {
                s_durationInHttp.Record(authResultMetadata.DurationInHttpInMs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId));
            }

            // Only log duration in microseconds when the cache level is L1.
            if (authResultMetadata.TokenSource.Equals(TokenSource.Cache) && authResultMetadata.CacheLevel.Equals(CacheLevel.L1Cache))
            {
                s_durationInL1CacheInUs.Record(totalDurationInUs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                new(TelemetryConstants.CacheLevel, cacheLevel));
            }
        }

        void IOtelInstrumentation.LogFailedMetrics(string platform, string errorCode)
        {
            s_failureCounter.Value.Add(1,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ErrorCode, errorCode));
        }
    }
}

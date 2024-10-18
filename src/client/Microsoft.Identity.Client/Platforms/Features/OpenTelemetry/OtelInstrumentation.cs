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
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using static Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos.NativeMethods;

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

        private const string SuccessCounterName = "MsalSuccess";
        private const string FailedCounterName = "MsalFailure";
        private const string TotalDurationHistogramName = "MsalTotalDuration.1A";
        private const string DurationInL1CacheHistogramName = "MsalDurationInL1CacheInUs.1B";
        private const string DurationInL2CacheHistogramName = "MsalDurationInL2Cache.1A";
        private const string DurationInHttpHistogramName = "MsalDurationInHttp.1A";
        private const string DurationInExtensionInMsHistogram = "MsalDurationInExtensionInMs.1B";

        /// <summary>
        /// Meter to hold the MSAL metrics.
        /// </summary>
        internal static readonly Meter Meter = new Meter(MeterName, "1.0.0");

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
        internal static readonly Lazy<Histogram<long>> s_durationTotal = new(() => Meter.CreateHistogram<long>(
            TotalDurationHistogramName,
            unit: "ms",
            description: "Performance of token acquisition calls total latency"));

        /// <summary>
        /// Histogram to record total duration of token acquisition calls in microseconds(us) when token is fetched from L1 cache.
        /// </summary>
        internal static readonly Lazy<Histogram<long>> s_durationInL1CacheInUs = new(() => Meter.CreateHistogram<long>(
            DurationInL1CacheHistogramName,
            unit: "us",
            description: "Performance of token acquisition calls total latency in microseconds when L1 cache is used."));

        /// <summary>
        /// Histogram to record duration in L2 cache for token acquisition calls.
        /// </summary>
        internal static readonly Lazy<Histogram<long>> s_durationInL2Cache = new Lazy<Histogram<long>>(() => Meter.CreateHistogram<long>(
            DurationInL2CacheHistogramName,
            unit: "ms",
            description: "Performance of token acquisition calls cache latency"));

        /// <summary>
        /// Histogram to record duration in milliseconds in http when the token is fetched from identity provider.
        /// </summary>
        internal static readonly Lazy<Histogram<long>> s_durationInHttp = new Lazy<Histogram<long>>(() => Meter.CreateHistogram<long>(
            DurationInHttpHistogramName,
            unit: "ms",
            description: "Performance of token acquisition calls network latency"));

        /// <summary>
        /// Histogram to record total duration of extension modifications in microseconds(us).
        /// </summary>
        internal static readonly Lazy<Histogram<long>> s_durationInExtensionInMs = new(() => Meter.CreateHistogram<long>(
            DurationInExtensionInMsHistogram,
            unit: "us",
            description: "Performance of token acquisition calls extension latency."));

        public OtelInstrumentation()
        {
            // Needed to fail fast if the runtime, like in-process Azure Functions, doesn't support OpenTelemetry 
            _ = Meter.Version;
        }

        // Aggregates the successful requests based on token source and cache refresh reason.
        public void LogSuccessMetrics(
            string platform,
            ApiEvent.ApiIds apiId,
            string callerSdkId,
            string callerSdkVersion,
            CacheLevel cacheLevel,
            long totalDurationInUs,
            AuthenticationResultMetadata authResultMetadata,
            ILoggerAdapter logger)
        {
            IncrementSuccessCounter(
                platform,
                apiId,
                callerSdkId,
                callerSdkVersion,
                authResultMetadata.TokenSource,
                authResultMetadata.CacheRefreshReason,
                cacheLevel,
                logger);

            if (s_durationTotal.Value.Enabled)
            {
                s_durationTotal.Value.Record(authResultMetadata.DurationTotalInMs,
                        new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                        new(TelemetryConstants.Platform, platform),
                        new(TelemetryConstants.ApiId, apiId),
                        new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                        new(TelemetryConstants.CacheLevel, cacheLevel), 
                        new(TelemetryConstants.CacheRefreshReason, authResultMetadata.CacheRefreshReason)); 
            }

            // Only log cache duration if L2 cache was used.
            if (s_durationInL2Cache.Value.Enabled && cacheLevel == CacheLevel.L2Cache)
            {
                s_durationInL2Cache.Value.Record(authResultMetadata.DurationInCacheInMs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.CacheRefreshReason, authResultMetadata.CacheRefreshReason));
            }

            // Only log duration in HTTP when token is fetched from IDP
            if (s_durationInHttp.Value.Enabled && authResultMetadata.TokenSource == TokenSource.IdentityProvider)
            {
                s_durationInHttp.Value.Record(authResultMetadata.DurationInHttpInMs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId));
            }

            // Only log duration in microseconds when the cache level is L1.
            if (s_durationInL1CacheInUs.Value.Enabled && authResultMetadata.TokenSource == TokenSource.Cache
                && authResultMetadata.CacheLevel.Equals(CacheLevel.L1Cache))
            {
                s_durationInL1CacheInUs.Value.Record(totalDurationInUs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                new(TelemetryConstants.CacheLevel, authResultMetadata.CacheLevel),
                new(TelemetryConstants.CacheRefreshReason, authResultMetadata.CacheRefreshReason));
            }

            if (s_durationInExtensionInMs.Value.Enabled && authResultMetadata.DurationCreatingExtendedTokenInUs > 0)
            {
                s_durationInExtensionInMs.Value.Record(authResultMetadata.DurationCreatingExtendedTokenInUs,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                new(TelemetryConstants.CacheLevel, authResultMetadata.CacheLevel));
            }
        }

        public void IncrementSuccessCounter(string platform, 
            ApiEvent.ApiIds apiId, 
            string callerSdkId,
            string callerSdkVersion,
            TokenSource tokenSource,
            CacheRefreshReason cacheRefreshReason, 
            CacheLevel cacheLevel,
            ILoggerAdapter logger)
        {
            if (s_successCounter.Value.Enabled)
            {
                s_successCounter.Value.Add(1,
                        new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                        new(TelemetryConstants.Platform, platform),
                        new(TelemetryConstants.ApiId, apiId),
                        new(TelemetryConstants.CallerSdkId, callerSdkId ?? string.Empty),
                        new(TelemetryConstants.CallerSdkVersion, callerSdkVersion ?? string.Empty),
                        new(TelemetryConstants.TokenSource, tokenSource),
                        new(TelemetryConstants.CacheRefreshReason, cacheRefreshReason),
                        new(TelemetryConstants.CacheLevel, cacheLevel));
                logger.Verbose(() => "[OpenTelemetry] Completed incrementing to success counter.");
            }
        }

        public void LogFailureMetrics(string platform, 
            string errorCode, 
            ApiEvent.ApiIds apiId,
            string callerSdkId,
            string callerSdkVersion,
            CacheRefreshReason cacheRefreshReason)
        {
            if (s_failureCounter.Value.Enabled)
            {
                s_failureCounter.Value.Add(1,
                        new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                        new(TelemetryConstants.Platform, platform),
                        new(TelemetryConstants.ErrorCode, errorCode), 
                        new(TelemetryConstants.ApiId, apiId),
                        new(TelemetryConstants.CallerSdkId, callerSdkId ?? ""),
                        new(TelemetryConstants.CallerSdkVersion, callerSdkVersion ?? ""),
                        new(TelemetryConstants.CacheRefreshReason, cacheRefreshReason)); 
            }
        }
    }
}

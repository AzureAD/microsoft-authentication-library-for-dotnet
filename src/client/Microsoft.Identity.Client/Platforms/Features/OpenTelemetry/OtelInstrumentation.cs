// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.TelemetryCore.OpenTelemetry;

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

        internal const string EnableExtendedTokenMetricsEnvVariable = "MSAL_ENABLE_EXTENDED_TOKEN_METRICS";

        // Captured at construction time (once per app build, mirroring MSAL_FORCE_REGION semantics).
        // Changing the env var after the OtelInstrumentation instance is created has no effect on that instance.
        private readonly bool _isExtendedMetricsEnabled;

        private static bool ReadExtendedMetricsEnvVar()
        {
            string value = Environment.GetEnvironmentVariable(EnableExtendedTokenMetricsEnvVariable);
            return !string.IsNullOrEmpty(value) &&
                (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                 value.Equals("true", StringComparison.OrdinalIgnoreCase));
        }

        private const string SuccessCounterName = "MsalSuccess";
        private const string FailedCounterName = "MsalFailure";
        private const string TotalDurationHistogramName = "MsalTotalDuration.1A";
        private const string DurationInL1CacheHistogramName = "MsalDurationInL1CacheInUs.1B";
        private const string DurationInL2CacheHistogramName = "MsalDurationInL2Cache.1A";
        private const string DurationInHttpHistogramName = "MsalDurationInHttp.1A";
        private const string DurationInExtensionInMsHistogram = "MsalDurationInExtensionInMs.1B";
        private const string RemainingTokenLifetimeHistogramName = "MsalRemainingTokenLifetime.1A";
        private const string TotalDurationV2HistogramName = "MsalTotalDurationV2.1A";
        private const string DurationInHttpV2HistogramName = "MsalDurationInHttpV2.1A";

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
        /// Histogram to record total duration in milliseconds of token acquisition calls, covering both successes and failures.
        /// Emitted only when extended metrics are enabled via the MSAL_ENABLE_EXTENDED_TOKEN_METRICS environment variable.
        /// </summary>
        internal static readonly Lazy<Histogram<long>> s_durationTotalV2 = new(() => Meter.CreateHistogram<long>(
            TotalDurationV2HistogramName,
            unit: "ms",
            description: "Performance of token acquisition calls total latency including both successes and failures"));

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
        /// Histogram to record duration in milliseconds in http when the token is fetched from identity provider, covering both successes and failures.
        /// Emitted only when extended metrics are enabled via the MSAL_ENABLE_EXTENDED_TOKEN_METRICS environment variable.
        /// </summary>
        internal static readonly Lazy<Histogram<long>> s_durationInHttpV2 = new(() => Meter.CreateHistogram<long>(
            DurationInHttpV2HistogramName,
            unit: "ms",
            description: "Performance of token acquisition calls network latency including both successes and failures"));

        /// <summary>
        /// Histogram to record total duration of extension modifications in microseconds(us).
        /// </summary>
        internal static readonly Lazy<Histogram<long>> s_durationInExtensionInMs = new(() => Meter.CreateHistogram<long>(
            DurationInExtensionInMsHistogram,
            unit: "us",
            description: "Performance of token acquisition calls extension latency."));

        /// <summary>
        /// Histogram to record the remaining lifetime of acquired tokens in seconds.
        /// </summary>
        internal static readonly Lazy<Histogram<long>> s_remainingTokenLifetime = new(() => Meter.CreateHistogram<long>(
            RemainingTokenLifetimeHistogramName,
            unit: "s",
            description: "Remaining lifetime of acquired tokens at the time of acquisition."));

        public OtelInstrumentation()
        {
            // Needed to fail fast if the runtime, like in-process Azure Functions, doesn't support OpenTelemetry
            _ = Meter.Version;

            _isExtendedMetricsEnabled = ReadExtendedMetricsEnvVar();
        }

        // Combined MsalVersion + Platform tag value for V2 histograms and MsalRemainingTokenLifetime.
        // Reduces dimension count vs. emitting them separately; downstream consumers split on ",".
        private static string MsalVersionPlatformTag(string platform) =>
            $"{MsalIdHelper.GetMsalVersion()},{platform}";

        // Builds the final TagList for a metric by appending the caller-supplied extra tags (if any) after
        // MSAL's canonical base tags. The extra tags are materialized once per acquisition by the caller
        // (see OtelEnrichmentHelper.MaterializeExtraTags) and the same fixed set is merged into every
        // instrument, so this method never invokes the enricher and never throws on its behalf. The
        // canonical base tags are emitted first and are protected: an extra tag with a null/empty key is
        // skipped, and an extra tag whose key collides with a canonical base tag key is dropped (a duplicate
        // key would otherwise shadow MSAL's canonical value in last-wins backends). So the canonical metric
        // set cannot be removed, overridden, or altered by the enricher.
        private static TagList BuildTagList(
            IReadOnlyList<KeyValuePair<string, object>> extraTags,
            params KeyValuePair<string, object>[] baseTags)
        {
            if (extraTags == null || extraTags.Count == 0)
            {
                return new TagList(baseTags);
            }

            var tagList = new TagList();
            var baseKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object> tag in baseTags)
            {
                tagList.Add(tag);
                baseKeys.Add(tag.Key);
            }

            foreach (KeyValuePair<string, object> tag in extraTags)
            {
                // Never let a caller-supplied tag remove, override, or corrupt a canonical tag:
                // drop tags with a null/empty key and tags whose key collides with a base tag key.
                if (string.IsNullOrEmpty(tag.Key) || baseKeys.Contains(tag.Key))
                {
                    continue;
                }

                tagList.Add(tag);
            }

            return tagList;
        }

        // Aggregates the successful requests based on token source and cache refresh reason.
        // Counter, L1, L2, and extension are always emitted.
        // When the MSAL_ENABLE_EXTENDED_TOKEN_METRICS env var is not set: V1 total duration and V1 HTTP duration are emitted.
        // When it is set: V2 total duration (Succeeded=true) and V2 HTTP duration (HttpStatusCode=200) are emitted instead.
        public void LogSuccessMetrics(
            string platform,
            ApiEvent.ApiIds apiId,
            string callerSdkId,
            string callerSdkVersion,
            CacheLevel cacheLevel,
            long totalDurationInUs,
            AuthenticationResultMetadata authResultMetadata,
            ILoggerAdapter logger,
            DateTimeOffset expiresOn,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null)
        {
            IncrementSuccessCounter(
                platform,
                apiId,
                callerSdkId,
                callerSdkVersion,
                authResultMetadata.TokenSource,
                authResultMetadata.CacheRefreshReason,
                cacheLevel,
                logger,
                authResultMetadata.TelemetryTokenType,
                extraTags);

            if (s_durationInL1CacheInUs.Value.Enabled && authResultMetadata.TokenSource == TokenSource.Cache
                && authResultMetadata.CacheLevel.Equals(CacheLevel.L1Cache))
            {
                var tags = BuildTagList(extraTags,
                    new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                    new(TelemetryConstants.Platform, platform),
                    new(TelemetryConstants.ApiId, apiId),
                    new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                    new(TelemetryConstants.CacheLevel, authResultMetadata.CacheLevel),
                    new(TelemetryConstants.CacheRefreshReason, authResultMetadata.CacheRefreshReason));
                s_durationInL1CacheInUs.Value.Record(totalDurationInUs, in tags);
            }

            // Only log cache duration if L2 cache was used.
            if (s_durationInL2Cache.Value.Enabled && cacheLevel == CacheLevel.L2Cache)
            {
                var tags = BuildTagList(extraTags,
                    new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                    new(TelemetryConstants.Platform, platform),
                    new(TelemetryConstants.ApiId, apiId),
                    new(TelemetryConstants.CacheRefreshReason, authResultMetadata.CacheRefreshReason));
                s_durationInL2Cache.Value.Record(authResultMetadata.DurationInCacheInMs, in tags);
            }

            if (s_durationInExtensionInMs.Value.Enabled)
            {
                var tags = BuildTagList(extraTags,
                    new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                    new(TelemetryConstants.Platform, platform),
                    new(TelemetryConstants.ApiId, apiId),
                    new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                    new(TelemetryConstants.CacheLevel, authResultMetadata.CacheLevel),
                    new(TelemetryConstants.TokenType, authResultMetadata.TelemetryTokenType));
                s_durationInExtensionInMs.Value.Record(authResultMetadata.DurationCreatingExtendedTokenInUs, in tags);
            }

            if (!_isExtendedMetricsEnabled)
            {
                if (s_durationTotal.Value.Enabled)
                {
                    var tags = BuildTagList(extraTags,
                        new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                        new(TelemetryConstants.Platform, platform),
                        new(TelemetryConstants.ApiId, apiId),
                        new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                        new(TelemetryConstants.CacheLevel, cacheLevel),
                        new(TelemetryConstants.CacheRefreshReason, authResultMetadata.CacheRefreshReason),
                        new(TelemetryConstants.TokenType, authResultMetadata.TelemetryTokenType));
                    s_durationTotal.Value.Record(authResultMetadata.DurationTotalInMs, in tags);
                }

                // Only log duration in HTTP when token is fetched from IDP.
                if (s_durationInHttp.Value.Enabled && authResultMetadata.TokenSource == TokenSource.IdentityProvider)
                {
                    var tags = BuildTagList(extraTags,
                        new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                        new(TelemetryConstants.Platform, platform),
                        new(TelemetryConstants.ApiId, apiId),
                        new(TelemetryConstants.TokenType, authResultMetadata.TelemetryTokenType));
                    s_durationInHttp.Value.Record(authResultMetadata.DurationInHttpInMs, in tags);
                }
            }
            else
            {
                if (s_durationTotalV2.Value.Enabled)
                {
                    var tags = BuildTagList(extraTags,
                        new(TelemetryConstants.MsalVersionPlatform, MsalVersionPlatformTag(platform)),
                        new(TelemetryConstants.ApiId, apiId),
                        new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                        new(TelemetryConstants.CacheLevel, cacheLevel),
                        new(TelemetryConstants.CacheRefreshReason, authResultMetadata.CacheRefreshReason),
                        new(TelemetryConstants.TokenType, authResultMetadata.TelemetryTokenType),
                        new(TelemetryConstants.ErrorCode, string.Empty),
                        new(TelemetryConstants.Succeeded, true));
                    s_durationTotalV2.Value.Record(authResultMetadata.DurationTotalInMs, in tags);
                }

                // Only log duration in HTTP when token is fetched from IDP.
                if (s_durationInHttpV2.Value.Enabled && authResultMetadata.TokenSource == TokenSource.IdentityProvider)
                {
                    var tags = BuildTagList(extraTags,
                        new(TelemetryConstants.MsalVersionPlatform, MsalVersionPlatformTag(platform)),
                        new(TelemetryConstants.ApiId, apiId),
                        new(TelemetryConstants.TokenType, authResultMetadata.TelemetryTokenType),
                        new(TelemetryConstants.HttpStatusCode, 200));
                    s_durationInHttpV2.Value.Record(authResultMetadata.DurationInHttpInMs, in tags);
                }
            }

            LogRemainingTokenLifetime(
                platform,
                apiId,
                authResultMetadata.TokenSource,
                cacheLevel,
                authResultMetadata.CacheRefreshReason,
                authResultMetadata.TelemetryTokenType,
                expiresOn,
                logger,
                extraTags);
        }

        public void IncrementSuccessCounter(string platform,
            ApiEvent.ApiIds apiId,
            string callerSdkId,
            string callerSdkVersion,
            TokenSource tokenSource,
            CacheRefreshReason cacheRefreshReason,
            CacheLevel cacheLevel,
            ILoggerAdapter logger,
            int tokenType,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null)
        {
            if (s_successCounter.Value.Enabled)
            {
                var tags = BuildTagList(extraTags,
                        new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                        new(TelemetryConstants.Platform, platform),
                        new(TelemetryConstants.ApiId, apiId),
                        new(TelemetryConstants.CallerSdkId, callerSdkId ?? string.Empty + "," + callerSdkVersion ?? string.Empty),
                        new(TelemetryConstants.TokenSource, tokenSource),
                        new(TelemetryConstants.CacheRefreshReason, cacheRefreshReason),
                        new(TelemetryConstants.CacheLevel, cacheLevel),
                        new(TelemetryConstants.TokenType, tokenType));
                s_successCounter.Value.Add(1, in tags);
                logger.Verbose(() => "[OpenTelemetry] Completed incrementing to success counter.");
            }
        }

        public void LogSuccessHttpDuration(
            string platform,
            ApiEvent.ApiIds apiId,
            AuthenticationResultMetadata authResultMetadata,
            ILoggerAdapter logger = null,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null)
        {
            if (authResultMetadata.TokenSource != TokenSource.IdentityProvider)
                return;

            if (!_isExtendedMetricsEnabled)
            {
                if (s_durationInHttp.Value.Enabled)
                {
                    var tags = BuildTagList(extraTags,
                        new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                        new(TelemetryConstants.Platform, platform),
                        new(TelemetryConstants.ApiId, apiId),
                        new(TelemetryConstants.TokenType, authResultMetadata.TelemetryTokenType));
                    s_durationInHttp.Value.Record(authResultMetadata.DurationInHttpInMs, in tags);
                }
            }
            else
            {
                if (s_durationInHttpV2.Value.Enabled)
                {
                    var tags = BuildTagList(extraTags,
                        new(TelemetryConstants.MsalVersionPlatform, MsalVersionPlatformTag(platform)),
                        new(TelemetryConstants.ApiId, apiId),
                        new(TelemetryConstants.TokenType, authResultMetadata.TelemetryTokenType),
                        new(TelemetryConstants.HttpStatusCode, 200));
                    s_durationInHttpV2.Value.Record(authResultMetadata.DurationInHttpInMs, in tags);
                }
            }
        }

        // Foreground failure path: increments the failure counter, records V2 total duration,
        // and records V2 HTTP duration. Mirrors LogSuccessMetrics on the success side.
        public void LogFailureMetrics(
            string platform,
            string errorCode,
            ApiEvent apiEvent,
            string callerSdkId,
            string callerSdkVersion,
            CacheRefreshReason cacheRefreshReason,
            int tokenType,
            int httpStatusCode,
            long totalDurationInMs,
            string rawStsErrorCode = null,
            ILoggerAdapter logger = null,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null)
        {
            IncrementFailureCounter(
                platform,
                errorCode,
                apiEvent.ApiId,
                callerSdkId,
                callerSdkVersion,
                cacheRefreshReason,
                tokenType,
                rawStsErrorCode,
                logger,
                extraTags);

            if (_isExtendedMetricsEnabled && s_durationTotalV2.Value.Enabled)
            {
                // TokenSource is empty on failure: no token was acquired, so no source applies.
                var tags = BuildTagList(extraTags,
                    new(TelemetryConstants.MsalVersionPlatform, MsalVersionPlatformTag(platform)),
                    new(TelemetryConstants.ApiId, apiEvent.ApiId),
                    new(TelemetryConstants.TokenSource, string.Empty),
                    new(TelemetryConstants.CacheLevel, string.Empty),
                    new(TelemetryConstants.CacheRefreshReason, apiEvent.CacheInfo),
                    new(TelemetryConstants.TokenType, apiEvent.TokenType),
                    new(TelemetryConstants.ErrorCode, errorCode),
                    new(TelemetryConstants.Succeeded, false));
                s_durationTotalV2.Value.Record(totalDurationInMs, in tags);
            }

            LogFailureHttpDuration(platform, apiEvent, httpStatusCode, logger, extraTags);
        }

        public void IncrementFailureCounter(
            string platform,
            string errorCode,
            ApiEvent.ApiIds apiId,
            string callerSdkId,
            string callerSdkVersion,
            CacheRefreshReason cacheRefreshReason,
            int tokenType,
            string rawStsErrorCode = null,
            ILoggerAdapter logger = null,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null)
        {
            if (!s_failureCounter.Value.Enabled)
                return;

            var baseTags = new List<KeyValuePair<string, object>>
            {
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ErrorCode, errorCode),
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.CallerSdkId, callerSdkId ?? string.Empty + "," + callerSdkVersion ?? string.Empty),
                new(TelemetryConstants.CacheRefreshReason, cacheRefreshReason),
                new(TelemetryConstants.TokenType, tokenType)
            };

            if (!string.IsNullOrEmpty(rawStsErrorCode))
                baseTags.Add(new(TelemetryConstants.RawStsErrorCode, rawStsErrorCode));

            var tags = BuildTagList(extraTags, baseTags.ToArray());

            s_failureCounter.Value.Add(1, in tags);
        }

        // Records V2 HTTP duration on the failure path. V1 has no failure HTTP histogram,
        // so this is a no-op when extended metrics are disabled. Mirrors LogSuccessHttpDuration.
        // Recorded whenever an HTTP exchange produced measurable duration — includes responses
        // (HttpStatusCode > 0) and pre-response failures like cancellations and connection
        // errors (HttpStatusCode = 0), which operators can separate via the HttpStatusCode tag.
        public void LogFailureHttpDuration(
            string platform,
            ApiEvent apiEvent,
            int httpStatusCode,
            ILoggerAdapter logger = null,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null)
        {
            if (!_isExtendedMetricsEnabled || !s_durationInHttpV2.Value.Enabled)
                return;

            if (apiEvent.DurationInHttpInMs > 0)
            {
                var tags = BuildTagList(extraTags,
                    new(TelemetryConstants.MsalVersionPlatform, MsalVersionPlatformTag(platform)),
                    new(TelemetryConstants.ApiId, apiEvent.ApiId),
                    new(TelemetryConstants.TokenType, apiEvent.TokenType),
                    new(TelemetryConstants.HttpStatusCode, httpStatusCode));
                s_durationInHttpV2.Value.Record(apiEvent.DurationInHttpInMs, in tags);
            }
        }

        public void LogRemainingTokenLifetime(
            string platform,
            ApiEvent.ApiIds apiId,
            TokenSource tokenSource,
            CacheLevel cacheLevel,
            CacheRefreshReason cacheRefreshReason,
            int tokenType,
            DateTimeOffset expiresOn,
            ILoggerAdapter logger = null,
            IReadOnlyList<KeyValuePair<string, object>> extraTags = null)
        {
            if (s_remainingTokenLifetime.Value.Enabled)
            {
                long remainingSeconds = Math.Max(0, (long)(expiresOn - DateTimeOffset.UtcNow).TotalSeconds);

                var tags = BuildTagList(extraTags,
                    new(TelemetryConstants.MsalVersionPlatform, MsalVersionPlatformTag(platform)),
                    new(TelemetryConstants.ApiId, apiId),
                    new(TelemetryConstants.TokenSource, tokenSource),
                    new(TelemetryConstants.CacheLevel, cacheLevel),
                    new(TelemetryConstants.CacheRefreshReason, cacheRefreshReason),
                    new(TelemetryConstants.TokenType, tokenType));
                s_remainingTokenLifetime.Value.Record(remainingSeconds, in tags);
            }
        }
    }
}

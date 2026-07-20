// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Describes the OpenTelemetry metrics that MSAL emits and, for each metric, the canonical
    /// (MSAL-owned) tag names it records. Consumers such as downstream metric pipelines can use
    /// <see cref="CanonicalTagsByMetric"/> to discover which tags belong to a given MSAL metric and,
    /// for example, keep only those tags.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tag names listed for a metric are the canonical base tags MSAL emits for it (some are conditional). They do not
    /// include any extra tags supplied through
    /// <see cref="AbstractConfidentialClientAcquireTokenParameterBuilderExtension.WithOtelTagsEnricher{T}"/>,
    /// which are caller-defined and therefore not part of this catalog. Some canonical tags are emitted only
    /// under certain conditions (for example the raw STS error-code tag is present on the failure counter only
    /// when the STS returns a sub-error); they are listed here because they are part of the metric's canonical
    /// schema.
    /// </para>
    /// <para>
    /// Both the metric names (the dictionary keys) and the tag names (the dictionary values) are plain strings
    /// that match what MSAL records, so consumers can compare them directly against the metric and tag names
    /// observed on the OpenTelemetry pipeline, then drive per-metric tag filtering from this mapping (for
    /// example via OpenTelemetry Views and <c>MetricStreamConfiguration.TagKeys</c>). Keeping the mapping here
    /// means it stays in sync with MSAL automatically: updating the MSAL package reference updates the canonical
    /// tag set, with nothing to maintain on the consumer side.
    /// </para>
    /// </remarks>
    public static class MsalMetricsCatalog
    {
        // Metric names are defined once here (internal) so the published catalog and the instruments created in
        // OtelInstrumentation share a single source of truth and cannot drift. They are intentionally not part of
        // the public surface: consumers read metric names from the keys of CanonicalTagsByMetric (and from the
        // OpenTelemetry pipeline), so there is no need to expose them as separate public constants.
        internal const string SuccessCounterName = "MsalSuccess";
        internal const string FailureCounterName = "MsalFailure";
        internal const string TotalDurationHistogramName = "MsalTotalDuration.1A";
        internal const string TotalDurationV2HistogramName = "MsalTotalDurationV2.1A";
        internal const string DurationInL1CacheHistogramName = "MsalDurationInL1CacheInUs.1B";
        internal const string DurationInL2CacheHistogramName = "MsalDurationInL2Cache.1A";
        internal const string DurationInHttpHistogramName = "MsalDurationInHttp.1A";
        internal const string DurationInHttpV2HistogramName = "MsalDurationInHttpV2.1A";
        internal const string DurationInExtensionHistogramName = "MsalDurationInExtensionInMs.1B";
        internal const string RemainingTokenLifetimeHistogramName = "MsalRemainingTokenLifetime.1A";

        /// <summary>
        /// Maps each MSAL metric name to the read-only list of canonical tag names that metric records.
        /// Keys are compared with <see cref="System.StringComparer.Ordinal"/>.
        /// </summary>
        public static IReadOnlyDictionary<string, IReadOnlyList<string>> CanonicalTagsByMetric { get; } =
            BuildCanonicalTagsByMetric();

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildCanonicalTagsByMetric()
        {
            // Tag names reference the internal TelemetryConstants used when recording, so the mapping cannot
            // drift from the tags MSAL actually emits.
            var map = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [SuccessCounterName] = new[]
                {
                    TelemetryConstants.MsalVersion,
                    TelemetryConstants.Platform,
                    TelemetryConstants.ApiId,
                    TelemetryConstants.CallerSdkId,
                    TelemetryConstants.TokenSource,
                    TelemetryConstants.CacheRefreshReason,
                    TelemetryConstants.CacheLevel,
                    TelemetryConstants.TokenType,
                },
                [FailureCounterName] = new[]
                {
                    TelemetryConstants.MsalVersion,
                    TelemetryConstants.Platform,
                    TelemetryConstants.ErrorCode,
                    TelemetryConstants.ApiId,
                    TelemetryConstants.CallerSdkId,
                    TelemetryConstants.CacheRefreshReason,
                    TelemetryConstants.TokenType,
                    TelemetryConstants.RawStsErrorCode,
                },
                [TotalDurationHistogramName] = new[]
                {
                    TelemetryConstants.MsalVersion,
                    TelemetryConstants.Platform,
                    TelemetryConstants.ApiId,
                    TelemetryConstants.TokenSource,
                    TelemetryConstants.CacheLevel,
                    TelemetryConstants.CacheRefreshReason,
                    TelemetryConstants.TokenType,
                },
                [TotalDurationV2HistogramName] = new[]
                {
                    TelemetryConstants.MsalVersionPlatform,
                    TelemetryConstants.ApiId,
                    TelemetryConstants.TokenSource,
                    TelemetryConstants.CacheLevel,
                    TelemetryConstants.CacheRefreshReason,
                    TelemetryConstants.TokenType,
                    TelemetryConstants.ErrorCode,
                    TelemetryConstants.Succeeded,
                },
                [DurationInL1CacheHistogramName] = new[]
                {
                    TelemetryConstants.MsalVersion,
                    TelemetryConstants.Platform,
                    TelemetryConstants.ApiId,
                    TelemetryConstants.TokenSource,
                    TelemetryConstants.CacheLevel,
                    TelemetryConstants.CacheRefreshReason,
                },
                [DurationInL2CacheHistogramName] = new[]
                {
                    TelemetryConstants.MsalVersion,
                    TelemetryConstants.Platform,
                    TelemetryConstants.ApiId,
                    TelemetryConstants.CacheRefreshReason,
                },
                [DurationInHttpHistogramName] = new[]
                {
                    TelemetryConstants.MsalVersion,
                    TelemetryConstants.Platform,
                    TelemetryConstants.ApiId,
                    TelemetryConstants.TokenType,
                },
                [DurationInHttpV2HistogramName] = new[]
                {
                    TelemetryConstants.MsalVersionPlatform,
                    TelemetryConstants.ApiId,
                    TelemetryConstants.TokenType,
                    TelemetryConstants.HttpStatusCode,
                },
                [DurationInExtensionHistogramName] = new[]
                {
                    TelemetryConstants.MsalVersion,
                    TelemetryConstants.Platform,
                    TelemetryConstants.ApiId,
                    TelemetryConstants.TokenSource,
                    TelemetryConstants.CacheLevel,
                    TelemetryConstants.TokenType,
                },
                [RemainingTokenLifetimeHistogramName] = new[]
                {
                    TelemetryConstants.MsalVersionPlatform,
                    TelemetryConstants.ApiId,
                    TelemetryConstants.TokenSource,
                    TelemetryConstants.CacheLevel,
                    TelemetryConstants.CacheRefreshReason,
                    TelemetryConstants.TokenType,
                },
            };

            // Expose each tag list as a ReadOnlyCollection rather than the backing array: an IReadOnlyList that is
            // actually a string[] can be cast back and mutated by a consumer, which would corrupt this shared
            // static catalog process-wide. The dictionary itself is already read-only.
            var readOnlyMap = new Dictionary<string, IReadOnlyList<string>>(map.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, string[]> entry in map)
            {
                readOnlyMap[entry.Key] = Array.AsReadOnly(entry.Value);
            }

            return new ReadOnlyDictionary<string, IReadOnlyList<string>>(readOnlyMap);
        }
    }
}

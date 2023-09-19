// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
#if NETSTANDARD || NET6_0 || NET462
using System.Diagnostics.Metrics;
#endif
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.TelemetryCore.OpenTelemetry
{
    /// <summary>
    /// Class to hold the OpenTelemetry objects used by MSAL.
    /// </summary>
    internal class OpenTelemetryClient : IOpenTelemetryClient

    {
        /// <summary>
        /// Constant to hold the name of the Meter.
        /// </summary>
        public const string MeterName = "ID4S_MSAL";

        /// <summary>
        /// Constant to holt the name of the ActivitySource.
        /// </summary>
        public const string ActivitySourceName = "MSAL_Activity";

#if NETSTANDARD || NET6_0 || NET462
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

        internal static readonly Lazy<Activity> s_activity = new Lazy<Activity>(() => s_acquireTokenActivity.StartActivity("Token Acquisition", ActivityKind.Internal));
#endif

        void IOpenTelemetryClient.LogActivity(Dictionary<string, object> tags)
        {
#if NETSTANDARD || NET6_0 || NET462
            foreach (KeyValuePair<string, object> tag in tags)
            {
                s_activity.Value?.AddTag(tag.Key, tag.Value);
            }
#endif
        }

        void IOpenTelemetryClient.LogActivityStatus(bool success)
        {
#if NETSTANDARD || NET6_0 || NET462
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

        void IOpenTelemetryClient.StopActivity()
        {
#if NETSTANDARD || NET6_0 || NET462
            s_activity.Value?.Stop();
#endif
        }

        void IOpenTelemetryClient.LogSuccessMetrics(string platform, string clientId, AuthenticationResultMetadata authResultMetadata, string apiId, string cacheLevel)
        {
#if NETSTANDARD || NET6_0 || NET462
            s_successCounter.Value.Add(1,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ClientId, clientId),
                new(TelemetryConstants.ApiId, apiId),
                new(TelemetryConstants.TokenSource, authResultMetadata.TokenSource),
                new(TelemetryConstants.CacheInfoTelemetry, authResultMetadata.CacheRefreshReason),
                new(TelemetryConstants.CacheLevel, cacheLevel));

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
#endif
        }

        void IOpenTelemetryClient.LogFailedMetrics(string platform, string clientId, string errorCode)
        {
#if NETSTANDARD || NET6_0 || NET462
            s_failureCounter.Value.Add(1,
                new(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion()),
                new(TelemetryConstants.Platform, platform),
                new(TelemetryConstants.ClientId, clientId),
                new(TelemetryConstants.ErrorCode, errorCode));
#endif
        }
    }
}

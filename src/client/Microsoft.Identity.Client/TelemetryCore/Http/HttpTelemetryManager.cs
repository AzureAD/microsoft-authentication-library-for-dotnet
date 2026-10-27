// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore.Http
{
    /// <summary>
    /// Responsible for recording API events and formatting CSV 
    /// with telemetry.
    /// </summary>
    internal class HttpTelemetryManager : IHttpTelemetryManager
    {
        private readonly object _coalescingLock = new object();
        private long _coalescedRequests;
        private long _successfulAcquisitions;

        public void RecordCoalescedRequest(CacheRefreshReason initialReason)
        {
            if (IsCoalescingReason(initialReason))
            {
                lock (_coalescingLock)
                {
                    _coalescedRequests++;
                }
            }
        }

        public void RecordSuccessfulAcquisition(CacheRefreshReason initialReason)
        {
            if (IsCoalescingReason(initialReason))
            {
                lock (_coalescingLock)
                {
                    _successfulAcquisitions++;
                }
            }
        }

        private static bool IsCoalescingReason(CacheRefreshReason reason) =>
            reason == CacheRefreshReason.Expired ||
            reason == CacheRefreshReason.ProactivelyRefreshed ||
            reason == CacheRefreshReason.NoCachedAccessToken;

        /// <summary>
        /// Clears only this schema's two aggregate fields when an HTTP request is replayed.
        /// Older headers and unrecognized formats are left unchanged.
        /// </summary>
        internal static string ClearCoalescingCounts(string header)
        {
            if (string.IsNullOrEmpty(header) || header.EndsWith(",0,0", StringComparison.Ordinal))
            {
                return header;
            }

            string[] sections = header.Split('|');
            if (sections.Length != 3 || sections[0] != TelemetryConstants.HttpTelemetrySchemaVersion.ToString())
            {
                return header;
            }

            string[] fields = sections[2].Split(',');
            if (fields.Length != 8)
            {
                return header;
            }

            fields[6] = "0";
            fields[7] = "0";
            sections[2] = string.Join(",", fields);
            return string.Join("|", sections);
        }

        /// <summary>
        /// Expected format: 5|api_id,cache_info,region_used,region_autodetection,region_outcome|platform_config
        /// platform_config: is_token_cache_serialized,is_legacy_cache_enabled, token_type, caller_sdk_id, caller_sdk_version, is_managed_cert_used
        /// Eligible refresh reasons append coalesced_requests,successful_acquisitions from prior operations.
        /// </summary>
        public string GetCurrentRequestHeader(ApiEvent eventInProgress)
        {
            if (eventInProgress == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            // Version
            sb.Append(TelemetryConstants.HttpTelemetrySchemaVersion);
            // Main values
            sb.Append(TelemetryConstants.HttpTelemetryPipe);
            sb.Append(eventInProgress.ApiIdString);
            sb.Append(TelemetryConstants.CommaDelimiter);
            sb.Append(eventInProgress.CacheInfoString);
            sb.Append(TelemetryConstants.CommaDelimiter);
            sb.Append(eventInProgress.RegionUsed);
            sb.Append(TelemetryConstants.CommaDelimiter);
            sb.Append(eventInProgress.RegionAutodetectionSourceString);
            sb.Append(TelemetryConstants.CommaDelimiter);
            sb.Append(eventInProgress.RegionOutcomeString);
            // Platform config
            sb.Append(TelemetryConstants.HttpTelemetryPipe);
            sb.Append(eventInProgress.IsTokenCacheSerializedString);
            sb.Append(TelemetryConstants.CommaDelimiter);
            sb.Append(eventInProgress.IsLegacyCacheEnabledString);
            sb.Append(TelemetryConstants.CommaDelimiter);
            // Token type is to indicate 1 - bearer, 2 - pop, 3 - ssh-cert, 4 - external.
            sb.Append(eventInProgress.TokenType);
            sb.Append(TelemetryConstants.CommaDelimiter);
            sb.Append(eventInProgress.CallerSdkApiId);
            sb.Append(TelemetryConstants.CommaDelimiter);
            sb.Append(eventInProgress.CallerSdkVersion);
            sb.Append(TelemetryConstants.CommaDelimiter);
            if (eventInProgress.IsManagedCertUsed != '\0')
            {
                sb.Append(eventInProgress.IsManagedCertUsed);
            }

            if (IsCoalescingReason(eventInProgress.CacheInfo))
            {
                long coalesced;
                long acquired;
                lock (_coalescingLock)
                {
                    coalesced = _coalescedRequests;
                    acquired = _successfulAcquisitions;
                    _coalescedRequests = 0;
                    _successfulAcquisitions = 0;
                }

                sb.Append(TelemetryConstants.CommaDelimiter);
                sb.Append(coalesced.ToString(CultureInfo.InvariantCulture));
                sb.Append(TelemetryConstants.CommaDelimiter);
                sb.Append(acquired.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }
    }
}

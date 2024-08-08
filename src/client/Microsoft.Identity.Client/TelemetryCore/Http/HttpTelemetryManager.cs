// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore.Http
{
    /// <summary>
    /// Responsible for recording API events and formatting CSV 
    /// with telemetry.
    /// </summary>
    /// <remarks>
    /// Not fully thread safe - it is possible that multiple threads request
    /// the "previous requests" data at the same time. It is the responsibility of 
    /// the caller to protect against this.
    /// </remarks>
    internal class HttpTelemetryManager : IHttpTelemetryManager
    {
        /// <summary>
        /// Expected format: 5|api_id,cache_info,region_used,region_autodetection,region_outcome|platform_config
        /// platform_config: is_token_cache_serialized,is_legacy_cache_enabled, token_type, caller_sdk_id, caller_sdk_version
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
            sb.Append(eventInProgress.TokenTypeString);
            sb.Append(TelemetryConstants.CommaDelimiter);
            sb.Append(eventInProgress.CallerSdkApiId);
            sb.Append(TelemetryConstants.CommaDelimiter);
            sb.Append(eventInProgress.CallerSdkVersion);

            return sb.ToString();
        }
    }
}

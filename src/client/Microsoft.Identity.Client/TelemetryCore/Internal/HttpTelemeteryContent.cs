// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using static Microsoft.Identity.Client.TelemetryCore.TelemetryManager;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal class HttpTelemetryContent
    { 
        private List<string> ApiId { get; set; } = new List<string>();
        private List<string> ErrorCode { get; set; } = new List<string>();

        private string _forceRefresh;
        
        public string GetCsvAsPrevious(
            int successfulSilentCallCount,
            ConcurrentDictionary<string, List<EventBase>> stoppedEvents,
            EventBase mostRecentStoppedApiEvent)
        {
            if (stoppedEvents.Count == 0)
            {
                return string.Empty;
            }

            string apiIdCorIdData = string.Empty;
            string correlationId = string.Empty;

            foreach (List<EventBase> cEvents in stoppedEvents.Values)
            {                
                cEvents.Find(e => e.TryGetValue("Microsoft.MSAL.correlation_id", out correlationId));
                string errorCode = string.Empty;
                cEvents.Find(e => e.TryGetValue(MsalTelemetryBlobEventNames.ApiErrorCodeConstStrKey, out errorCode));
                ErrorCode.Add(errorCode);
            }

            string apiId = string.Empty;
            if (mostRecentStoppedApiEvent != null)
            {
                if (mostRecentStoppedApiEvent.TryGetValue(MsalTelemetryBlobEventNames.ApiIdConstStrKey, out apiId))
                {
                    ApiId.Add(apiId);                   
                }
            }
            apiIdCorIdData = CreateApiIdAndCorrelationIdContent(correlationId);
            // csv expected format:
            // 2|silent_successful_count|failed_requests|errors|platform_fields
            // failed_request can be further expanded to include:
            // api_id_1,correlation_id_1,api_id_2,correlation_id_2|error_1,error_2
            string data = string.Format(CultureInfo.InvariantCulture,
                $"{TelemetryConstants.HttpTelemetrySchemaVersion2}{TelemetryConstants.HttpTelemetryPipe}{successfulSilentCallCount}{TelemetryConstants.HttpTelemetryPipe}" +
                $"{CreateFailedRequestsContent(apiIdCorIdData)}" +
                $"{TelemetryConstants.HttpTelemetryPipe}");

            if (data.Length > 3800)
            {
                ApiId?.Clear();
                ErrorCode?.Clear();
                return string.Empty;
            }

            return data;
        }

        public string GetCsvAsCurrent(ConcurrentDictionary<EventKey, EventBase> eventsInProgress)
        {
            if (eventsInProgress == null)
            {
                return string.Empty;
            }

            IEnumerable<KeyValuePair<EventKey, EventBase>> apiEvent = eventsInProgress.Where(e => e.Key.EventName == "msal.api_event");

            foreach (KeyValuePair<EventKey, EventBase> events in apiEvent)
            {
                events.Value.TryGetValue(MsalTelemetryBlobEventNames.ApiIdConstStrKey, out string apiId);
                ApiId.Add(apiId);
                events.Value.TryGetValue(MsalTelemetryBlobEventNames.ForceRefreshId, out string forceRefresh);
                _forceRefresh = forceRefresh;
            }

            string apiEvents = string.Join(",", ApiId);

            // csv expected format:
            // 2|api_id,force_refresh|platform_config
            string[] myValues = new string[] {
                apiEvents,
                ConvertFromStringToBitwise(_forceRefresh)};

            string csvString = string.Join(",", myValues);
            return $"{TelemetryConstants.HttpTelemetrySchemaVersion2}{TelemetryConstants.HttpTelemetryPipe}{csvString}{TelemetryConstants.HttpTelemetryPipe}";
        }

        private string CreateApiIdAndCorrelationIdContent(string correlationId)
        {
            ApiId.Add(correlationId);
            return string.Join(",", ApiId);
        }

        private string CreateFailedRequestsContent(string apiIdAndCorIdCsv)
        {
            string errorCodeCsv = string.Join(",", ErrorCode);
            string csv = $"{apiIdAndCorIdCsv}{TelemetryConstants.HttpTelemetryPipe}{errorCodeCsv}";
            return csv;
        }

        private string ConvertFromStringToBitwise(string value)
        {
            if (string.IsNullOrEmpty(value) || value == TelemetryConstants.False)
            {
                return TelemetryConstants.Zero;
            }

            return TelemetryConstants.One;
        }
    }
}

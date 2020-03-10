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
        private List<string> CorrelationId { get; set; } = new List<string>();

        private string _forceRefresh;

        public string GetCsvAsPrevious(
            int successfulSilentCallCount,
            EventBase mostRecentStoppedApiEvent)
        {
            if (mostRecentStoppedApiEvent == null || mostRecentStoppedApiEvent.Count == 0)
            {
                return string.Empty;
            }

            CorrelationId.Add(mostRecentStoppedApiEvent[MsalTelemetryBlobEventNames.MsalCorrelationIdConstStrKey]);
            ApiId.Add(mostRecentStoppedApiEvent[MsalTelemetryBlobEventNames.ApiIdConstStrKey]);

            if (mostRecentStoppedApiEvent.ContainsKey(MsalTelemetryBlobEventNames.ApiErrorCodeConstStrKey))
            {
                ErrorCode.Add(mostRecentStoppedApiEvent[MsalTelemetryBlobEventNames.ApiErrorCodeConstStrKey]);
            }

            string apiIdCorIdData = CreateApiIdAndCorrelationIdContent();
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
                ClearData();
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

        private string CreateApiIdAndCorrelationIdContent()
        {
            List<string> combinedApiIdCorrId = new List<string>();
            List<string> apiId = new List<string>();
            apiId = ApiId;

            foreach (var apiIds in ApiId)
            {
                combinedApiIdCorrId.Add(apiIds);

                foreach (var corrId in CorrelationId)
                {
                    combinedApiIdCorrId.Add(corrId);
                }
            }

            return string.Join(",", combinedApiIdCorrId);
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

        internal void ClearData()
        {
            ApiId?.Clear();
            CorrelationId?.Clear();
            ErrorCode?.Clear();
        }
    }
}

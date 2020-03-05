// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using static Microsoft.Identity.Client.TelemetryCore.TelemetryManager;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal class HttpTelemetryContent
    {
        ConcurrentDictionary<string, List<EventBase>> _stoppedEvents = new ConcurrentDictionary<string, List<EventBase>>();
        ConcurrentDictionary<EventKey, EventBase> _eventsInProgress = new ConcurrentDictionary<EventKey, EventBase>();
        private List<string> ApiId { get; set; } = new List<string>();
        private List<string> ErrorCode { get; set; } = new List<string>();

        EventBase _eventBase;
        private string _forceRefresh;
        
        public HttpTelemetryContent(ConcurrentDictionary<string, List<EventBase>> stoppedEvents, EventBase eventBase)
        {
            if (eventBase != null)
            {
                _stoppedEvents = stoppedEvents;
                _eventBase = eventBase;
            }
        }

        public HttpTelemetryContent(ConcurrentDictionary<EventKey, EventBase> eventsInProgress, EventBase eventBase)
        {
            if (eventsInProgress != null)
            {
                _eventsInProgress = eventsInProgress;
                _eventBase = eventBase;
            }
        }

        public string GetCsvAsPrevious(int successfulSilentCallCount)
        {
            if (_stoppedEvents.Count == 0)
            {
                return string.Empty;
            }

            string apiIdCorIdData = string.Empty;

            if (_eventBase.TryGetValue(MsalTelemetryBlobEventNames.MsalCorrelationIdConstStrKey, out string correlationId))
            {
                if (!string.IsNullOrEmpty(correlationId))
                {
                    _eventBase.TryGetValue(MsalTelemetryBlobEventNames.ApiIdConstStrKey, out string apiId);
                    ApiId.Add(apiId);
                    apiIdCorIdData = CreateApiIdAndCorrelationIdContent(correlationId);                    
                }
            }

            _eventBase.TryGetValue(MsalTelemetryBlobEventNames.ApiErrorCodeConstStrKey, out string errorCode);
            ErrorCode.Add(errorCode);
            
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

        public string GetCsvAsCurrent()
        {
            if (_eventsInProgress == null)
            {
                return string.Empty;
            }

            var eventsStuff = _eventsInProgress.FirstOrDefault();

            if (eventsStuff.Value.ContainsKey(MsalTelemetryBlobEventNames.ApiIdConstStrKey))
            {
                eventsStuff.Value.TryGetValue(MsalTelemetryBlobEventNames.ApiIdConstStrKey, out string apiId);
                ApiId.Add(apiId);
                eventsStuff.Value.TryGetValue(MsalTelemetryBlobEventNames.ForceRefreshId, out string forceRefresh);
                _forceRefresh = forceRefresh;
            }            

            // csv expected format:
            // 2|api_id,force_refresh|platform_config
            string[] myValues = new string[] {
                ApiId.FirstOrDefault(),
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

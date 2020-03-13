// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Xml;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using static Microsoft.Identity.Client.TelemetryCore.TelemetryManager;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal class HttpTelemetryContent
    {
        private List<string> ApiId { get; set; } = new List<string>();
        private List<string> PreviousApiId { get; set; } = new List<string>();
        private List<string> ErrorCode { get; set; } = new List<string>();
        private List<string> CorrelationId { get; set; } = new List<string>();

        private string _forceRefresh;

        private ConcurrentQueue<EventBase> _stoppedEvents = new ConcurrentQueue<EventBase>();

        public void RecordFailedApiEvent(EventBase stoppedEvent)
        {
            _stoppedEvents.Enqueue(stoppedEvent);
        }

        public string GetCsvAsPrevious(int successfulSilentCallCount)
        {
            // TODO: make sure to add some locking here to protect against duplicate data
            // i.e. multiple requiests in parallel accessing the queue

            if (_stoppedEvents.Count == 0)
            {
                return string.Empty;
            }

            foreach (var stopEvent in _stoppedEvents)
            {
                if (stopEvent.ContainsKey(MsalTelemetryBlobEventNames.ApiErrorCodeConstStrKey)) //only want to record failed events
                {
                    ErrorCode.Add(stopEvent[MsalTelemetryBlobEventNames.ApiErrorCodeConstStrKey]);
                    CorrelationId.Add(stopEvent[MsalTelemetryBlobEventNames.MsalCorrelationIdConstStrKey]);
                    PreviousApiId.Add(stopEvent[MsalTelemetryBlobEventNames.ApiIdConstStrKey]);
                }
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

        public string GetCsvAsCurrent(EventBase eventsInProgress)
        {
            if (eventsInProgress == null)
            {
                return string.Empty;
            }

            eventsInProgress.TryGetValue(MsalTelemetryBlobEventNames.ApiIdConstStrKey, out string apiId);
            ApiId.Add(apiId);
            eventsInProgress.TryGetValue(MsalTelemetryBlobEventNames.ForceRefreshId, out string forceRefresh);
            _forceRefresh = forceRefresh;

            string apiEvents = string.Join(",", ApiId);

            // csv expected format:
            // 2|api_id,force_refresh|platform_config
            string[] myValues = new string[] {
                apiEvents,
                ConvertFromStringToBitwise(_forceRefresh)};

            string csvString = string.Join(",", myValues);
            ApiId.Clear();
            return $"{TelemetryConstants.HttpTelemetrySchemaVersion2}{TelemetryConstants.HttpTelemetryPipe}{csvString}{TelemetryConstants.HttpTelemetryPipe}";
        }

        private string CreateApiIdAndCorrelationIdContent()
        {
            List<string> combinedApiIdCorrId = new List<string>();

            for (int i = 0; i < Math.Min(PreviousApiId.Count, CorrelationId.Count); i++)
            {
                combinedApiIdCorrId.Add(PreviousApiId[i]);
                combinedApiIdCorrId.Add(CorrelationId[i]);

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
            //ApiId?.Clear();
            //CorrelationId?.Clear();
            //ErrorCode?.Clear();

            while (_stoppedEvents.TryDequeue(out _))
            {
                // do nothing
            }
        }
    }
}

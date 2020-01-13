// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal class HttpTelemetryContent
    {
        public HttpTelemetryContent(EventBase evt)
        {
            if (evt != null)
            {
                evt.TryGetValue(MsalTelemetryBlobEventNames.ApiIdConstStrKey, out string apiId);
                evt.TryGetValue(MsalTelemetryBlobEventNames.MsalCorrelationIdConstStrKey, out string correlationId);
                evt.TryGetValue(MsalTelemetryBlobEventNames.ApiErrorCodeConstStrKey, out string errorCode);
                evt.TryGetValue(MsalTelemetryBlobEventNames.ForceRefreshId, out string forceRefresh);

                ApiId.Add(apiId ?? string.Empty);
                CorrelationId.Add(correlationId ?? string.Empty);
                LastErrorCode.Add(errorCode ?? string.Empty);
                ForceRefresh = forceRefresh ?? string.Empty;
            }
        }

        public /*For testing*/ HttpTelemetryContent(bool shouldClearCache)
        {
            if (shouldClearCache)
            {
                ApiId?.Clear();
                CorrelationId?.Clear();
                LastErrorCode?.Clear();
            }
        }

        public string ForceRefresh { get; set; } = string.Empty;
        public List<string> ApiId { get; set; } = new List<string>();
        public List<string> CorrelationId { get; set; } = new List<string>();
        public List<string> LastErrorCode { get; set; } = new List<string>();

        public string GetCsvAsPrevious(int successfulSilentCallCount)
        {
            if (ApiId == null)
            {
                return string.Empty;
            }

            // csv expected format:
            // 2|silent_successful_count|failed_requests|errors|platform_fields
            // failed_request can be further expanded to include:
            // api_id_1,correlation_id_1,api_id_2,correlation_id_2|error_1,error_2
            string apiIdCorIdData = CreateApiIdAndCorrelationIdContent();
            string data = string.Format(CultureInfo.InvariantCulture,
                $"{TelemetryConstants.HttpTelemetrySchemaVersion2}{TelemetryConstants.HttpTelemetryPipe}{successfulSilentCallCount}{TelemetryConstants.HttpTelemetryPipe}" +
                $"{CreateFailedRequestsContent(apiIdCorIdData)}" +
                $"{TelemetryConstants.HttpTelemetryPipe}");

            if (data.Length <= 3800)
            {
                return data;
            }
            else
            {
                ApiId?.Clear();
                CorrelationId?.Clear();
                LastErrorCode?.Clear();
                return string.Empty;
            }                
        }

        private string CreateApiIdAndCorrelationIdContent()
        {
            var apiIdAndCorId = ApiId.Concat(CorrelationId)
                                        .ToList();
            return string.Join(",", apiIdAndCorId);
        }

        private string CreateFailedRequestsContent(string apiIdAndCorIdCsv)
        {
            string errorCodeCsv = string.Join(",", LastErrorCode);
            return $"{apiIdAndCorIdCsv}{TelemetryConstants.HttpTelemetryPipe}{errorCodeCsv}";
        }

        public string GetCsvAsCurrent()
        {
            // csv expected format:
            // 2|api_id,force_refresh|platform_config
            string[] myValues = new string[] {
                ApiId.FirstOrDefault(),
                ConvertFromStringToBitwise(ForceRefresh)};

            string csvString = string.Join(",", myValues);
            return $"{TelemetryConstants.HttpTelemetrySchemaVersion2}{TelemetryConstants.HttpTelemetryPipe}{csvString}{TelemetryConstants.HttpTelemetryPipe}";
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

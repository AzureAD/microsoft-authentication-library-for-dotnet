// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

                ApiId = apiId ?? string.Empty;
                CorrelationId = correlationId ?? string.Empty;
                LastErrorCode = errorCode ?? string.Empty;
            }
        }

        public string LastErrorCode { get; set; } = string.Empty;
        //public int UnreportedErrorCount { get; set; }
        public string ApiId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;

        public string GetCsvAsPrevious()
        {
            if (string.IsNullOrWhiteSpace(ApiId))
            {
                return string.Empty;
            }

            // csv expected format:
            // 1|api_id,correlation_id,last_error_code
            string[] myValues = new string[] {
                ApiId,
                CorrelationId,
                LastErrorCode};

            string csvString = string.Join(",", myValues);
            csvString = $"{TelemetryConstants.HttpTelemetrySchemaVersion1}{TelemetryConstants.HttpTelemetryPipe}{csvString}";
            return csvString;
        }

        public string GetCsvAsCurrent()
        {
            // csv expected format:
            // 1|api_id,platform_config
            string csvString = $"{TelemetryConstants.HttpTelemetrySchemaVersion1}{TelemetryConstants.HttpTelemetryPipe}{ApiId}";
            return csvString;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
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

                ApiId = apiId ?? string.Empty;
                CorrelationId = correlationId ?? string.Empty;
                LastErrorCode = errorCode ?? string.Empty;
                ForceRefresh = forceRefresh ?? string.Empty;
            }
        }

        public string LastErrorCode { get; set; } = string.Empty;
        public string ApiId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string ForceRefresh { get; set; } = string.Empty;

        public string GetCsvAsPrevious()
        {
            if (string.IsNullOrWhiteSpace(ApiId))
            {
                return string.Empty;
            }

            // csv expected format:
            // 1|api_id,correlation_id,last_error_code|platform_config
            string[] myValues = new string[] {
                ApiId,
                CorrelationId,
                LastErrorCode};

            string csvString = string.Join(",", myValues);
            csvString = $"{TelemetryConstants.HttpTelemetrySchemaVersion1}{TelemetryConstants.HttpTelemetryPipe}{csvString}{TelemetryConstants.HttpTelemetryPipe}";
            return csvString;
        }

        public string GetCsvAsCurrent()
        {
            // csv expected format:
            // 1|api_id,force_refresh|platform_config
            string[] myValues = new string[] {
                ApiId,
                ConvertFromStringToBitwise(ForceRefresh),
                };

            string csvString = string.Join(",", myValues);
            csvString = $"{TelemetryConstants.HttpTelemetrySchemaVersion1}{TelemetryConstants.HttpTelemetryPipe}{csvString}{TelemetryConstants.HttpTelemetryPipe}";
            return csvString;
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

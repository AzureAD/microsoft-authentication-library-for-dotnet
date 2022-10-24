// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal static class TelemetryConstants
    {
        public const string HttpTelemetrySchemaVersion = "5";
        public const string HttpTelemetryPipe = "|";
        public const string XClientCurrentTelemetry = "x-client-current-telemetry";
        public const string XClientLastTelemetry = "x-client-last-telemetry";
        public const string False = "false";
        public const string True = "true";
        public const string One = "1";
        public const string Zero = "0";
        public const string CommaDelimiter = ",";
        public const string PlatformFields = "platform_fields";

#region Telemetry Client Constants

        public const string AcquireTokenEventName = "acquire_token";
        public const string RemainingLifetime = "RemainingLifetime";
        public const string PopToken = "PopToken";
        public const string TokenSource = "TokenSource";
        public const string CacheInfoTelemetry = "CacheInfoTelemetry";
        public const string ErrorCode = "ErrorCode";
        public const string Duration = "Duration";
        public const string Succeeded = "Succeeded";
        public const string DurationInCache = "DurationInCache";
        public const string DurationInHttp = "DurationInHttp";
        public const string ActivityId = "ActivityId";
        public const string Resource = "Resource";

#endregion
    }
}

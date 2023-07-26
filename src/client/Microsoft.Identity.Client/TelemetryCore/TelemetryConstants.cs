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
        public const string ConfigurationUpdateEventName = "config_update";
        public const string MsalVersion = "MsalVersion";
        public const string RemainingLifetime = "RemainingLifetime";
        public const string TokenType = "TokenType";
        public const string TokenSource = "TokenSource";
        public const string CacheInfoTelemetry = "CacheInfoTelemetry";
        public const string ErrorCode = "ErrorCode";
        public const string StsErrorCode = "StsErrorCode";
        public const string ErrorMessage = "ErrorMessage";
        public const string Duration = "Duration";
        public const string Succeeded = "Succeeded";
        public const string DurationInCache = "DurationInCache";
        public const string DurationInHttp = "DurationInHttp";
        public const string ActivityId = "ActivityId";
        public const string Resource = "Resource";
        public const string RefreshOn = "RefreshOn";
        public const string CacheLevel = "CacheLevel";
        public const string AssertionType = "AssertionType";
        public const string Endpoint = "Endpoint";
        public const string Scopes = "Scopes";
        public const string ClientId = "ClientId";
        public const string Platform = "Platform";

        #endregion
    }
}

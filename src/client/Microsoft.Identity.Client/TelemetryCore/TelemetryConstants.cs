// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal static class TelemetryError
    {
        public const string XmsCliTelemMalformed = "Malformed x-ms-clitelem header: '{0}'";
        public const string XmsUnrecognizedHeaderVersion = "Header version '{0}' unrecognized";
    }

    //internal static class TelemetryEventProperties
    //{
    //    public const string MsalDefaultEvent = "msal.default_event";
    //    public const string MsalHttpEventCount = "msal.http_event_count";
    //    public const string MsalCacheEventCount = "msal.cache_event_count";
    //    public const string MsalUiEventCount = "msal.ui_event_count";
    //}

    internal static class TelemetryConstants
    {
        public const string HttpTelemetrySchemaVersion2 = "2";
        public const string HttpTelemetryPipe = "|";
        public const string XClientCurrentTelemetry = "x-client-current-telemetry";
        public const string XClientLastTelemetry = "x-client-last-telemetry";
        public const string False = "false";
        public const string True = "true";
        public const string One = "1";
        public const string Zero = "0";
        public const string CommaDelimiter = ",";
        public const string PlatformFields = "platform_fields";
    }
}

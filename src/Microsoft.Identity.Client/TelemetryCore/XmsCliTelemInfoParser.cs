// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal class XmsCliTelemInfoParser
    {
        private const string ExpectedCliTelemHeaderVersion = "1";
        private const int ErrorCodeIndex = 1;
        private const int SubErrorCodeIndex = 2;
        private const int TokenAgeIndex = 3;
        private const int SpeInfoIndex = 4;

        internal XmsCliTelemInfo ParseXMsTelemHeader(string headerValue, RequestContext requestContext)
        {
            if (string.IsNullOrEmpty(headerValue))
            {
                return null;
            }

            string[] headerSegments = headerValue.Split(',');
            if (headerSegments.Length == 0)
            {
                requestContext.Logger.Warning(
                    FormatLogMessage(TelemetryError.XmsCliTelemMalformed, headerValue));
                return null;
            }

            string headerVersion = headerSegments[0];
            XmsCliTelemInfo xMsTelemetryInfo = new XmsCliTelemInfo
            {
                Version = headerVersion
            };

            if (!string.Equals(headerVersion, ExpectedCliTelemHeaderVersion))
            {
                requestContext.Logger.Warning(
                    FormatLogMessage(TelemetryError.XmsUnrecognizedHeaderVersion, headerVersion));
                return xMsTelemetryInfo;
            }

            MatchCollection formatMatcher = MatchHeaderToExpectedFormat(headerValue);
            if (formatMatcher.Count < 1)
            {
                requestContext.Logger.Warning(
                    FormatLogMessage(TelemetryError.XmsCliTelemMalformed, headerValue));
                return xMsTelemetryInfo;
            }

            xMsTelemetryInfo.ServerErrorCode = headerSegments[ErrorCodeIndex];
            xMsTelemetryInfo.ServerSubErrorCode = headerSegments[SubErrorCodeIndex];
            xMsTelemetryInfo.TokenAge = headerSegments[TokenAgeIndex];
            xMsTelemetryInfo.SpeInfo = headerSegments[SpeInfoIndex];
            return xMsTelemetryInfo;
        }

        private MatchCollection MatchHeaderToExpectedFormat(string headerValue)
        {
            // Verify the expected format "<version>, <error_code>, <sub_error_code>, <token_age>, <ring>"
            Regex headerFormat = new Regex(@"^[1-9]+\.?[0-9|\\.]*,[0-9|\\.]*,[0-9|\\.]*,[^,]*[0-9\\.]*,[^,]*$");
            MatchCollection formatMatcher = headerFormat.Matches(headerValue);
            return formatMatcher;
        }

        private string FormatLogMessage(string telemetryError , string valueToAppend)
        {
            if (valueToAppend != null)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    telemetryError,
                    valueToAppend);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture,
                    telemetryError,
                    "null");
            }
        }
    }
}

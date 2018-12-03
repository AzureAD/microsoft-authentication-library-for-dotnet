//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.Identity.Core.Telemetry
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
            XmsCliTelemInfo xMsTelemetryInfo = new XmsCliTelemInfo();
            xMsTelemetryInfo.Version = headerVersion;

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

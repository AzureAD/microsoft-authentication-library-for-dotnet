//----------------------------------------------------------------------
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

using System;
using System.Globalization;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal class HttpEvent : EventBase
    {
        public const string HttpPathKey = EventNamePrefix + "http_path";
        public const string UserAgentKey = EventNamePrefix + "user_agent";
        public const string QueryParametersKey = EventNamePrefix + "query_parameters";
        public const string ApiVersionKey = EventNamePrefix + "api_version";
        public const string ResponseCodeKey = EventNamePrefix + "response_code";
        public const string OauthErrorCodeKey = EventNamePrefix + "oauth_error_code";
        public const string HttpMethodKey = EventNamePrefix + "http_method";
        public const string RequestIdHeaderKey = EventNamePrefix + "request_id_header";
        public const string TokenAgeKey = EventNamePrefix + "token_age";
        public const string SpeInfoKey = EventNamePrefix + "spe_info";
        public const string ServerErrorCodeKey = EventNamePrefix + "server_error_code";
        public const string ServerSubErrorCodeKey = EventNamePrefix + "server_sub_error_code";

        public HttpEvent() : base(EventNamePrefix + "http_event") {}

        public Uri HttpPath
        {
            set { this[HttpPathKey] = ScrubTenant(value); } // http path is case-sensitive
        }

        public string UserAgent
        {
            set { this[UserAgentKey] = value; }
        }

        public string QueryParams
        {
            set
            {
                this[QueryParametersKey] = String.Join( // query parameters are case-sensitive
                    "&",
                    CoreHelpers.ParseKeyValueList(value, '&', false, true, null)
                        .Keys); // It turns out ParseKeyValueList(..., null) is valid
            }
        }

        public string ApiVersion
        {
            set { this[ApiVersionKey] = value?.ToLowerInvariant(); }
        }

        public int HttpResponseStatus
        {
            set { this[ResponseCodeKey] = value.ToString(CultureInfo.InvariantCulture); }
        }

        public string OauthErrorCode
        {
            set { this[OauthErrorCodeKey] = value; }
        }

        public string HttpMethod
        {
            set { this[HttpMethodKey] = value;  }
        }

        /// <summary>
        /// GUID included in request header 
        /// </summary>
        public string RequestIdHeader
        {
            set { this[RequestIdHeaderKey] = value; }
        }

        /// <summary>
        /// Floating-point value with a unit of milliseconds indicating the 
        /// refresh token age
        /// </summary>
        public string TokenAge
        {
            set { this[TokenAgeKey] = value; }
        }

        /// <summary>
        ///  Indicates whether the request was executed on a ring serving SPE traffic.
        ///  An empty string indicates this occurred on an outer ring, and the string "I" 
        ///  indicates the request occurred on the inner ring
        /// </summary>
        public string SpeInfo
        {
            set { this[SpeInfoKey] = value; }
        }

        /// <summary>
        /// Error code sent by ESTS
        /// </summary>
        public string ServerErrorCode
        {
            set { this[ServerErrorCodeKey] = value; }
        }

        /// <summary>
        /// Error code which gives more detailed information about server error code
        /// </summary>
        public string ServerSubErrorCode
        {
            set { this[ServerSubErrorCodeKey] = value; }
        }
    }
}

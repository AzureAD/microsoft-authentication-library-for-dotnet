// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Mats.Internal.Events
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

        public HttpEvent(string telemetryCorrelationId) : base(EventNamePrefix + "http_event", telemetryCorrelationId) {}

        public Uri HttpPath
        {
            // http path is case-sensitive
            set => this[HttpPathKey] = ScrubTenant(value);
        }

        public string UserAgent
        {
            set => this[UserAgentKey] = value;
        }

        public string QueryParams
        {
            // query parameters are case-sensitive
            set =>
                this[QueryParametersKey] = string.Join(
                    "&",
                    CoreHelpers.ParseKeyValueList(value, '&', false, true, null)
                               .Keys);
        }

        public string ApiVersion
        {
            set => this[ApiVersionKey] = value?.ToLowerInvariant();
        }

        public int HttpResponseStatus
        {
            set => this[ResponseCodeKey] = value.ToString(CultureInfo.InvariantCulture);
        }

        public string OauthErrorCode
        {
            set => this[OauthErrorCodeKey] = value;
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
            set => this[RequestIdHeaderKey] = value;
        }

        /// <summary>
        /// Floating-point value with a unit of milliseconds indicating the
        /// refresh token age
        /// </summary>
        public string TokenAge
        {
            set => this[TokenAgeKey] = value;
        }

        /// <summary>
        ///  Indicates whether the request was executed on a ring serving SPE traffic.
        ///  An empty string indicates this occurred on an outer ring, and the string "I"
        ///  indicates the request occurred on the inner ring
        /// </summary>
        public string SpeInfo
        {
            set => this[SpeInfoKey] = value;
        }

        /// <summary>
        /// Error code sent by ESTS
        /// </summary>
        public string ServerErrorCode
        {
            set => this[ServerErrorCodeKey] = value;
        }

        /// <summary>
        /// Error code which gives more detailed information about server error code
        /// </summary>
        public string ServerSubErrorCode
        {
            set => this[ServerSubErrorCodeKey] = value;
        }
    }
}

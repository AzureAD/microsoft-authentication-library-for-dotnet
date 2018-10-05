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
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Core.Telemetry
{
    internal class ApiEvent : EventBase
    {
        public const string ApiIdKey = EventNamePrefix + "api_id";
        public const string AuthorityKey = EventNamePrefix + "authority";
        public const string AuthorityTypeKey = EventNamePrefix + "authority_type";
        public const string UiBehaviorKey = EventNamePrefix + "ui_behavior";
        public const string ValidationStatusKey = EventNamePrefix + "validation_status";
        public const string TenantIdKey = EventNamePrefix + "tenant_id";
        public const string UserIdKey = EventNamePrefix + "user_id";
        public const string WasSuccessfulKey = EventNamePrefix + "was_successful";
        public const string CorrelationIdKey = EventNamePrefix + "correlation_id";
        public const string RequestIdKey = EventNamePrefix + "request_id";
        public const string IsConfidentialClientKey = EventNamePrefix + "is_confidential_client";
        public const string ApiErrorCodeKey = EventNamePrefix + "api_error_code";

        public enum ApiIds
        {
            AcquireTokenSilentWithAuthority = 31,
            AcquireTokenSilentWithoutAuthority = 30,

            AcquireTokenWithScope = 170,
            AcquireTokenWithScopeHint = 171,
            AcquireTokenWithScopeHintBehavior = 172,
            AcquireTokenWithScopeHintBehaviorAuthority = 173,
            AcquireTokenWithScopeUser = 176,
            AcquireTokenWithScopeUserBehavior = 174,
            AcquireTokenWithScopeUserBehaviorAuthority = 175,

            AcquireTokenOnBehalfOfWithScopeUser = 520,
            AcquireTokenOnBehalfOfWithScopeUserAuthority = 521,

            AcquireTokenForClientWithScope = 726,
            AcquireTokenForClientWithScopeRefresh = 727,

            AcquireTokenByAuthorizationCodeWithCodeScope = 830,
        }

        private readonly ICoreLogger _logger;

        public ApiEvent(ICoreLogger logger) : base(EventNamePrefix + "api_event")
        {
            _logger = logger;
        }

        public ApiIds ApiId
        {
            set { this[ApiIdKey] = ((int) value).ToString(CultureInfo.InvariantCulture); }
        }

        public Uri Authority
        {
            set { this[AuthorityKey] = ScrubTenant(value)?.ToLowerInvariant(); }
        }

        public string AuthorityType
        {
            set { this[AuthorityTypeKey] = value?.ToLowerInvariant(); }
        }

        public string UiBehavior
        {
            set { this[UiBehaviorKey] = value?.ToLowerInvariant(); }
        }

        public string ValidationStatus
        {
            set { this[ValidationStatusKey] = value?.ToLowerInvariant(); }
        }

        public string TenantId
        {
            set
            {
                this[TenantIdKey] = value != null && _logger.PiiLoggingEnabled
                    ? CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(value)
                    : null;
            }
        }

        public string AccountId
        {
            set
            {
                this[UserIdKey] = value != null && _logger.PiiLoggingEnabled
                    ? CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(value)
                    : null;
            }
        }

        public bool WasSuccessful
        {
            set { this[WasSuccessfulKey] = value.ToString().ToLowerInvariant(); }
            get { return this[WasSuccessfulKey] == true.ToString().ToLowerInvariant(); }
        }

        public string CorrelationId
        {
            set { this[CorrelationIdKey] = value; }
        }

        public string RequestId
        {
            set { this[RequestIdKey] = value; }
        }

        public bool IsConfidentialClient
        {
            set { this[IsConfidentialClientKey] = value.ToString().ToLowerInvariant(); }
        }

        public string ApiErrorCode {
            set { this[ApiErrorCodeKey] = value; }
        }
    }
}

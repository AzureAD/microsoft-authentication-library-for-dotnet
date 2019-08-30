// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.TelemetryCore.Internal.Events
{
    internal class ApiEvent : EventBase
    {
        public const string AuthorityKey = EventNamePrefix + "authority";
        public const string AuthorityTypeKey = EventNamePrefix + "authority_type";
        public const string PromptKey = EventNamePrefix + "ui_behavior";
        public const string TenantIdKey = EventNamePrefix + "tenant_id";
        public const string UserIdKey = EventNamePrefix + "user_id";
        public const string WasSuccessfulKey = EventNamePrefix + "was_successful";
        public const string IsConfidentialClientKey = EventNamePrefix + "is_confidential_client";
        public const string ApiErrorCodeKey = EventNamePrefix + "api_error_code";
        public const string LoginHintKey = EventNamePrefix + "login_hint";

        public enum ApiIds
        {
            None = 0,

            // TODO: These are all new ids, one for each of the flows.
            // These are differentiated from the existing IDs because of the new construct of having
            // the TelemetryFeature bits to avoid geometric permutations of ID values and allow more robust filtering
            // on the server side.

            // If these arrive, then the permutuations of "with behavior/hint/scope/refresh/etc" are all
            // bits sent as separate fields via ApiTelemetryFeature values.
            AcquireTokenByAuthorizationCode = 1000,
            AcquireTokenByRefreshToken = 1001,
            AcquireTokenByIntegratedWindowsAuth = 1002,
            AcquireTokenByUsernamePassword = 1003,
            AcquireTokenForClient = 1004,
            AcquireTokenInteractive = 1005,
            AcquireTokenOnBehalfOf = 1006,
            AcquireTokenSilent = 1007,
            AcquireTokenByDeviceCode = 1008,
            GetAuthorizationRequestUrl = 1009,
        }

        private readonly ICryptographyManager _cryptographyManager;
        private readonly ICoreLogger _logger;

        public ApiEvent(
            ICoreLogger logger,
            ICryptographyManager cryptographyManager,
            string correlationId) : base(EventNamePrefix + "api_event", correlationId)
        {
            _logger = logger;
            _cryptographyManager = cryptographyManager;
        }

        public ApiTelemetryId ApiTelemId
        {
            set => this[MsalTelemetryBlobEventNames.ApiTelemIdConstStrKey] = ((int) value).ToString(CultureInfo.InvariantCulture);
        }

        public ApiIds ApiId
        {
            set => this[MsalTelemetryBlobEventNames.ApiIdConstStrKey] = ((int) value).ToString(CultureInfo.InvariantCulture);
        }

        public Uri Authority
        {
            set => this[AuthorityKey] = ScrubTenant(value)?.ToLowerInvariant();
        }

        public string AuthorityType
        {
            set => this[AuthorityTypeKey] = value?.ToLowerInvariant();
        }

        public string Prompt
        {
            set => this[PromptKey] = value?.ToLowerInvariant();
        }

        public string TenantId
        {
            set =>
                this[TenantIdKey] = value != null && _logger.PiiLoggingEnabled
                                        ? HashPersonalIdentifier(_cryptographyManager, value)
                                        : null;
        }

        public string AccountId
        {
            set =>
                this[UserIdKey] = value != null && _logger.PiiLoggingEnabled
                                      ? HashPersonalIdentifier(_cryptographyManager, value)
                                      : null;
        }

        public bool WasSuccessful
        {
#pragma warning disable CA1305 // .net standard does not have an overload for ToString() with Culture
            set { this[WasSuccessfulKey] = value.ToString().ToLowerInvariant(); }
            get { return this[WasSuccessfulKey] == true.ToString().ToLowerInvariant(); }
#pragma warning restore CA1305 // Specify IFormatProvider

        }

        public bool IsConfidentialClient
        {
#pragma warning disable CA1305 // Specify IFormatProvider
            set { this[IsConfidentialClientKey] = value.ToString().ToLowerInvariant(); }
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        public string ApiErrorCode
        {
            set => this[ApiErrorCodeKey] = value;
        }

        public string LoginHint
        {
            set =>
                this[LoginHintKey] = value != null && _logger.PiiLoggingEnabled
                                         ? HashPersonalIdentifier(_cryptographyManager, value)
                                         : null;
        }
    }
}

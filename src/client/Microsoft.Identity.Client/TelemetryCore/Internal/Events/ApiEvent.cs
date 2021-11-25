// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;

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
        public const string IsAccessTokenCacheHitKey = EventNamePrefix + "at_cache_hit";
        public const string RegionUsedKey = EventNamePrefix + "region_used";
        public const string RegionSourceKey = EventNamePrefix + "region_source";
        public const string IsTokenCacheSerializedKey = EventNamePrefix + "is_token_cache_serialized";
        public const string IsLegacyCacheEnabledKey = EventNamePrefix + "is_legacy_cache_enabled";
        public const string CacheInfoKey = EventNamePrefix + "cache_info";
        public const string RegionOutcomeKey = EventNamePrefix + "region_outcome";

        public enum ApiIds
        {
            None = 0,
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
            GetAccounts = 1010,
            GetAccountById = 1011,
            GetAccountsByUserFlow = 1012,
            RemoveAccount = 1013
        }

        private readonly ICryptographyManager _cryptographyManager;
        private readonly ICoreLogger _logger;

        public ApiEvent(
            ICoreLogger logger,
            ICryptographyManager cryptographyManager,
            Guid correlationId) : base(EventNamePrefix + "api_event")
        {
            _logger = logger;
            _cryptographyManager = cryptographyManager;
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }

        public ApiIds ApiId
        {
            get => TryGetValue(MsalTelemetryBlobEventNames.ApiIdConstStrKey, out string apiIdString) ? (ApiIds)Enum.Parse(typeof(ApiIds), apiIdString) : ApiIds.None;

            set => this[MsalTelemetryBlobEventNames.ApiIdConstStrKey] = ((int)value).ToString(CultureInfo.InvariantCulture);
        }

        public string ApiIdString
        {
            get => ContainsKey(MsalTelemetryBlobEventNames.ApiIdConstStrKey) ?
                this[MsalTelemetryBlobEventNames.ApiIdConstStrKey] :
                null;
        }

        public string TokenEndpoint
        {
            get; set;
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

        public bool IsAccessTokenCacheHit
        {
#pragma warning disable CA1305 // Specify IFormatProvider
            get
            {
                return this.ContainsKey(IsAccessTokenCacheHitKey) ?
                     (this[IsAccessTokenCacheHitKey] == true.ToString().ToLowerInvariant()) :
                     false;
            }
            set { this[IsAccessTokenCacheHitKey] = value.ToString().ToLowerInvariant(); }
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        public string ApiErrorCode
        {
            get => this.ContainsKey(ApiErrorCodeKey) ? this[ApiErrorCodeKey] : null;
            set => this[ApiErrorCodeKey] = value;
        }

        public string LoginHint
        {
            set =>
                this[LoginHintKey] = value != null && _logger.PiiLoggingEnabled
                                         ? HashPersonalIdentifier(_cryptographyManager, value)
                                         : null;
        }

        #region Region
        public string RegionUsed
        {
            get => this.ContainsKey(RegionUsedKey) ? this[RegionUsedKey] : null;
            set => this[RegionUsedKey] = value;
        }

        public int RegionAutodetectionSource
        {
            get => this.ContainsKey(RegionSourceKey) ?
                (int)Enum.Parse(typeof(RegionAutodetectionSource), this[RegionSourceKey]) : 0;
            set => this[RegionSourceKey] = (value).ToString(CultureInfo.InvariantCulture);
        }

        public int RegionOutcome
        {
            get => this.ContainsKey(RegionOutcomeKey) ?
                (int)Enum.Parse(typeof(RegionOutcome), this[RegionOutcomeKey]) : 0;
            set => this[RegionOutcomeKey] = (value).ToString(CultureInfo.InvariantCulture);
        }
        #endregion

        public bool IsTokenCacheSerialized
        {
#pragma warning disable CA1305 // .net standard does not have an overload for ToString() with Culture
            set { this[IsTokenCacheSerializedKey] = value.ToString().ToLowerInvariant(); }
            get { return this[IsTokenCacheSerializedKey] == true.ToString().ToLowerInvariant(); }
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        public bool IsLegacyCacheEnabled
        {
#pragma warning disable CA1305 // .NET Standard does not have an overload for ToString() with culture
            set { this[IsLegacyCacheEnabledKey] = value.ToString().ToLowerInvariant(); }
            get { return this[IsLegacyCacheEnabledKey] == true.ToString().ToLowerInvariant(); }
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        public int CacheInfo
        {
            get => this.ContainsKey(CacheInfoKey) ?
                (int)Enum.Parse(typeof(CacheRefreshReason), this[CacheInfoKey]) : (int)CacheRefreshReason.NotApplicable;

            set => this[CacheInfoKey] = value.ToString(CultureInfo.InvariantCulture);
        }

        public long DurationInHttpInMs
        {
            get;
            set;
        }

        public long DurationInCacheInMs
        {
            get;
            set;
        }
    }
}

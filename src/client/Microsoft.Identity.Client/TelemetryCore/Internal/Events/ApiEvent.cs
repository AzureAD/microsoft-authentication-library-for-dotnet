// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Region;

namespace Microsoft.Identity.Client.TelemetryCore.Internal.Events
{
    internal class ApiEvent
    {
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
            RemoveAccount = 1013,

            // MSAL 4.51.0+
            RemoveOboTokens = 1014,
            // The API IDs for legacy managed identity will not be found in HTTP telemetry,
            // as we don't hit eSTS for managed identity calls.
            AcquireTokenForSystemAssignedManagedIdentity = 1015,
            AcquireTokenForUserAssignedManagedIdentity = 1016,

            // MSAL 4.54.0+
            InitiateLongRunningObo = 1017,
            AcquireTokenInLongRunningObo = 1018,
        }

        public ApiEvent(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }

        public ApiIds ApiId { get; set; }

        public string ApiIdString
        {
            get => ApiId.ToString("D");
        }

        public string TokenEndpoint { get; set; }

        public bool IsAccessTokenCacheHit { get; set; }

        public string ApiErrorCode { get; set; }

        #region Region
        public string RegionUsed { get; set; }

        private RegionAutodetectionSource? _regionAutodetectionSource;
        public RegionAutodetectionSource RegionAutodetectionSource
        {
            get { return _regionAutodetectionSource ?? RegionAutodetectionSource.None; }
            set { _regionAutodetectionSource = value; }
        }

        public string RegionAutodetectionSourceString
        {
            get => _regionAutodetectionSource.HasValue ? _regionAutodetectionSource.Value.ToString("D") : null;
        }

        private RegionOutcome? _regionOutcome;
        public RegionOutcome RegionOutcome
        {
            get { return _regionOutcome ?? RegionOutcome.None; }
            set { _regionOutcome = value; }
        }

        public string RegionOutcomeString
        {
            get => _regionOutcome.HasValue ? _regionOutcome.Value.ToString("D") : null;
        }
        public string AutoDetectedRegion { get; internal set; }

        public string RegionDiscoveryFailureReason { get; set; }
        #endregion

        public bool IsTokenCacheSerialized { get; set; }

        public string IsTokenCacheSerializedString
        {
            get => IsTokenCacheSerialized ? TelemetryConstants.One : TelemetryConstants.Zero;
        }

        public bool IsLegacyCacheEnabled { get; set; }

        public string IsLegacyCacheEnabledString
        {
            get => IsLegacyCacheEnabled ? TelemetryConstants.One : TelemetryConstants.Zero;
        }

        private CacheRefreshReason? _cacheInfo;
        public CacheRefreshReason CacheInfo
        {
            get { return _cacheInfo ?? CacheRefreshReason.NotApplicable; }
            set { _cacheInfo = value; }
        }

        public string CacheInfoString
        {
            get => _cacheInfo.HasValue ? _cacheInfo.Value.ToString("D") : null;
        }

        public long DurationInHttpInMs { get; set; }

        public long DurationInCacheInMs { get; set; }

        public TokenType? TokenType { get; set; }

        public string TokenTypeString
        {
            get => TokenType.HasValue ? TokenType.Value.ToString("D") : null;
        }

        public AssertionType AssertionType { get; set; }

        public CacheLevel CacheLevel { get; set; }

        public string MsalRuntimeTelemetry { get; set; }

        public TokenSource CredentialSource { get; set; }

        public static bool IsLongRunningObo(ApiIds apiId) => apiId == ApiIds.InitiateLongRunningObo || apiId == ApiIds.AcquireTokenInLongRunningObo;

        public static bool IsOnBehalfOfRequest(ApiIds apiId) => apiId == ApiIds.AcquireTokenOnBehalfOf || IsLongRunningObo(apiId);
    }
}

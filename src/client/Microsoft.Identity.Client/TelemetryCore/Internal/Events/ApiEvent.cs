// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
            RemoveAccount = 1013
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
    }
}

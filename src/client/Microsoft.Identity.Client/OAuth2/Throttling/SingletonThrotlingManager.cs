// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{

    /// <summary>
    /// Throttling occurs
    /// <list type="bullet">
    /// <item>Afetr receving an RetryAfter header</item>
    /// <item>After receiving 429, 5xx HTTP status.</item>
    /// <item>After receiving a UI Interaction required signal</item>
    /// </list>
    /// 
    /// This class manages the throttling providers and is itself a provider
    /// </summary>
    /// <remarks>
    /// Client Throttling spec https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1624
    /// </remarks>
    internal class SingletonThrottlingManager : IThrottlingProvider
    {
        #region For Test

        public IEnumerable<IThrottlingProvider> ThrottlingProvidersForTest => _throttlingProviders;
        #endregion

        #region Singleton
        private SingletonThrottlingManager()
        {
            _throttlingProviders = new List<IThrottlingProvider>()
            {
                // the order is important
                new UiRequiredProvider(),
                new RetryAfterProvider(),
                new HttpStatusProvider(),
            }; 
        }      

        private static readonly Lazy<SingletonThrottlingManager> lazyPrivateCtor =
            new Lazy<SingletonThrottlingManager>(() => new SingletonThrottlingManager());

        public static SingletonThrottlingManager GetInstance()
        {            
            return lazyPrivateCtor.Value; 
        }

        #endregion

        private readonly IEnumerable<IThrottlingProvider> _throttlingProviders;        

        private readonly ISet<ApiEvent.ApiIds> s_supportedRequests = new HashSet<ApiEvent.ApiIds>()
        {
            ApiEvent.ApiIds.AcquireTokenSilent,
            ApiEvent.ApiIds.AcquireTokenByUsernamePassword,
            ApiEvent.ApiIds.AcquireTokenByIntegratedWindowsAuth,
            ApiEvent.ApiIds.AcquireTokenByDeviceCode
        };

        public void RecordException(
            AuthenticationRequestParameters requestParams,
            IReadOnlyDictionary<string, string> bodyParams,
            MsalServiceException ex)
        {
            if (!ex.IsThrottlingException && IsSupportedRequest(requestParams))
            {
                foreach (var provider in _throttlingProviders)
                {
                    provider.RecordException(requestParams, bodyParams, ex);
                }
            }
        }

        public void TryThrottle(
           AuthenticationRequestParameters requestParams,
           IReadOnlyDictionary<string, string> bodyParams)
        {
            if (IsSupportedRequest(requestParams))
            {
                foreach (var provider in _throttlingProviders)
                {
                    provider.TryThrottle(requestParams, bodyParams);
                }
            }
        }

        private bool IsSupportedRequest(AuthenticationRequestParameters requestParameters)
        {
            return s_supportedRequests.Contains(requestParameters.ApiId) &&
                !requestParameters.IsConfidentialClient;
        }

        public void ResetCache()
        {
            foreach (var provider in _throttlingProviders)
            {
                provider.ResetCache();
            }
        }
    }
}

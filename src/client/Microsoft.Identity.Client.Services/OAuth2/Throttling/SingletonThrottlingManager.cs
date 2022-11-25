// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    /// <summary>
    /// Throttling is the action through which MSAL blocks applications from making repeated 
    /// bad requests to the server. This works by MSAL detecting certain conditions when the server
    /// returns an error. If a similar request is then issued under the same condition, the same
    /// server error is returned by MSAL, without contacting the server.
    /// 
    /// Throttling occurs in the following conditions:
    /// <list type="bullet">
    /// <item><description>After receiving an RetryAfter header</description></item>
    /// <item><description>After receiving 429, 5xx HTTP status.</description></item>    
    /// </list>
    /// This class manages the throttling providers and is itself a provider
    /// </summary>
    /// <remarks>
    /// Client Throttling spec https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1624
    /// 
    /// </remarks>
    internal class SingletonThrottlingManager : IThrottlingProvider
    {
        public /* internal for test only */ IEnumerable<IThrottlingProvider> ThrottlingProviders { get; }

        #region Singleton
        private SingletonThrottlingManager()
        {
            ThrottlingProviders = new List<IThrottlingProvider>()
            {
                // the order is important
                new RetryAfterProvider(),
                new HttpStatusProvider(),
                new UiRequiredProvider()
            };
        }

        private static readonly Lazy<SingletonThrottlingManager> lazyPrivateCtor =
            new Lazy<SingletonThrottlingManager>(() => new SingletonThrottlingManager());

        public static SingletonThrottlingManager GetInstance()
        {
            return lazyPrivateCtor.Value;
        }

        #endregion

        public void RecordException(
            AuthenticationRequestParameters requestParams,
            IReadOnlyDictionary<string, string> bodyParams,
            MsalServiceException ex)
        {
            if (!(ex is MsalThrottledServiceException))
            {
                foreach (var provider in ThrottlingProviders)
                {
                    provider.RecordException(requestParams, bodyParams, ex);
                }
            }
        }

        public void TryThrottle(
           AuthenticationRequestParameters requestParams,
           IReadOnlyDictionary<string, string> bodyParams)
        {
            foreach (var provider in ThrottlingProviders)
            {
                provider.TryThrottle(requestParams, bodyParams);
            }
        }

        public void ResetCache()
        {
            foreach (var provider in ThrottlingProviders)
            {
                provider.ResetCache();
            }
        }
    }
}

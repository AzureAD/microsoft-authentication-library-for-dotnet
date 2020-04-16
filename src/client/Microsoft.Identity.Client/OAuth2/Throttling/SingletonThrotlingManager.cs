// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    /// <summary>
    /// Throttling occurs 
    /// <list type="bullet">
    /// <item>Afetr receving an RetryAfter header</item>
    /// <item>After receiving 429, 5xx HTTP status.</item>
    /// <item>After receiving a UI Interaction required signal</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Client Throttling spec https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1624
    /// </remarks>
    internal class SingletonThrottlingManager : IThrottlingManager
    {
        #region Singleton
        private SingletonThrottlingManager() { }

        private static readonly Lazy<SingletonThrottlingManager> lazyPrivateCtor =
            new Lazy<SingletonThrottlingManager>(() => new SingletonThrottlingManager());

        public static SingletonThrottlingManager Instance { get { return lazyPrivateCtor.Value; } }
        #endregion

        private readonly ConcurrentDictionary<string, MsalServiceException>

        public void RecordException(IEnumerable<KeyValuePair<string, string>> bodyParams, MsalServiceException ex)
        {
            ex.
        }

        public void ThrottleIfNeeded(IEnumerable<KeyValuePair<string, string>> bodyParams)
        {
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.CacheV2
{
    /// <summary>
    /// This interface is for an individual request to access the cache functions.
    /// It is assumed that the implementation will have context about the call
    /// when using the cache manager.  In msal, this context means AuthenticationParameters.
    /// </summary>
    internal interface ICacheManager
    {
        /// <summary>
        /// Try to read the cache.  If a cache hit of any kind is found, return the token(s)
        /// and account information that was discovered.
        /// </summary>
        /// <param name="msalTokenResponse"></param>
        /// <param name="account"></param>
        /// <returns>True if a cache hit of any kind is found, False otherwise.</returns>
        bool TryReadCache(out MsalTokenResponse msalTokenResponse, out IAccount account);

        /// <summary>
        /// Given a MsalTokenResponse from the server, cache any relevant entries.
        /// </summary>
        /// <param name="tokenResponse"></param>
        /// <returns></returns>
        IAccount CacheTokenResponse(MsalTokenResponse tokenResponse);

        /// <summary>
        /// Delete the cached refresh token for this cache context.
        /// </summary>
        void DeleteCachedRefreshToken();
    }
}

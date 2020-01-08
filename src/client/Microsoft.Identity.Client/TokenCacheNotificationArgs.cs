// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains parameters used by the MSAL call accessing the cache.
    /// See also <see cref="T:ITokenCacheSerializer"/> which contains methods
    /// to customize the cache serialization.
    /// For more details about the token cache see https://aka.ms/msal-net-web-token-cache
    /// </summary>
    public sealed partial class TokenCacheNotificationArgs
    {
        internal TokenCacheNotificationArgs(
            ITokenCacheSerializer tokenCacheSerializer,
            string clientId,
            IAccount account,
            bool hasStateChanged,
            bool isAppCache)
        {
            TokenCache = tokenCacheSerializer;
            ClientId = clientId;
            Account = account;
            HasStateChanged = hasStateChanged;
            IsApplicationCache = isAppCache;
        }

        /// <summary>
        /// Gets the <see cref="ITokenCacheSerializer"/> involved in the transaction
        /// </summary>
        /// <remarks><see cref="TokenCache" > objects</see> implement this interface.</remarks>
        public ITokenCacheSerializer TokenCache { get; }

        /// <summary>
        /// Gets the ClientId (application ID) of the application involved in the cache transaction
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets the account involved in the cache transaction.
        /// </summary>
        public IAccount Account { get; }

        /// <summary>
        /// Indicates whether the state of the cache has changed, for example when tokens are being added or removed.
        /// Not all cache operations modify the state of the cache.
        /// </summary>
        public bool HasStateChanged { get; internal set; }

        /// <summary>
        /// Indicates whether the cache change occurred in the UserTokenCache or in the AppTokenCache.
        /// </summary>
        /// <remarks>
        /// The Application Cache is used in Client Credential grant,  which is not available on all platforms.
        /// See https://aka.ms/msal-net-app-cache-serialization for details.
        /// </remarks>
        public bool IsApplicationCache { get; }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains parameters used by the MSAL call accessing the cache.
    /// See also <see cref="T:ITokenCacheSerializer"/> which contains methods
    /// to customize the cache serialization
    /// </summary>
    public sealed partial class TokenCacheNotificationArgs
    {
        internal TokenCacheNotificationArgs(
            ITokenCacheSerializer tokenCacheSerializer,
            string clientId,
            IAccount account,
            bool hasStateChanged)
        {
            TokenCache = tokenCacheSerializer;
            ClientId = clientId;
            Account = account;
            HasStateChanged = hasStateChanged;
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
    }
}

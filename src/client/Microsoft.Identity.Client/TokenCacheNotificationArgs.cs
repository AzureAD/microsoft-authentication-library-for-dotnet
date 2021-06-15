// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;

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
            bool isAppCache, 
            bool hasTokens,
            CancellationToken cancellationToken,
            string suggestedCacheKey = null,
            DateTime? suggestedCacheExpiry = null)
        {
            TokenCache = tokenCacheSerializer;
            ClientId = clientId;
            Account = account;
            HasStateChanged = hasStateChanged;
            IsApplicationCache = isAppCache;
            HasTokens = hasTokens;
            SuggestedCacheKey = suggestedCacheKey;
            CancellationToken = cancellationToken;
            SuggestedCacheExpiry = suggestedCacheExpiry;
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

        /// <summary>
        /// A suggested token cache key, which can be used with general purpose storage mechanisms that allow 
        /// storing key-value pairs and key based retrieval. Useful in applications that store 1 token cache per user, 
        /// the recommended pattern for web apps.
        /// 
        /// The value is: 
        /// 
        /// <list type="bullet">
        /// <item>the homeAccountId for AcquireTokenSilent, GetAccount(homeAccountId), RemoveAccount and when writing tokens on confidential client calls</item>
        /// <item>clientID + "_AppTokenCache" for AcquireTokenForClient</item>
        /// <item>clientID_tenantID + "_AppTokenCache" for AcquireTokenForClient when tenant specific authority</item>
        /// <item>the hash of the original token for AcquireTokenOnBehalfOf</item>
        /// </list>
        /// </summary>
        public string SuggestedCacheKey { get; }

        /// <summary>
        /// Is true when at least one non-expired access token exists in the cache. 
        /// </summary>
        /// <remarks>  
        /// If this flag is false in the OnAfterAccessAsync notification, the *application* token cache - used by client_credentials flow / AcquireTokenForClient -  can be deleted.        
        /// MSAL takes into consideration access tokens expiration when computing this flag, but not refresh token expiration, which is not known to MSAL.
        /// </remarks>
        public bool HasTokens { get; }

        /// <summary>
        /// The cancellation token that was passed to AcquireToken* flow via ExecuteAsync(CancellationToken). Can be passed
        /// along to the custom token cache implementation.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <Summary>
        /// Suggested value of the expiry, to help determining the cache eviction time. 
        /// This value is <b>only</b> set on the <code>OnAfterAcces</code> delegate, on a cache write
        /// operation (that is when <code>args.HasStateChanged</code> is <code>true</code>) and when the cache write 
        /// is triggered from the <code>AcquireTokenForClient</code> method. In all other cases it's <code>null</code>, as there is a refresh token, and therefore the
        /// access tokens are refreshable.
        /// </Summary> 
        public DateTime? SuggestedCacheExpiry { get; private set; }
    }
}

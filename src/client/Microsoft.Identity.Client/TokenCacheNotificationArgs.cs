// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.IdentityModel.Abstractions;

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
        /// <summary>
        /// This constructor is for test purposes only. It allows apps to unit test their MSAL token cache implementation code.
        /// </summary>
        public TokenCacheNotificationArgs(
          ITokenCacheSerializer tokenCache,
          string clientId,
          IAccount account,
          bool hasStateChanged,
          bool isApplicationCache,
          string suggestedCacheKey,
          bool hasTokens,
          DateTimeOffset? suggestedCacheExpiry,
          CancellationToken cancellationToken)
            : this(tokenCache,
                   clientId,
                   account,
                   hasStateChanged,
                   isApplicationCache,
                   suggestedCacheKey,
                   hasTokens,
                   suggestedCacheExpiry,
                   cancellationToken,
                   default, 
                   default, 
                   default,
                   null,
                   default)
            {
            }

        /// <summary>
        /// This constructor is for test purposes only. It allows apps to unit test their MSAL token cache implementation code.
        /// </summary>
        public TokenCacheNotificationArgs(
            ITokenCacheSerializer tokenCache,
            string clientId,
            IAccount account,
            bool hasStateChanged,
            bool isApplicationCache,
            string suggestedCacheKey,
            bool hasTokens,
            DateTimeOffset? suggestedCacheExpiry,
            CancellationToken cancellationToken,
            Guid correlationId)       
           : this(tokenCache,
                   clientId,
                   account,
                   hasStateChanged,
                   isApplicationCache,
                   suggestedCacheKey,
                   hasTokens,
                   suggestedCacheExpiry,
                   cancellationToken,
                   correlationId,
                   default,
                   default,
                   null,
                   default)
        { 
        }

        /// <summary>
        /// This constructor is for test purposes only. It allows apps to unit test their MSAL token cache implementation code.
        /// </summary>
        public TokenCacheNotificationArgs(    // only use this constructor in product code
            ITokenCacheSerializer tokenCache,
            string clientId,
            IAccount account,
            bool hasStateChanged,
            bool isApplicationCache,
            string suggestedCacheKey,
            bool hasTokens,
            DateTimeOffset? suggestedCacheExpiry,
            CancellationToken cancellationToken,
            Guid correlationId, 
            IEnumerable<string> requestScopes,
            string requestTenantId)
            
        {
            TokenCache = tokenCache;
            ClientId = clientId;
            Account = account;
            HasStateChanged = hasStateChanged;
            IsApplicationCache = isApplicationCache;
            SuggestedCacheKey = suggestedCacheKey;
            HasTokens = hasTokens;
            CancellationToken = cancellationToken;
            CorrelationId = correlationId;
            RequestScopes = requestScopes;
            RequestTenantId = requestTenantId;
            SuggestedCacheExpiry = suggestedCacheExpiry;
        }

        /// <summary>
        /// This constructor is for test purposes only. It allows apps to unit test their MSAL token cache implementation code.
        /// </summary>
        public TokenCacheNotificationArgs(    // only use this constructor in product code
            ITokenCacheSerializer tokenCache,
            string clientId,
            IAccount account,
            bool hasStateChanged,
            bool isApplicationCache,
            string suggestedCacheKey,
            bool hasTokens,
            DateTimeOffset? suggestedCacheExpiry,
            CancellationToken cancellationToken,
            Guid correlationId,
            IEnumerable<string> requestScopes,
            string requestTenantId,
            IIdentityLogger identityLogger,
            bool piiLoggingEnabled,
            TelemetryData telemetryData = null)
        {
            TokenCache = tokenCache;
            ClientId = clientId;
            Account = account;
            HasStateChanged = hasStateChanged;
            IsApplicationCache = isApplicationCache;
            SuggestedCacheKey = suggestedCacheKey;
            HasTokens = hasTokens;
            CancellationToken = cancellationToken;
            CorrelationId = correlationId;
            RequestScopes = requestScopes;
            RequestTenantId = requestTenantId;
            SuggestedCacheExpiry = suggestedCacheExpiry;
            IdentityLogger = identityLogger;
            PiiLoggingEnabled = piiLoggingEnabled;
            TelemetryData = telemetryData?? new TelemetryData();
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
        /// storing key-value pairs and key based retrieval. Useful in applications that store one token cache per user, 
        /// the recommended pattern for web apps.
        /// 
        /// The value is: 
        /// 
        /// <list type="bullet">
        /// <item><description><c>homeAccountId</c> for <c>AcquireTokenSilent</c>, <c>GetAccount(homeAccountId)</c>, <c>RemoveAccount</c> and when writing tokens on confidential client calls</description></item>
        /// <item><description><c>"{clientId}__AppTokenCache"</c> for <c>AcquireTokenForClient</c></description></item>
        /// <item><description><c>"{clientId}_{tenantId}_AppTokenCache"</c> for <c>AcquireTokenForClient</c> when using a tenant specific authority</description></item>
        /// <item><description>the hash of the original token for <c>AcquireTokenOnBehalfOf</c></description></item>
        /// </list>
        /// </summary>
        public string SuggestedCacheKey { get; }

        /// <summary>
        /// Is true when at least one non-expired access token exists in the cache. 
        /// </summary>
        /// <remarks>  
        /// If this flag is false in the OnAfterAccessAsync notification - the node can be deleted from the underlying storage (e.g. IDistributedCache).
        /// MSAL takes into consideration access tokens expiration when computing this flag. Use in conjunction with SuggestedCacheExpiry.
        /// If a refresh token exists in the cache, this property will always be true.
        /// </remarks>
        public bool HasTokens { get; }

        /// <summary>
        /// The cancellation token that was passed to AcquireToken* flow via ExecuteAsync(CancellationToken). Can be passed
        /// along to the custom token cache implementation.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// The correlation id associated with the request. See <see cref="BaseAbstractAcquireTokenParameterBuilder{T}.WithCorrelationId(Guid)"/>
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Scopes specified in the AcquireToken* method. 
        /// </summary>
        /// <remarks>
        /// Note that Azure Active Directory may return more scopes than requested, however this property will only contain the scopes requested.
        /// </remarks>
        public IEnumerable<string> RequestScopes { get; }

        /// <summary>
        /// Tenant Id specified in the AcquireToken* method, if any.         
        /// </summary>
        /// <remarks>
        /// Note that if "common" or "organizations" is specified, Azure Active Directory discovers the host tenant for the user, and the tokens 
        /// are associated with it. This property is not impacted.</remarks>
        public string RequestTenantId { get; }

        /// <summary>
        /// Suggested value of the expiry, to help determining the cache eviction time. 
        /// This value is <b>only</b> set on the <code>OnAfterAccess</code> delegate, on a cache write
        /// operation (that is when <code>args.HasStateChanged</code> is <code>true</code>) and when the cache node contains only access tokens.        
        /// In all other cases it's <code>null</code>. 
        /// </summary> 
        public DateTimeOffset? SuggestedCacheExpiry { get; }

        /// <summary>
        /// Identity Logger provided at the time of application creation Via WithLogging(IIdentityLogger, bool)/>
        /// Calling the log function will automatically add MSAL formatting to the message. For details see https://aka.ms/msal-net-logging
        /// </summary>
        public IIdentityLogger IdentityLogger { get; }

        /// <summary>
        /// Boolean used to determine if Personally Identifiable Information (PII) logging is enabled.
        /// </summary>
        public bool PiiLoggingEnabled { get; }

        /// <summary>
        /// Cache Details contains the details of L1/ L2 cache for telemetry logging.
        /// </summary>
        public TelemetryData TelemetryData { get; }
    }
}

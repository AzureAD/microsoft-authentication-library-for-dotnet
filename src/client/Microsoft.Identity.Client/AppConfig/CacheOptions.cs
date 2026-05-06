// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Options for MSAL token caches. 
    /// </summary>
    /// <remarks>
    /// Detailed cache guidance for each application type and platform, including L2 options:
    /// https://aka.ms/msal-net-token-cache-serialization
    /// </remarks>
    public class CacheOptions
    {
        /// <summary>
        /// Recommended options for using a static cache. 
        /// </summary>
        public static CacheOptions EnableSharedCacheOptions
        {
            get
            {
                return new CacheOptions(true);
            }
        }

        /// <summary>
        /// Options that disable MSAL's internal in-memory token cache entirely.
        /// Use this when your application manages its own token cache lifecycle.
        /// <para>When set:</para>
        /// <list type="bullet">
        ///   <item><description>MSAL will not read from or write to the internal cache accessor.</description></item>
        ///   <item><description>Token cache serialization callbacks (<c>OnBeforeAccess</c>, <c>OnAfterAccess</c>, <c>OnBeforeWrite</c>) will <b>not</b> be invoked.</description></item>
        ///   <item><description><see cref="IClientApplicationBase.GetAccountsAsync()"/> will always return an empty collection.</description></item>
        ///   <item><description><see cref="IClientApplicationBase.AcquireTokenSilent(System.Collections.Generic.IEnumerable{string}, IAccount)"/> will throw a <see cref="MsalUiRequiredException"/> with error code <see cref="MsalError.InternalCacheDisabled"/>.</description></item>
        ///   <item><description><see cref="ILongRunningWebApi.AcquireTokenInLongRunningProcess(System.Collections.Generic.IEnumerable{string}, string)"/> will throw a <see cref="MsalUiRequiredException"/> with error code <see cref="MsalError.InternalCacheDisabled"/>, because the long-running OBO flow requires a cached token to refresh.</description></item>
        /// </list>
        /// <para>
        /// For confidential client flows, retrieve the refresh token using
        /// <see cref="Extensibility.AuthenticationResultExtensions.GetRefreshToken(AuthenticationResult)"/>
        /// and call <see cref="IByRefreshToken.AcquireTokenByRefreshToken(System.Collections.Generic.IEnumerable{string}, string)"/>
        /// to re-acquire a token on subsequent requests, or use another interactive flow.
        /// </para>
        /// </summary>
        public static CacheOptions DisableInternalCacheOptions => new CacheOptions { IsInternalCacheDisabled = true };

        /// <summary>
        /// Constructor for the options with default values.
        /// </summary>
        public CacheOptions()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="useSharedCache">Set to true to share the cache between all ClientApplication objects. The cache becomes static. <see cref="UseSharedCache"/> for a detailed description. </param>
        public CacheOptions(bool useSharedCache)
        {
            UseSharedCache = useSharedCache;
        }

        /// <summary>
        /// Share the cache between all ClientApplication objects. The cache becomes static. Defaults to false.
        /// </summary>
        /// <remarks>
        /// Recommended only for client credentials flow (service to service communication).
        /// Web apps and Web APIs should use external token caching (Redis, Cosmos etc.) for scaling purposes.
        /// Desktop apps should encrypt and persist their token cache to disk, to avoid losing tokens when app restarts. 
        /// ADAL used a static cache by default.
        /// </remarks>
        public bool UseSharedCache { get; set; }

        /// <summary>
        /// When set to true, MSAL will not read from or write to the internal in-memory token cache.
        /// This is intended for advanced scenarios where the caller manages its own token cache.
        /// Cannot be combined with <see cref="UseSharedCache"/>.
        /// </summary>
        /// <remarks>
        /// When enabled, <see cref="IClientApplicationBase.AcquireTokenSilent(System.Collections.Generic.IEnumerable{string}, IAccount)"/>
        /// and <see cref="ILongRunningWebApi.AcquireTokenInLongRunningProcess(System.Collections.Generic.IEnumerable{string}, string)"/>
        /// will throw a <see cref="MsalUiRequiredException"/> with error code <see cref="MsalError.InternalCacheDisabled"/>.
        /// </remarks>
        public bool IsInternalCacheDisabled { get; set; }

        /// <summary>
        /// Returns <c>true</c> if the given <see cref="CacheOptions"/> instance has the internal cache disabled.
        /// Safe to call with a <c>null</c> argument (returns <c>false</c>).
        /// </summary>
        internal static bool IsDisabledFor(CacheOptions options) => options?.IsInternalCacheDisabled == true;

    }
}

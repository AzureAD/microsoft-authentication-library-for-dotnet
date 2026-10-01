// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extension methods for ITokenCache
    /// </summary>
    public static class TokenCacheExtensions
    {
        /// <summary>
        /// Options for MSAL token caches. 
        /// 
        /// MSAL maintains a token cache internally in memory. By default, this cache object is part of each instance of <see cref="PublicClientApplication"/> or <see cref="ConfidentialClientApplication"/>.
        /// This method allows customization of the in-memory token cache of MSAL. 
        /// 
        /// MSAL's memory cache is different than token cache serialization. Cache serialization pulls the tokens from a cache (e.g. Redis, Cosmos, or a file on disk), 
        /// where they are stored in JSON format, into MSAL's internal memory cache. Memory cache operations do not involve JSON operations. 
        /// 
        /// External cache serialization remains the recommended way to handle desktop apps, web site and web APIs, as it provides persistence. These options
        /// do not currently control external cache serialization.
        /// 
        /// Detailed guidance for each application type and platform:
        /// https://aka.ms/msal-net-token-cache-serialization
        /// </summary>
        /// <param name="tokenCache">Either the UserTokenCache or the AppTokenCache, for which these options apply.</param>
        /// <param name="options">Options for the internal MSAL token caches. </param>
#if !SUPPORTS_CUSTOM_CACHE 
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public static void SetCacheOptions(this ITokenCache tokenCache, CacheOptions options)
        {
            ValidatePlatform();
            TokenCache cache = (TokenCache)tokenCache;
            ITokenCacheInternal tokenCacheInternal = (ITokenCacheInternal)tokenCache;

            cache.ServiceBundle.Config.AccessorOptions = options;

            if (tokenCacheInternal.IsAppSubscribedToSerializationEvents())
            {
                throw new MsalClientException(
                    MsalError.StaticCacheWithExternalSerialization,
                    MsalErrorMessage.StaticCacheWithExternalSerialization);
            }

            var proxy = cache.ServiceBundle?.PlatformProxy ?? PlatformProxyFactory.CreatePlatformProxy(null);
            cache.Accessor = proxy.CreateTokenCacheAccessor(options, tokenCacheInternal.IsApplicationCache);
        }

        /// <summary>
        /// Enables or disables optimized reads from the application token cache.
        /// </summary>
        /// <param name="tokenCache">The application token cache returned by <see cref="IConfidentialClientApplication.AppTokenCache"/>.</param>
        /// <param name="enabled">
        /// When <see langword="true"/>, <c>AcquireTokenForClient</c> first checks MSAL's existing
        /// in-memory cache before invoking external cache serialization callbacks. Concurrent
        /// equivalent external cache hits are coalesced so only one callback transaction enters
        /// the token-cache semaphore. External cache misses retain the existing acquisition
        /// behavior.
        /// </param>
        /// <remarks>
        /// This option is disabled by default. Enable it only when the external cache provider
        /// does not require its callbacks to run once per token request and accepts MSAL's
        /// in-memory application-token cache as a process-local cache layer.
        /// Final authentication-result formatting still runs independently for every request.
        /// Changes made only in the external cache are not observed while a matching, unexpired
        /// token remains in MSAL's in-memory cache. Disable the optimization when immediate
        /// external-cache revalidation is required.
        /// </remarks>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public static void SetAppTokenCacheReadOptimization(
            this ITokenCache tokenCache,
            bool enabled)
        {
            ValidatePlatform();

            if (tokenCache is null)
            {
                throw new System.ArgumentNullException(nameof(tokenCache));
            }

            TokenCache cache = (TokenCache)tokenCache;
            if (!cache.IsAppTokenCache)
            {
                throw new System.InvalidOperationException(
                    "App token cache read optimization can only be configured on IConfidentialClientApplication.AppTokenCache.");
            }

            cache.IsAppTokenCacheReadOptimizationEnabled = enabled;
        }

        private static void ValidatePlatform()
        {
#if !SUPPORTS_CUSTOM_CACHE 
            throw new System.PlatformNotSupportedException("You should not use these TokenCache methods on mobile platforms. " +
                "They are meant to allow applications to define their own storage strategy on .NET desktop and non-mobile platforms such as .NET Core. " +
                "On mobile platforms, MSAL.NET implements a secure and performant storage mechanism. " +
                "For more details about custom token cache serialization, visit https://aka.ms/msal-net-token-cache-serialization.");
#endif
        }
    }
}

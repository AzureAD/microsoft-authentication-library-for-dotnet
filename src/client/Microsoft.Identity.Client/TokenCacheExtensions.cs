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
#if !SUPPORTS_CUSTOM_CACHE || WINDOWS_APP
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

        private static void ValidatePlatform()
        {
#if !SUPPORTS_CUSTOM_CACHE || WINDOWS_APP
            throw new System.PlatformNotSupportedException("You should not use these TokenCache methods on mobile platforms. " +
                "They are meant to allow applications to define their own storage strategy on .NET desktop and non-mobile platforms such as .NET Core. " +
                "On mobile platforms, MSAL.NET implements a secure and performant storage mechanism. " +
                "For more details about custom token cache serialization, visit https://aka.ms/msal-net-token-cache-serialization.");
#endif
        }
    }
}

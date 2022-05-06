// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Notification for certain token cache interactions during token acquisition. This delegate is
    /// used in particular to provide a custom token cache serialization.
    /// See https://aka.ms/aka.ms/msal-net-token-cache-serialization
    /// </summary>
    /// <param name="args">Arguments related to the cache item impacted</param>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
    public delegate void TokenCacheCallback(TokenCacheNotificationArgs args);

    /// <summary>
    /// This is the interface that implements the public access to cache operations.
    /// With CacheV2, this should only be necessary if the caller is persisting
    /// the cache in their own store, since this will provide the serialize/deserialize
    /// and before/after notifications used in that scenario.
    /// See https://aka.ms/aka.ms/msal-net-token-cache-serialization
    /// </summary>
    public partial interface ITokenCache
    {
        /// <summary>
        /// Sets a delegate to be notified before any library method accesses the cache. This gives an option to the
        /// delegate to deserialize a cache entry for the application and accounts specified in the <see cref="TokenCacheNotificationArgs"/>.
        /// See https://aka.ms/msal-net-token-cache-serialization.
        /// If you need async/task-based callbacks, please use SetBeforeAccessAsync instead.
        /// </summary>
        /// <param name="beforeAccess">Delegate set in order to handle the cache deserialization</param>
        /// <remarks>When the delegate is used to deserialize the cache, it might
        /// want to call <see cref="ITokenCacheSerializer.DeserializeMsalV3(byte[], bool)"/></remarks>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        void SetBeforeAccess(TokenCacheCallback beforeAccess);

        /// <summary>
        /// Sets a delegate to be notified after any library method accesses the cache. This gives an option to the
        /// delegate to serialize a cache entry for the application and accounts specified in the <see cref="TokenCacheNotificationArgs"/>.
        /// See https://aka.ms/msal-net-token-cache-serialization.
        /// If you need async/task-based callbacks, please use SetAfterAccessAsync instead.
        /// </summary>
        /// <param name="afterAccess">Delegate set in order to handle the cache serialization in the case where the <see cref="TokenCache.HasStateChanged"/>
        /// member of the cache is <c>true</c></param>
        /// <remarks>In the case where the delegate is used to serialize the cache entirely (not just a row), it might
        /// want to call <see cref="ITokenCacheSerializer.SerializeMsalV3()"/></remarks>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        void SetAfterAccess(TokenCacheCallback afterAccess);

        /// <summary>
        /// Sets a delegate called before any library method writes to the cache. This gives an option to the delegate
        /// to reload the cache state from a row in database and lock that row. That database row can then be unlocked in the delegate
        /// registered with <see cref="SetAfterAccess(TokenCacheCallback)"/>
        /// If you need async/task-based callbacks, please use SetBeforeWriteAsync instead.
        /// </summary>
        /// <param name="beforeWrite">Delegate set in order to prepare the cache serialization</param>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
# endif
        void SetBeforeWrite(TokenCacheCallback beforeWrite);

        /// <summary>
        /// Sets a delegate to be notified before any library method accesses the cache. This gives an option to the
        /// delegate to deserialize a cache entry for the application and accounts specified in the <see cref="TokenCacheNotificationArgs"/>.
        /// See https://aka.ms/msal-net-token-cache-serialization.
        /// This provides the same functionality as SetBeforeAccess but it provides for an async/task-based callback.
        /// </summary>
        /// <param name="beforeAccess">Delegate set in order to handle the cache deserialization</param>
        /// <remarks>In the case where the delegate is used to deserialize the cache, it might
        /// want to call <see cref="ITokenCacheSerializer.DeserializeMsalV3(byte[], bool)"/></remarks>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        void SetBeforeAccessAsync(Func<TokenCacheNotificationArgs, Task> beforeAccess);
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

        /// <summary>
        /// Sets a delegate to be notified after any library method accesses the cache. This gives an option to the
        /// delegate to serialize a cache entry for the application and accounts specified in the <see cref="TokenCacheNotificationArgs"/>.
        /// See https://aka.ms/msal-net-token-cache-serialization.
        /// This provides the same functionality as SetAfterAccess but it provides for an async/task-based callback.
        /// </summary>
        /// <param name="afterAccess">Delegate set in order to handle the cache serialization in the case where the <see cref="TokenCache.HasStateChanged"/>
        /// member of the cache is <c>true</c></param>
        /// <remarks>In the case where the delegate is used to serialize the cache entirely (not just a row), it might
        /// want to call <see cref="ITokenCacheSerializer.SerializeMsalV3()"/></remarks>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        void SetAfterAccessAsync(Func<TokenCacheNotificationArgs, Task> afterAccess);
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

        /// <summary>
        /// Sets a delegate called before any library method writes to the cache. This gives an option to the delegate
        /// to reload the cache state from a row in database and lock that row. That database row can then be unlocked in the delegate
        /// registered with <see cref="SetAfterAccess(TokenCacheCallback)"/>
        /// This provides the same functionality as SetBeforeWrite but it provides for an async/task-based callback.
        /// </summary>
        /// <param name="beforeWrite">Delegate set in order to prepare the cache serialization</param>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        void SetBeforeWriteAsync(Func<TokenCacheNotificationArgs, Task> beforeWrite);
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
    }
}

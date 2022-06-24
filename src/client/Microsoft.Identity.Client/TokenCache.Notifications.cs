// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    public sealed partial class TokenCache : ITokenCacheInternal
    {
        /// <summary>
        /// Notification method called before any library method accesses the cache.
        /// </summary>
        internal TokenCacheCallback BeforeAccess { get; set; }

        /// <summary>
        /// Notification method called before any library method writes to the cache. This notification can be used to reload
        /// the cache state from a row in database and lock that row. That database row can then be unlocked in the
        /// <see cref="AfterAccess"/>notification.
        /// </summary>
        internal TokenCacheCallback BeforeWrite { get; set; }

        /// <summary>
        /// Notification method called after any library method accesses the cache.
        /// </summary>
        internal TokenCacheCallback AfterAccess { get; set; }

        internal Func<TokenCacheNotificationArgs, Task> AsyncBeforeAccess { get; set; }
        internal Func<TokenCacheNotificationArgs, Task> AsyncAfterAccess { get; set; }
        internal Func<TokenCacheNotificationArgs, Task> AsyncBeforeWrite { get; set; }

        bool ITokenCacheInternal.IsAppSubscribedToSerializationEvents()
        {
            return BeforeAccess != null || AfterAccess != null || BeforeWrite != null ||
                AsyncBeforeAccess != null || AsyncAfterAccess != null || AsyncBeforeWrite != null;
        }

        bool ITokenCacheInternal.IsExternalSerializationConfiguredByUser()
        {
            return !this.UsesDefaultSerialization &&
                (this as ITokenCacheInternal).IsAppSubscribedToSerializationEvents();
        }

        async Task ITokenCacheInternal.OnAfterAccessAsync(TokenCacheNotificationArgs args)
        {
            AfterAccess?.Invoke(args);

            if (AsyncAfterAccess != null)
            {
                await AsyncAfterAccess.Invoke(args).ConfigureAwait(false);
            }
        }

        async Task ITokenCacheInternal.OnBeforeAccessAsync(TokenCacheNotificationArgs args)
        {
            BeforeAccess?.Invoke(args);
            if (AsyncBeforeAccess != null)
            {
                await AsyncBeforeAccess
                    .Invoke(args)
                    .ConfigureAwait(false);
            }
        }

        async Task ITokenCacheInternal.OnBeforeWriteAsync(TokenCacheNotificationArgs args)
        {
#pragma warning disable CS0618 // Type or member is obsolete, but preserve old behavior until it is deleted
            HasStateChanged = true;
#pragma warning restore CS0618 // Type or member is obsolete
            args.HasStateChanged = true;
            BeforeWrite?.Invoke(args);

            if (AsyncBeforeWrite != null)
            {
                await AsyncBeforeWrite.Invoke(args).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets a delegate to be notified before any library method accesses the cache. This gives an option to the
        /// delegate to deserialize a cache entry for the application and accounts specified in the <see cref="TokenCacheNotificationArgs"/>.
        /// See https://aka.ms/msal-net-token-cache-serialization
        /// </summary>
        /// <param name="beforeAccess">Delegate set in order to handle the cache deserialization</param>
        /// <remarks>In the case where the delegate is used to deserialize the cache, it might
        /// want to call <see cref="Deserialize(byte[])"/></remarks>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public void SetBeforeAccess(TokenCacheCallback beforeAccess)
        {
            Validate();
            ResetDefaultDelegates();
            BeforeAccess = beforeAccess;
        }

        /// <summary>
        /// Sets a delegate to be notified after any library method accesses the cache. This gives an option to the
        /// delegate to serialize a cache entry for the application and accounts specified in the <see cref="TokenCacheNotificationArgs"/>.
        /// See https://aka.ms/msal-net-token-cache-serialization
        /// </summary>
        /// <param name="afterAccess">Delegate set in order to handle the cache serialization in the case where the <see cref="TokenCache.HasStateChanged"/>
        /// member of the cache is <c>true</c></param>
        /// <remarks>In the case where the delegate is used to serialize the cache entirely (not just a row), it might
        /// want to call <see cref="Serialize()"/></remarks>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public void SetAfterAccess(TokenCacheCallback afterAccess)
        {
            Validate();
            ResetDefaultDelegates();
            AfterAccess = afterAccess;
        }

        /// <summary>
        /// Sets a delegate called before any library method writes to the cache. This gives an option to the delegate
        /// to reload the cache state from a row in database and lock that row. That database row can then be unlocked in the delegate
        /// registered with <see cref="SetAfterAccess(TokenCacheCallback)"/>
        /// </summary>
        /// <param name="beforeWrite">Delegate set in order to prepare the cache serialization</param>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public void SetBeforeWrite(TokenCacheCallback beforeWrite)
        {
            Validate();
            ResetDefaultDelegates();
            BeforeWrite = beforeWrite;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="beforeAccess"></param>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public void SetBeforeAccessAsync(Func<TokenCacheNotificationArgs, Task> beforeAccess)
        {
            Validate();
            ResetDefaultDelegates();
            AsyncBeforeAccess = beforeAccess;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="afterAccess"></param>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public void SetAfterAccessAsync(Func<TokenCacheNotificationArgs, Task> afterAccess)
        {
            Validate();
            ResetDefaultDelegates();
            AsyncAfterAccess = afterAccess;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="beforeWrite"></param>
#if !SUPPORTS_CUSTOM_CACHE
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public void SetBeforeWriteAsync(Func<TokenCacheNotificationArgs, Task> beforeWrite)
        {
            Validate();
            ResetDefaultDelegates();
            AsyncBeforeWrite = beforeWrite;
        }

        private void Validate()
        {
            if (ServiceBundle.Config.AccessorOptions != null)
            {
                throw new MsalClientException(
                    MsalError.StaticCacheWithExternalSerialization,
                    MsalErrorMessage.StaticCacheWithExternalSerialization);
            }

#if !SUPPORTS_CUSTOM_CACHE
        throw new PlatformNotSupportedException("You should not use these TokenCache methods on mobile platforms. " +
            "They are meant to allow applications to define their own storage strategy on .net desktop and non-mobile platforms such as .net core. " +
            "On mobile platforms, MSAL.NET implements a secure and performant storage mechanism. " +
            "For more details about custom token cache serialization, visit https://aka.ms/msal-net-serialization");
#endif
        }

        // In some cases MSAL brings its own serializer (UWP, Confidential Client App cache)
        // so reset them all if the user customizes the serializer
        private void ResetDefaultDelegates()
        {
            if (UsesDefaultSerialization)
            {
                BeforeAccess = null;
                AfterAccess = null;
                BeforeWrite = null;

                AsyncBeforeAccess = null;
                AsyncAfterAccess = null;
                AsyncBeforeWrite = null;
                UsesDefaultSerialization = false;
            }
        }
    }
}

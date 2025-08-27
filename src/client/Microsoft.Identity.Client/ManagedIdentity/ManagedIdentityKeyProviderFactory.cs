// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Creates (once) and caches the most suitable Managed Identity key provider for the current platform.
    /// Thread-safe, lock-free (uses CompareExchange).
    /// </summary>
    internal static class ManagedIdentityKeyProviderFactory
    {
        // Cached singleton instance of the chosen key provider.
        private static IManagedIdentityKeyProvider s_provider;

        /// <summary>
        /// Returns the cached provider if available; otherwise creates it in a thread-safe manner.
        /// </summary>
        internal static IManagedIdentityKeyProvider GetOrCreateProvider(ILoggerAdapter logger)
        {
            // Fast path: read the field once (Volatile ensures latest published value).
            IManagedIdentityKeyProvider existing = Volatile.Read(ref s_provider);

            if (existing != null)
            {
                logger?.Verbose(() => "[MI][KeyProviderFactory] Returning cached key provider instance.");
                return existing;
            }

            logger?.Verbose(() => "[MI][KeyProviderFactory] Creating key provider instance (first use).");
            IManagedIdentityKeyProvider created = CreateProviderCore(logger);

            // Publish the created instance only if another thread has not already published one.
            // If another thread won the race, discard our newly created instance and use theirs.
            IManagedIdentityKeyProvider prior = Interlocked.CompareExchange(ref s_provider, created, null);
            
            if (prior == null)
            {
                logger?.Info($"[MI][KeyProviderFactory] Key provider created: {created.GetType().Name}.");
                return created;
            }

            logger?.Verbose(() => "[MI][KeyProviderFactory] Another thread already created the provider; using existing instance.");
            return prior;
        }

        /// <summary>
        /// Chooses an implementation based on compile-time and runtime platform capabilities.
        /// </summary>
        private static IManagedIdentityKeyProvider CreateProviderCore(ILoggerAdapter logger)
        {
            if (DesktopOsHelper.IsWindows())
            {
                logger?.Info("[MI][KeyProviderFactory] Windows detected with CNG support - using Windows managed identity key provider.");
                return new WindowsManagedIdentityKeyProvider();
            }

            // Non-Windows OS - we will fall back to in-memory implementation.
            logger?.Info("[MI][KeyProviderFactory] Non-Windows platform (with CNG) - using InMemory provider.");
            return new InMemoryManagedIdentityKeyProvider();
        }
    }
}

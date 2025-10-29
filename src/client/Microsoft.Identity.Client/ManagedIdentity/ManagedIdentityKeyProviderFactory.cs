// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Creates (once) and caches the most suitable Managed Identity key provider for the current platform.
    /// Thread-safe, lock-free (uses CompareExchange).
    /// </summary>
    /// <remarks>
    /// This factory class uses a singleton pattern with lazy initialization to ensure only one
    /// key provider instance is created per application domain. The implementation is thread-safe
    /// using <see cref="Interlocked.CompareExchange{T}(ref T, T, T)"/> to avoid locking overhead.
    /// 
    /// The factory automatically selects the most appropriate key provider based on the current
    /// platform capabilities:
    /// <list type="bullet">
    /// <item><description>Windows: Uses <c>WindowsManagedIdentityKeyProvider</c> with CNG support</description></item>
    /// <item><description>Non-Windows: Falls back to <c>InMemoryManagedIdentityKeyProvider</c></description></item>
    /// </list>
    /// </remarks>
    internal static class ManagedIdentityKeyProviderFactory
    {
        // Cached singleton instance of the chosen key provider.
        private static IManagedIdentityKeyProvider s_provider;

        /// <summary>
        /// Returns the cached provider if available; otherwise creates it in a thread-safe manner.
        /// </summary>
        /// <param name="logger">
        /// Logger adapter for recording operations and diagnostics. Can be <c>null</c>.
        /// </param>
        /// <returns>
        /// The singleton <see cref="IManagedIdentityKeyProvider"/> instance appropriate for the current platform.
        /// </returns>
        /// <remarks>
        /// This method implements the double-checked locking pattern using atomic operations
        /// to ensure thread safety without the overhead of explicit locks. If multiple threads
        /// call this method concurrently before initialization, only one provider instance
        /// will be created and cached.
        /// </remarks>
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
        /// <param name="logger">
        /// Logger adapter for recording platform detection and provider selection. Can be <c>null</c>.
        /// </param>
        /// <returns>
        /// A new <see cref="IManagedIdentityKeyProvider"/> instance suitable for the detected platform.
        /// </returns>
        /// <remarks>
        /// This method performs platform detection and selects the most appropriate key provider:
        /// 
        /// <para><strong>Windows Platform:</strong></para>
        /// <list type="bullet">
        /// <item><description>Detected using <see cref="DesktopOsHelper.IsWindows()"/></description></item>
        /// <item><description>Returns <c>WindowsManagedIdentityKeyProvider</c> with CNG support</description></item>
        /// <item><description>Provides hardware-backed key storage when available</description></item>
        /// </list>
        /// 
        /// <para><strong>Non-Windows Platforms:</strong></para>
        /// <list type="bullet">
        /// <item><description>Includes Linux, macOS, and other Unix-like systems</description></item>
        /// <item><description>Returns <c>InMemoryManagedIdentityKeyProvider</c> as fallback</description></item>
        /// <item><description>Keys are stored in memory for the application lifetime</description></item>
        /// </list>
        /// </remarks>
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

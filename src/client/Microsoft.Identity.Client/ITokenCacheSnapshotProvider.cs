// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Optional support for process-local, structured token cache storage.
    /// </summary>
    /// <remarks>
    /// Use within the existing cache notification callbacks and their synchronization
    /// lifecycle. These methods do not acquire the cache lock. Snapshots contain credentials
    /// and must be protected like serialized caches. This interface is not supported on mobile
    /// platforms or when internal cache options have been configured.
    /// </remarks>
    public interface ITokenCacheSnapshotProvider
    {
        /// <summary>
        /// Captures an isolated snapshot of the persisted cache state without JSON serialization.
        /// </summary>
        /// <returns>An immutable, process-local snapshot.</returns>
        /// <exception cref="System.PlatformNotSupportedException">Custom caching is not supported on this platform.</exception>
        /// <exception cref="MsalClientException">Internal cache options are configured.</exception>
        TokenCacheSnapshot CreateSnapshot();

        /// <summary>
        /// Replaces the working token state with isolated copies from a snapshot.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore, or null to clear token state.</param>
        /// <remarks>
        /// Preserves the clearing semantics of DeserializeMsalV3 with shouldClearExistingCache
        /// enabled, including retention of application metadata.
        /// </remarks>
        /// <exception cref="System.ArgumentException">The snapshot belongs to a different cache type (application or user).</exception>
        /// <exception cref="System.PlatformNotSupportedException">Custom caching is not supported on this platform.</exception>
        /// <exception cref="MsalClientException">Internal cache options are configured.</exception>
        void RestoreSnapshot(TokenCacheSnapshot snapshot);
    }
}

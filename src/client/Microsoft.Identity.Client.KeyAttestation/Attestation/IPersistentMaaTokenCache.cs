// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    /// <summary>
    /// Interface for persistent MAA token cache implementations.
    /// Implementations must be best-effort and never throw exceptions that block token acquisition.
    /// </summary>
    internal interface IPersistentMaaTokenCache
    {
        /// <summary>
        /// Attempts to read a cached token from persistent storage.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="entry">The cached entry if found and valid.</param>
        /// <param name="logVerbose">Optional logging callback.</param>
        /// <returns>True if a valid entry was found; false otherwise.</returns>
        bool TryRead(string cacheKey, out MaaTokenCacheEntry entry, Action<string> logVerbose);

        /// <summary>
        /// Attempts to write a token to persistent storage.
        /// This operation is best-effort and must not block token acquisition.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="entry">The entry to persist.</param>
        /// <param name="logVerbose">Optional logging callback.</param>
        void TryWrite(string cacheKey, MaaTokenCacheEntry entry, Action<string> logVerbose);

        /// <summary>
        /// Attempts to delete expired entries from persistent storage.
        /// This operation is best-effort and must not block token acquisition.
        /// </summary>
        /// <param name="cacheKey">The cache key scope for cleanup.</param>
        /// <param name="logVerbose">Optional logging callback.</param>
        void TryDelete(string cacheKey, Action<string> logVerbose);
    }
}

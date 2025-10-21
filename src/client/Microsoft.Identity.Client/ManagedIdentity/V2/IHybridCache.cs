// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Interface for attestation token caching implementations.
    /// Supports hybrid caching strategies combining in-memory and persistent storage
    /// for attestation tokens used in managed identity authentication scenarios.
    /// </summary>
    /// <remarks>
    /// This interface abstracts the caching mechanism to allow for different storage strategies:
    /// - Hybrid caching with in-memory primary and file-based fallback for optimal performance
    /// - Pure in-memory caching for single-process scenarios
    /// - Pure persistent file-based caching for cross-process scenarios
    /// - Custom implementations for specific requirements
    /// 
    /// Implementations should be thread-safe and handle concurrent access gracefully.
    /// For persistent implementations, cross-process synchronization should be considered.
    /// Hybrid implementations should synchronize between memory and persistent storage when possible.
    /// </remarks>
    internal interface IHybridCache
    {
        /// <summary>
        /// Retrieves a cached attestation token if available and valid.
        /// </summary>
        /// <param name="key">
        /// The cache key used to identify the token entry. Typically derived from the KeyHandle pointer value
        /// to ensure uniqueness per cryptographic key.
        /// </param>
        /// <param name="ct">
        /// A cancellation token that can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous get operation. The task result contains:
        /// - An <see cref="AttestationTokenResponse"/> if a valid token is found in the cache
        /// - <c>null</c> if no token is found, the token has expired, or the token is invalid
        /// </returns>
        /// <remarks>
        /// This method should:
        /// - Check token expiration before returning cached values
        /// - Return null for expired tokens rather than throwing exceptions
        /// - Handle storage errors gracefully (e.g., corrupted cache files, memory issues)
        /// - Be thread-safe for concurrent access
        /// - For hybrid implementations: check in-memory cache first, then persistent storage
        /// - Synchronize between cache tiers when possible
        /// </remarks>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled via the cancellation token.
        /// </exception>
        Task<AttestationTokenResponse> GetAsync(long key, CancellationToken ct);

        /// <summary>
        /// Stores an attestation token in the cache with the specified expiration time.
        /// </summary>
        /// <param name="key">
        /// The cache key used to identify the token entry. Should be the same key used in <see cref="GetAsync"/>.
        /// </param>
        /// <param name="token">
        /// The attestation token to cache. This is typically a JWT (JSON Web Token) in string format.
        /// Must not be null or empty.
        /// </param>
        /// <param name="expiresOnUtc">
        /// The UTC date and time when the token expires. The cache implementation may add additional
        /// buffer time to account for clock skew and ensure tokens are refreshed before actual expiration.
        /// </param>
        /// <param name="ct">
        /// A cancellation token that can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous set operation.
        /// </returns>
        /// <remarks>
        /// This method should:
        /// - Overwrite existing entries for the same key
        /// - Handle storage errors gracefully (cache failures should not fail the authentication flow)
        /// - Be thread-safe for concurrent access
        /// - Validate input parameters
        /// - For hybrid implementations: update both in-memory and persistent storage when possible
        /// - Prioritize fast in-memory updates over slower persistent operations
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="token"/> is null or empty.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled via the cancellation token.
        /// </exception>
        Task SetAsync(long key, string token, DateTimeOffset expiresOnUtc, CancellationToken ct);

        /// <summary>
        /// Removes an expired or invalid token from the cache.
        /// </summary>
        /// <param name="key">
        /// The cache key identifying the token entry to remove.
        /// </param>
        /// <param name="ct">
        /// A cancellation token that can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous remove operation.
        /// </returns>
        /// <remarks>
        /// This method should:
        /// - Gracefully handle cases where the key doesn't exist (no-op)
        /// - Handle storage errors gracefully
        /// - Be thread-safe for concurrent access
        /// - Not throw exceptions for missing entries
        /// - For hybrid implementations: remove from both in-memory and persistent storage
        /// - Continue operation even if one storage tier fails
        /// </remarks>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled via the cancellation token.
        /// </exception>
        Task RemoveAsync(long key, CancellationToken ct);
    }
}

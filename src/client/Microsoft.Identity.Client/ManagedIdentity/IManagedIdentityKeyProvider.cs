// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Provides managed identity keys for authentication scenarios.
    /// Implementations of this interface are responsible for obtaining or creating
    /// the best available key type (KeyGuard, Hardware, or InMemory) for managed identity authentication.
    /// </summary>
    internal interface IManagedIdentityKeyProvider
    {
        /// <summary>
        /// Gets an existing managed identity key or creates a new one if none exists.
        /// The method returns the best available key type based on the provider's capabilities
        /// and the current environment.
        /// </summary>
        /// <param name="logger">Logger adapter for recording operations and diagnostics.</param>
        /// <param name="ct">Cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains
        /// a <see cref="ManagedIdentityKeyInfo"/> object with the key, its type, and provider message.
        /// </returns>
        /// <exception cref="System.OperationCanceledException">
        /// Thrown when the operation is canceled via the cancellation token.
        /// </exception>
        Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(ILoggerAdapter logger, CancellationToken ct);
    }
}

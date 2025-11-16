// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    /// <summary>
    /// Provides per-key async gates using <see cref="SemaphoreSlim"/>.
    /// Call <see cref="EnterAsync"/> and ensure <see cref="Release"/> is called in a finally block.
    /// Semaphores are kept for the lifetime of the pool; we do not remove or dispose them
    /// per key to avoid races with concurrent callers.
    /// </summary>
    internal sealed class KeyedSemaphorePool
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

        /// <summary>
        /// Enters the gate for <paramref name="key"/>. Await and pair with <see cref="Release"/>.
        /// </summary>
        public async Task EnterAsync(string key, CancellationToken cancellationToken)
        {
            var gate = _gates.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Releases the gate for <paramref name="key"/>. Safe to call even if the key
        /// is missing; a missing key is a no-op.
        /// </summary>
        public void Release(string key)
        {
            if (_gates.TryGetValue(key, out var gate))
            {
                gate.Release();
            }
        }
    }
}

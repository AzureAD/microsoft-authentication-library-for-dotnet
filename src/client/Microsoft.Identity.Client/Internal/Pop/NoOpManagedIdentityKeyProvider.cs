// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;
using System;

namespace Microsoft.Identity.Client.Internal.Pop
{
    /// <summary>
    /// Fallback provider used when the POP plug-in is not referenced.
    /// Always returns a software key with no attestation token.
    /// </summary>
    internal sealed class NoOpManagedIdentityKeyProvider : IManagedIdentityKeyProvider
    {
        private static readonly KeyInfo _softKey = new KeyInfo(
            keyRef: "noop-software-key",
            keyType: KeyType.InMemory,
            keyStrength: 2048,
            token: null);

        public Task<KeyInfo> GetOrCreateKeyAsync(
            KeyRequest request,
            CancellationToken _)
        {
            // Attestation is never available
            return Task.FromResult(_softKey);
        }
    }
}

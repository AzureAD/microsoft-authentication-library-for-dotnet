// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyProviders
{
    /// <summary>
    /// In-memory managed identity key provider.
    /// </summary>
    internal sealed class InMemoryManagedIdentityKeyProvider : IManagedIdentityKeyProvider
    {
        private static readonly SemaphoreSlim s_once = new(1, 1);
        private volatile ManagedIdentityKeyInfo _cached;

        public async Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(CancellationToken ct)
        {
            if (_cached is not null)
            {
                return _cached;
            }

            // Ensure only one thread can create the key at a time.
            await s_once.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                if (_cached is not null)
                {
                    return _cached;
                }

                // Respect cancellation after entering critical section.
                ct.ThrowIfCancellationRequested();

                RSA rsa = null;
                string message;

                try
                {
                    rsa = CreateRsaKeyPair();
                    message = "In-memory RSA key created for Managed Identity authentication.";
                }
                catch (Exception ex)
                {
                    message = $"Failed to create in-memory RSA key: {ex.GetType().Name} - {ex.Message}";
                }

                _cached = new ManagedIdentityKeyInfo(rsa, ManagedIdentityKeyType.InMemory, message);
                return _cached;
            }
            finally
            {
                s_once.Release();
            }
        }

        private static RSA CreateRsaKeyPair()
        {
            RSA rsa;
#if NET462 || NET472
            // .NET Framework (Windows): use RSACng 
            rsa = new RSACng();
#else
            // Cross-platform (.NET Core/8+/Standard)
            rsa = RSA.Create();
#endif
            rsa.KeySize = 2048;
            return rsa;
        }
    }
}

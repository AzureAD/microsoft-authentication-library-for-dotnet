// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// ManagedIdentity/Providers/InMemoryManagedIdentityKeyProvider.cs
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ManagedIdentity.Providers
{
    internal sealed class InMemoryManagedIdentityKeyProvider : IManagedIdentityKeyProvider
    {
        private static readonly SemaphoreSlim s_once = new(1, 1);
        private volatile MiKeyInfo _cached;

        public async Task<MiKeyInfo> GetOrCreateKeyAsync(CancellationToken ct)
        {
            if (_cached is not null)
                return _cached;

            await s_once.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_cached is not null)
                    return _cached;

                var rsa = CreateRsaKeyPair();
                _cached = new MiKeyInfo(rsa, MiKeyType.InMemory);
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
            // Cross-platform (.NET Core/5+/Standard)
            rsa = RSA.Create();
#endif
            rsa.KeySize = 2048;
            return rsa;
        }
    }
}

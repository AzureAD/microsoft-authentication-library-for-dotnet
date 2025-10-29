// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    /// <summary>
    /// Returns a KeyGuard key (Type = KeyGuard). On Windows, attempts to use RSACng so the
    /// production check in GetAttestationJwtAsync passes; elsewhere, RSA is fine (the RSACng
    /// requirement is compiled only for Windows/NETFX).
    /// </summary>
    internal sealed class TestKeyGuardManagedIdentityKeyProvider : IManagedIdentityKeyProvider
    {
        // Keep a single ManagedIdentityKeyInfo per provider instance
        private readonly ManagedIdentityKeyInfo _keyInfo;

        /// <summary>
        /// Creates a provider with a fresh 2048-bit RSACng key.
        /// </summary>
        public TestKeyGuardManagedIdentityKeyProvider()
            : this(new RSACng(2048))
        { }

        /// <summary>
        /// Creates a provider that will always return the supplied RSACng key.
        /// Useful when you want two identities with different, fixed keys.
        /// </summary>
        public TestKeyGuardManagedIdentityKeyProvider(RSACng fixedKey)
        {
            _keyInfo = new ManagedIdentityKeyInfo(
                fixedKey,
                ManagedIdentityKeyType.KeyGuard,
                "Test KeyGuard Provider (fixed)");
        }

        public Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(ILoggerAdapter logger, CancellationToken cancellationToken)
            => Task.FromResult(_keyInfo);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
        public Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(ILoggerAdapter logger, CancellationToken cancellationToken)
        {
            var rsacng = new RSACng(2048);
            return Task.FromResult(new ManagedIdentityKeyInfo(rsacng, ManagedIdentityKeyType.KeyGuard, "Test KeyGuard Provider"));
        }
    }
}

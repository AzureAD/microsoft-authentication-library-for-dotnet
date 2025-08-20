// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// src/client/Microsoft.Identity.Client/ManagedIdentity/MiKeyAbstractions.cs
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal enum MiKeyType { KeyGuard, Hardware, InMemory }

    internal sealed class MiKeyInfo
    {
        public RSA KeyInfo { get; }
        public MiKeyType Type { get; }

        public MiKeyInfo(RSA keyInfo, MiKeyType type)
        {
            KeyInfo = keyInfo ?? throw new ArgumentNullException(nameof(keyInfo));
            Type = type;
        }
    }

    internal interface IManagedIdentityKeyProvider
    {
        Task<MiKeyInfo> GetOrCreateKeyAsync(CancellationToken ct);
    }
}

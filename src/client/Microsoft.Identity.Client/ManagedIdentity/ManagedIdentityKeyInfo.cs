// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Encapsulates information about a Managed Identity key used for authentication.
    /// Provides the best available key and its type for Managed Identity scenarios.
    /// The caller does not need to know how the key is sourced.
    /// 
    /// Key types:
    /// - <see cref="ManagedIdentityKeyType.KeyGuard"/>: Key sourced from KeyGuard provider.
    /// - <see cref="ManagedIdentityKeyType.Hardware"/>: Key stored in hardware (e.g., TPM).
    /// - <see cref="ManagedIdentityKeyType.InMemory"/>: Key stored in memory only.
    /// </summary>
    internal sealed class ManagedIdentityKeyInfo
    {
        public RSA Key { get; }
        public ManagedIdentityKeyType Type { get; }
        public string ProviderMessage { get; }

        public ManagedIdentityKeyInfo(RSA keyInfo, ManagedIdentityKeyType type, string providerMessage)
        {
            Key = keyInfo;
            Type = type;
            ProviderMessage = providerMessage;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    /// <remarks>
    /// Initializes a new instance of the <see cref="ManagedIdentityKeyInfo"/> class.
    /// </remarks>
    /// <param name="keyInfo">The RSA key instance to be used for cryptographic operations.</param>
    /// <param name="type">The type of the Managed Identity key indicating its storage method.</param>
    /// <param name="providerMessage">A message from the key provider with additional information.</param>
    internal sealed class ManagedIdentityKeyInfo(RSA keyInfo, ManagedIdentityKeyType type, string providerMessage)
    {
        public RSA Key { get; } = keyInfo;
        public ManagedIdentityKeyType Type { get; } = type;
        public string ProviderMessage { get; } = providerMessage;
    }
}

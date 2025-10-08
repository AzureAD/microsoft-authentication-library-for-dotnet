// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Specifies the type of key storage mechanism used for managed identity authentication.
    /// </summary>
    internal enum ManagedIdentityKeyType
    {
        // Represents a key stored using a secure key guard mechanism that provides hardware-level protection.
        KeyGuard,

        // Represents a key stored directly in hardware security modules or trusted platform modules.
        Hardware,

        // Represents a key stored in memory with software-based protection mechanisms.
        InMemory
    }
}

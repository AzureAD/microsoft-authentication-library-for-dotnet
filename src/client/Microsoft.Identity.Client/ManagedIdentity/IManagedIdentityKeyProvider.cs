// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// src/client/Microsoft.Identity.Client/ManagedIdentity/MiKeyAbstractions.cs
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal interface IManagedIdentityKeyProvider
    {
        Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(CancellationToken ct);
    }
}

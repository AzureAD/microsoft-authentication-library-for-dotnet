// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal interface IManagedIdentityKeyProvider
    {
        Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(ILoggerAdapter logger, CancellationToken ct);
    }
}

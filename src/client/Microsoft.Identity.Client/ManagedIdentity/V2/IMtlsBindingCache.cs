// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Abstraction over the in-memory + persisted cache for IMDSv2 mTLS binding certificates.
    /// </summary>
    internal interface IMtlsBindingCache
    {
        /// <summary>
        /// Returns a cached binding certificate for the given <paramref name="cacheKey"/>,
        /// or uses <paramref name="factory"/> to create, persist and return one when needed.
        /// </summary>
        Task<MtlsBindingInfo> GetOrCreateAsync(
            string cacheKey,
            Func<Task<MtlsBindingInfo>> factory,
            CancellationToken cancellationToken,
            ILoggerAdapter logger);
    }
}

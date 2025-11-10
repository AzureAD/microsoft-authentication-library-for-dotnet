// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal interface IMtlsBindingCache
    {
        Task<Tuple<X509Certificate2, string /*endpoint*/, string /*clientId*/>> GetOrCreateAsync(
            string cacheKey,
            Func<Task<Tuple<X509Certificate2, string, string>>> factory,
            CancellationToken cancellationToken,
            ILoggerAdapter logger);
    }
}

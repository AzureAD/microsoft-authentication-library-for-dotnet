// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser
{
    internal interface ITcpInterceptor
    {
        Task<Uri> ListenToSingleRequestAndRespondAsync(
            int port,
            Func<Uri, string> responseProducer,
            CancellationToken cancellationToken);
    }
}

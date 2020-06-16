// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal interface ISilentAuthRequestStrategy
    {
        Task PreRunAsync();

        Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken);
    }
}

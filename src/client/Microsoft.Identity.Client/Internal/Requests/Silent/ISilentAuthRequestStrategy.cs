// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Requests.Silent
{
    internal interface ISilentAuthRequestStrategy
    {
        Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken);
    }
}

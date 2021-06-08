// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Internal
{
    internal interface IAuthCodeRequestComponent
    {
        Task<Tuple<AuthorizationResult, string>> FetchAuthCodeAndPkceVerifierAsync(CancellationToken cancellationToken);

        Uri GetAuthorizationUriWithoutPkce();
    }
}

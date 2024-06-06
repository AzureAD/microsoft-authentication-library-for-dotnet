﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal interface IConfidentialClientApplicationExecutor
    {
        IServiceBundle ServiceBundle { get; }

        Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByAuthorizationCodeParameters authorizationCodeParameters,
            CancellationToken cancellationToken);

        Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenForClientParameters clientParameters,
            CancellationToken cancellationToken);

        Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenOnBehalfOfParameters onBehalfOfParameters,
            CancellationToken cancellationToken);

        Task<Uri> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            GetAuthorizationRequestUrlParameters authorizationRequestUrlParameters,
            CancellationToken cancellationToken);

        Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByUsernamePasswordParameters usernamePasswordParameters,
            CancellationToken cancellationToken);
    }
}

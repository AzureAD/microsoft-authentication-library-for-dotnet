﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Mats;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal class MatsClientApplicationBaseExecutor : AbstractMatsExecutor, IClientApplicationBaseExecutor
    {
        private readonly IClientApplicationBaseExecutor _executor;

        public MatsClientApplicationBaseExecutor(IClientApplicationBaseExecutor executor, IMats mats)
            : base(mats)
        {
            _executor = executor;
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenSilentParameters silentParameters,
            CancellationToken cancellationToken)
        {
            return ExecuteMatsAsync(
                commonParameters,
                async () => await _executor.ExecuteAsync(commonParameters, silentParameters, cancellationToken).ConfigureAwait(false));
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByRefreshTokenParameters byRefreshTokenParameters,
            CancellationToken cancellationToken)
        {
            return ExecuteMatsAsync(
                commonParameters,
                async () => await _executor.ExecuteAsync(commonParameters, byRefreshTokenParameters, cancellationToken).ConfigureAwait(false));
        }
    }
}

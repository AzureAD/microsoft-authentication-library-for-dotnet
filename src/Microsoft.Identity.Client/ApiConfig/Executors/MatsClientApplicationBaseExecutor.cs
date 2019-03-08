// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Mats;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal class MatsClientApplicationBaseExecutor : IClientApplicationBaseExecutor
    {
        private readonly IClientApplicationBaseExecutor _executor;
        private readonly IMats _mats;

        public MatsClientApplicationBaseExecutor(IClientApplicationBaseExecutor executor, IMats mats)
        {
            _executor = executor;
            _mats = mats;
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenSilentParameters silentParameters,
            CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(commonParameters, silentParameters, cancellationToken);
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByRefreshTokenParameters byRefreshTokenParameters,
            CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(commonParameters, byRefreshTokenParameters, cancellationToken);
        }
    }
}

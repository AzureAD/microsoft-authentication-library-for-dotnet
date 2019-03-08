// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Mats;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal class MatsPublicClientExecutor : IPublicClientApplicationExecutor
    {
        private readonly IPublicClientApplicationExecutor _executor;
        private readonly IMats _mats;

        public MatsPublicClientExecutor(IPublicClientApplicationExecutor executor, IMats mats)
        {
            _executor = executor;
            _mats = mats;
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenInteractiveParameters interactiveParameters,
            CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(commonParameters, interactiveParameters, cancellationToken);

            // TODO(mats): Wrap calls with MATS MsalAction calls to generate the appropriate telemetry
            //var actionHandle = _mats.StartAction(null, null);

            //try
            //{
            //    var result = await _executor.ExecuteAsync(commonParameters, interactiveParameters, cancellationToken).ConfigureAwait(false);
            //    _mats.EndAction(actionHandle, AuthOutcome.Succeeded, ErrorSource.None, null, null);
            //    return result;
            //}
            //catch (Exception ex)
            //{
            //    _mats.EndAction(actionHandle, AuthOutcome.Failed, ErrorSource.Client, ex.Message, ex.Message);
            //    throw;
            //}
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenWithDeviceCodeParameters withDeviceCodeParameters,
            CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(commonParameters, withDeviceCodeParameters, cancellationToken);
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByIntegratedWindowsAuthParameters integratedWindowsAuthParameters,
            CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(commonParameters, integratedWindowsAuthParameters, cancellationToken);
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByUsernamePasswordParameters usernamePasswordParameters,
            CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(commonParameters, usernamePasswordParameters, cancellationToken);
        }
    }
}

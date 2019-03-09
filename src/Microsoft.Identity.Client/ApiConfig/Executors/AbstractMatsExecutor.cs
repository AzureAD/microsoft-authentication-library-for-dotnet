// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Mats;
using Microsoft.Identity.Client.Mats.Internal;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal abstract class AbstractMatsExecutor
    {
        private readonly IMats _mats;

        protected AbstractMatsExecutor(IMats mats)
        {
            _mats = mats;
        }

        protected async Task<AuthenticationResult> ExecuteMatsAsync(
            AcquireTokenCommonParameters commonParameters,
            Func<Task<AuthenticationResult>> executorAction)
        {
            var actionHandle = _mats.StartAction(null, commonParameters.TelemetryCorrelationId.AsMatsCorrelationId());

            try
            {
                var result = await executorAction().ConfigureAwait(false);
                _mats.EndAction(actionHandle, result);
                return result;
            }
            catch (Exception ex)
            {
                // todo(mats):  add an EndAction(actionHandle, ex) method so we can do switch logic on the exception type, error codes, etc to properly
                // fill in the end action data?  this would be nice for unit testing as well.
                _mats.EndAction(actionHandle, AuthOutcome.Failed, ErrorSource.Client, ex.Message, ex.Message);
                throw;
            }
        }

        protected async Task<Uri> ExecuteMatsToUriAsync(
            AcquireTokenCommonParameters commonParameters,
            Func<Task<Uri>> executorAction)
        {
            var actionHandle = _mats.StartAction(null, commonParameters.TelemetryCorrelationId.AsMatsCorrelationId());

            try
            {
                Uri result = await executorAction().ConfigureAwait(false);
                _mats.EndAction(actionHandle, AuthOutcome.Succeeded, ErrorSource.None, string.Empty, string.Empty);
                return result;
            }
            catch (Exception ex)
            {
                _mats.EndAction(actionHandle, AuthOutcome.Failed, ErrorSource.Client, ex.Message, ex.Message);
                throw;
            }
        }
    }
}

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
        private readonly IMatsTelemetryClient _mats;

        protected AbstractMatsExecutor(IMatsTelemetryClient mats)
        {
            _mats = mats;
        }

        protected async Task<AuthenticationResult> ExecuteMatsAsync(
            AcquireTokenCommonParameters commonParameters,
            Func<Task<AuthenticationResult>> executorAction)
        {
            var action = _mats.StartAction(_mats.CreateScenario(), commonParameters.TelemetryCorrelationId.AsMatsCorrelationId());

            try
            {
                var result = await executorAction().ConfigureAwait(false);
                _mats.EndAction(action, result);
                return result;
            }
            catch (Exception ex)
            {
                _mats.EndAction(action, ex);
                throw;
            }
        }

        protected async Task<Uri> ExecuteMatsToUriAsync(
            AcquireTokenCommonParameters commonParameters,
            Func<Task<Uri>> executorAction)
        {
            var action = _mats.StartAction(null, commonParameters.TelemetryCorrelationId.AsMatsCorrelationId());

            try
            {
                Uri result = await executorAction().ConfigureAwait(false);
                _mats.EndAction(action, AuthOutcome.Succeeded, ErrorSource.None, string.Empty, string.Empty);
                return result;
            }
            catch (Exception ex)
            {
                _mats.EndAction(action, ex);
                throw;
            }
        }
    }
}

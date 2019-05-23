// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal class MatsConfidentialClientExecutor : AbstractMatsExecutor, IConfidentialClientApplicationExecutor
    {
        private readonly IConfidentialClientApplicationExecutor _executor;

        public MatsConfidentialClientExecutor(IConfidentialClientApplicationExecutor executor, ITelemetryClient mats)
            : base(mats)
        {
            _executor = executor;
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByAuthorizationCodeParameters authorizationCodeParameters,
            CancellationToken cancellationToken)
        {
            return ExecuteMatsAsync(
                commonParameters,
                async () => await _executor.ExecuteAsync(commonParameters, authorizationCodeParameters, cancellationToken).ConfigureAwait(false));
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenForClientParameters clientParameters,
            CancellationToken cancellationToken)
        {
            return ExecuteMatsAsync(
                commonParameters,
                async () => await _executor.ExecuteAsync(commonParameters, clientParameters, cancellationToken).ConfigureAwait(false));
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenOnBehalfOfParameters onBehalfOfParameters,
            CancellationToken cancellationToken)
        {
            return ExecuteMatsAsync(
                commonParameters,
                async () => await _executor.ExecuteAsync(commonParameters, onBehalfOfParameters, cancellationToken).ConfigureAwait(false));
        }

        public Task<Uri> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            GetAuthorizationRequestUrlParameters authorizationRequestUrlParameters,
            CancellationToken cancellationToken)
        {
            return ExecuteMatsToUriAsync(
                commonParameters,
                async () => await _executor.ExecuteAsync(commonParameters, authorizationRequestUrlParameters, cancellationToken).ConfigureAwait(false));
        }
    }
}

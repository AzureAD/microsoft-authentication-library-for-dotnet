// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Mats;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal class MatsConfidentialClientExecutor : IConfidentialClientApplicationExecutor
    {
        private readonly IConfidentialClientApplicationExecutor _executor;
        private readonly IMats _mats;

        public MatsConfidentialClientExecutor(IConfidentialClientApplicationExecutor executor, IMats mats)
        {
            _executor = executor;
            _mats = mats;
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByAuthorizationCodeParameters authorizationCodeParameters,
            CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(commonParameters, authorizationCodeParameters, cancellationToken);
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenForClientParameters clientParameters,
            CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(commonParameters, clientParameters, cancellationToken);
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenOnBehalfOfParameters onBehalfOfParameters,
            CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(commonParameters, onBehalfOfParameters, cancellationToken);
        }

        public Task<Uri> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            GetAuthorizationRequestUrlParameters authorizationRequestUrlParameters,
            CancellationToken cancellationToken)
        {
            return _executor.ExecuteAsync(commonParameters, authorizationRequestUrlParameters, cancellationToken);
        }
    }
}

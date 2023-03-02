// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide managed identity flow on mobile
#endif
    internal class ManagedIdentityExecutor : AbstractExecutor, IManagedIdentityApplicationExecutor
    {
        private readonly ManagedIdentityApplication _managedIdentityApplication;

        public ManagedIdentityExecutor(IServiceBundle serviceBundle, ManagedIdentityApplication managedIdentityApplication) 
            : base(serviceBundle)
        {
            ClientApplicationBase.GuardMobileFrameworks();

            _managedIdentityApplication = managedIdentityApplication;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId, cancellationToken);

            var requestParams = await _managedIdentityApplication.CreateRequestParametersAsync(
                commonParameters,
                requestContext,
                _managedIdentityApplication.AppTokenCacheInternal).ConfigureAwait(false);

            var handler = new ManagedIdentityAuthRequest(
                ServiceBundle,
                requestParams,
                managedIdentityParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

     
    }
}

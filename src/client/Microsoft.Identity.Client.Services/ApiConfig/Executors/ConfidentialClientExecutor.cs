// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    internal class ConfidentialClientExecutor : AbstractExecutor, IConfidentialClientApplicationExecutor
    {
        private readonly ConfidentialClientApplication _confidentialClientApplication;

        public ConfidentialClientExecutor(IServiceBundle serviceBundle, ConfidentialClientApplication confidentialClientApplication)
            : base(serviceBundle)
        {
            ClientApplicationBase.GuardMobileFrameworks();

            _confidentialClientApplication = confidentialClientApplication;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByAuthorizationCodeParameters authorizationCodeParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId, cancellationToken);

            var requestParams = await _confidentialClientApplication.CreateRequestParametersAsync(
                commonParameters,
                requestContext,
                _confidentialClientApplication.UserTokenCacheInternal).ConfigureAwait(false);
            requestParams.SendX5C = authorizationCodeParameters.SendX5C ?? false;

            var handler = new ConfidentialAuthCodeRequest(
                ServiceBundle,
                requestParams,
                authorizationCodeParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenForClientParameters clientParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId, cancellationToken);

            var requestParams = await _confidentialClientApplication.CreateRequestParametersAsync(
                commonParameters,
                requestContext,
                _confidentialClientApplication.AppTokenCacheInternal).ConfigureAwait(false);
       
            requestParams.SendX5C = clientParameters.SendX5C ?? false;
            
            var handler = new ClientCredentialRequest(
                ServiceBundle,
                requestParams,
                clientParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenOnBehalfOfParameters onBehalfOfParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId, cancellationToken);

            var requestParams = await _confidentialClientApplication.CreateRequestParametersAsync(
                commonParameters,
                requestContext,
                _confidentialClientApplication.UserTokenCacheInternal).ConfigureAwait(false);

            requestParams.SendX5C = onBehalfOfParameters.SendX5C ?? false;
            requestParams.UserAssertion = onBehalfOfParameters.UserAssertion;
            requestParams.LongRunningOboCacheKey = onBehalfOfParameters.LongRunningOboCacheKey;

            var handler = new OnBehalfOfRequest(
                ServiceBundle,
                requestParams,
                onBehalfOfParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<Uri> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            GetAuthorizationRequestUrlParameters authorizationRequestUrlParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId, cancellationToken);

            var requestParameters = await _confidentialClientApplication.CreateRequestParametersAsync(
                commonParameters,
                requestContext,
                _confidentialClientApplication.UserTokenCacheInternal).ConfigureAwait(false);

            requestParameters.Account = authorizationRequestUrlParameters.Account;
            requestParameters.LoginHint = authorizationRequestUrlParameters.LoginHint;
            requestParameters.CcsRoutingHint = authorizationRequestUrlParameters.CcsRoutingHint;

            if (!string.IsNullOrWhiteSpace(authorizationRequestUrlParameters.RedirectUri))
            {
                requestParameters.RedirectUri = new Uri(authorizationRequestUrlParameters.RedirectUri);
            }

            await requestParameters.AuthorityManager.RunInstanceDiscoveryAndValidationAsync().ConfigureAwait(false);
            var handler = new AuthCodeRequestComponent(
                requestParameters,
                authorizationRequestUrlParameters.ToInteractiveParameters());

            if (authorizationRequestUrlParameters.CodeVerifier != null)
            {
                return handler.GetAuthorizationUriWithPkce(authorizationRequestUrlParameters.CodeVerifier);
            }
            else
            {
                return handler.GetAuthorizationUriWithoutPkce();
            }
        }
    }
}

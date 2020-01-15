// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    internal class ConfidentialClientExecutor : AbstractExecutor, IConfidentialClientApplicationExecutor
    {
        private readonly ConfidentialClientApplication _confidentialClientApplication;

        public ConfidentialClientExecutor(IServiceBundle serviceBundle, ConfidentialClientApplication confidentialClientApplication)
            : base(serviceBundle, confidentialClientApplication)
        {
            _confidentialClientApplication = confidentialClientApplication;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByAuthorizationCodeParameters authorizationCodeParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId);

            var requestParams = _confidentialClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _confidentialClientApplication.UserTokenCacheInternal);
            requestParams.SendX5C = authorizationCodeParameters.SendX5C;

            var handler = new AuthorizationCodeRequest(
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
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId);

            var requestParams = _confidentialClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _confidentialClientApplication.AppTokenCacheInternal);

            requestParams.SendX5C = clientParameters.SendX5C;
            requestParams.IsClientCredentialRequest = true;

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
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId);

            var requestParams = _confidentialClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _confidentialClientApplication.UserTokenCacheInternal);

            requestParams.SendX5C = onBehalfOfParameters.SendX5C;
            requestParams.UserAssertion = onBehalfOfParameters.UserAssertion;

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
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId);

            var requestParameters = _confidentialClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _confidentialClientApplication.UserTokenCacheInternal);

            requestParameters.Account = authorizationRequestUrlParameters.Account;
            requestParameters.LoginHint = authorizationRequestUrlParameters.LoginHint;

            if (!string.IsNullOrWhiteSpace(authorizationRequestUrlParameters.RedirectUri))
            {
                requestParameters.RedirectUri = new Uri(authorizationRequestUrlParameters.RedirectUri);
            }

            var handler = new InteractiveRequest(
                ServiceBundle,
                requestParameters,
                authorizationRequestUrlParameters.ToInteractiveParameters(),
                null);

            // todo: need to pass through cancellation token here
            return await handler.CreateAuthorizationUriAsync().ConfigureAwait(false);
        }
    }
#endif
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal class PublicClientExecutor : AbstractExecutor, IPublicClientApplicationExecutor
    {
        private readonly PublicClientApplication _publicClientApplication;

        public PublicClientExecutor(IServiceBundle serviceBundle, PublicClientApplication publicClientApplication)
            : base(serviceBundle, publicClientApplication)
        {
            _publicClientApplication = publicClientApplication;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenInteractiveParameters interactiveParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            var requestParams = _publicClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _publicClientApplication.UserTokenCacheInternal);

            requestParams.LoginHint = interactiveParameters.LoginHint;
            requestParams.Account = interactiveParameters.Account;

            var handler = new InteractiveRequest(
                ServiceBundle,
                requestParams,
                interactiveParameters,
                CreateWebAuthenticationDialog(interactiveParameters, requestParams.RequestContext));

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);

        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenWithDeviceCodeParameters deviceCodeParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            var requestParams = _publicClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _publicClientApplication.UserTokenCacheInternal);

            var handler = new DeviceCodeRequest(
                ServiceBundle,
                requestParams,
                deviceCodeParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByIntegratedWindowsAuthParameters integratedWindowsAuthParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            var requestParams = _publicClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _publicClientApplication.UserTokenCacheInternal);

            var handler = new IntegratedWindowsAuthRequest(
                ServiceBundle,
                requestParams,
                integratedWindowsAuthParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByUsernamePasswordParameters usernamePasswordParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            var requestParams = _publicClientApplication.CreateRequestParameters(
                commonParameters,
                requestContext,
                _publicClientApplication.UserTokenCacheInternal);

            var handler = new UsernamePasswordRequest(
                ServiceBundle,
                requestParams,
                usernamePasswordParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        private IWebUI CreateWebAuthenticationDialog(
            AcquireTokenInteractiveParameters interactiveParameters,
            RequestContext requestContext)
        {
            if (interactiveParameters.CustomWebUi != null)
            {
                return new CustomWebUiHandler(interactiveParameters.CustomWebUi);
            }

            var coreUiParent = interactiveParameters.UiParent.CoreUiParent;

#if ANDROID || iOS
            coreUiParent.UseEmbeddedWebview = interactiveParameters.UseEmbeddedWebView;
#endif

#if WINDOWS_APP || DESKTOP
            // hidden web view can be used in both WinRT and desktop applications.
            coreUiParent.UseHiddenBrowser = interactiveParameters.Prompt.Equals(Prompt.Never);
#if WINDOWS_APP
            coreUiParent.UseCorporateNetwork = _publicClientApplication.AppConfig.UseCorporateNetwork;
#endif
#endif
            return ServiceBundle.PlatformProxy.GetWebUiFactory().CreateAuthenticationDialog(coreUiParent, requestContext);
        }

    }
}

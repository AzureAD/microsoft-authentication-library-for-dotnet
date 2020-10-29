// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Desktop.Internal.Requests;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal
{
    /// <summary>
    /// Responsible for getting an auth code
    /// </summary>
    internal class AuthCodeRequestComponent : IAuthCodeRequestComponent
    {
        private readonly AuthenticationRequestParameters _requestParams;
        private readonly AcquireTokenInteractiveParameters _interactiveParameters;
        private readonly IServiceBundle _serviceBundle;

        public AuthCodeRequestComponent(
            AuthenticationRequestParameters requestParams,
            AcquireTokenInteractiveParameters interactiveParameters)
        {
            _requestParams = requestParams ?? throw new ArgumentNullException(nameof(requestParams));
            _interactiveParameters = interactiveParameters ?? throw new ArgumentNullException(nameof(requestParams));
            _serviceBundle = _requestParams.RequestContext.ServiceBundle;
        }


        public async Task<Tuple<string, string>> FetchAuthCodeAndPkceVerifierAsync(
            CancellationToken cancellationToken)
        {
            var webUi = CreateWebAuthenticationDialog();
            return await FetchAuthCodeAndPkceInternalAsync(webUi, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Tuple<string, string>> FetchAuthCodeAndPkceInternalAsync(
            IWebUI webUi,
            CancellationToken cancellationToken)
        {
            RedirectUriHelper.Validate(_requestParams.RedirectUri);

            _requestParams.RedirectUri = webUi.UpdateRedirectUri(_requestParams.RedirectUri);

            Tuple<Uri, string, string> authorizationTuple = 
                AuthorizationUriBuilder.CreateAuthorizationUri(
                    _interactiveParameters.ToAuthorizationRequestParams(),
                    _requestParams, 
                    addPkceAndState: true);
                
            Uri authorizationUri = authorizationTuple.Item1;
            string state = authorizationTuple.Item2;
            string codeVerifier = authorizationTuple.Item3;

            var uiEvent = new UiEvent(_requestParams.RequestContext.CorrelationId.AsMatsCorrelationId());
            using (_requestParams.RequestContext.CreateTelemetryHelper(uiEvent))
            {
                var authorizationResult = await webUi.AcquireAuthorizationAsync(
                                           authorizationUri,
                                           _requestParams.RedirectUri,
                                           _requestParams.RequestContext,
                                           cancellationToken).ConfigureAwait(false);

                uiEvent.UserCancelled = authorizationResult.Status == AuthorizationStatus.UserCancel;
                uiEvent.AccessDenied = authorizationResult.Status == AuthorizationStatus.ProtocolError;

                VerifyAuthorizationResult(authorizationResult, state);

                return new Tuple<string, string>(authorizationResult.Code, codeVerifier);
            }

        }

         private IWebUI CreateWebAuthenticationDialog()
        {
            var pcaProxy = ((IPublicClientPlatformProxy)_serviceBundle.PlatformProxy);
            if (_interactiveParameters.CustomWebUi != null)
            {
                return new CustomWebUiHandler(_interactiveParameters.CustomWebUi);
            }

            CoreUIParent coreUiParent = _interactiveParameters.UiParent;

            coreUiParent.UseEmbeddedWebview = GetUseEmbeddedWebview(
                _interactiveParameters.UseEmbeddedWebView,
                pcaProxy.UseEmbeddedWebViewDefault);

#if DESKTOP
            // hidden web view can be used in both WinRT and desktop applications.
            coreUiParent.UseHiddenBrowser = _interactiveParameters.Prompt.Equals(Prompt.Never);
#endif
#if WINDOWS_APP
            coreUiParent.UseCorporateNetwork = _serviceBundle.Config.UseCorporateNetwork;
#endif
            return pcaProxy.GetWebUiFactory()
                .CreateAuthenticationDialog(coreUiParent, _requestParams.RequestContext);
        }

        private static bool GetUseEmbeddedWebview(WebViewPreference userPreference, bool defaultValue)
        {
            switch (userPreference)
            {
                case WebViewPreference.NotSpecified:
                    return defaultValue;
                case WebViewPreference.Embedded:
                    return true;
                case WebViewPreference.System:
                    return false;
                default:
                    throw new NotImplementedException("Unknown option");
            }
        }

        private void VerifyAuthorizationResult(AuthorizationResult authorizationResult, string originalState)
        {
            if (authorizationResult.Status == AuthorizationStatus.Success &&
                originalState != null &&
                !originalState.Equals(authorizationResult.State,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new MsalClientException(
                    MsalError.StateMismatchError,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        MsalErrorMessage.StateMismatchErrorMessage,
                        authorizationResult.State,
                        originalState));
            }

            if (authorizationResult.Error == OAuth2Error.LoginRequired)
            {
                throw new MsalUiRequiredException(
                    MsalError.NoPromptFailedError,
                    MsalErrorMessage.NoPromptFailedErrorMessage,
                    null,
                    UiRequiredExceptionClassification.PromptNeverFailed);
            }

            if (authorizationResult.Status == AuthorizationStatus.UserCancel)
            {
                _requestParams.RequestContext.Logger.Info(LogMessages.UserCancelledAuthentication);
                throw new MsalClientException(
                    authorizationResult.Error,
                    authorizationResult.ErrorDescription ?? "User canceled authentication.");
            }

            if (authorizationResult.Status != AuthorizationStatus.Success)
            {
                _requestParams.RequestContext.Logger.ErrorPii(
                    LogMessages.AuthorizationResultWasNotSuccessful + authorizationResult.ErrorDescription ?? "Unknown error.",
                    LogMessages.AuthorizationResultWasNotSuccessful);

                throw new MsalServiceException(
                    authorizationResult.Error,
                    !string.IsNullOrEmpty(authorizationResult.ErrorDescription) ? authorizationResult.ErrorDescription : "Unknown error");
            }
        }
    }
}

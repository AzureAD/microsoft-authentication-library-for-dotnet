// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal
{
    /// <summary>
    /// Responsible for getting an auth code
    /// </summary>
    internal class AuthCodeRequestComponent : AuthCodeRequestComponentBase, IAuthCodeRequestComponent
    {
        private readonly AcquireTokenInteractiveParameters _interactiveParameters;
        private readonly IServiceBundlePublic _serviceBundlePublic;

        public AuthCodeRequestComponent(
            AuthenticationRequestParameters requestParams,
            AcquireTokenInteractiveParameters interactiveParameters) : base(requestParams, interactiveParameters)
        {
            _interactiveParameters = interactiveParameters ?? throw new ArgumentNullException(nameof(interactiveParameters));
            _serviceBundlePublic = (IServiceBundlePublic)_requestParams.RequestContext.ServiceBundle;
        }

        public new async Task<Tuple<AuthorizationResult, string>> FetchAuthCodeAndPkceVerifierAsync(
            CancellationToken cancellationToken)
        {
            var webUi = CreateWebAuthenticationDialog();
            return await FetchAuthCodeAndPkceInternalAsync(webUi, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Tuple<AuthorizationResult, string>> FetchAuthCodeAndPkceInternalAsync(
            IWebUI webUi,
            CancellationToken cancellationToken)
        {
            RedirectUriHelper.Validate(_requestParams.RedirectUri);

            _requestParams.RedirectUri = webUi.UpdateRedirectUri(_requestParams.RedirectUri);

            Tuple<Uri, string, string> authorizationTuple = CreateAuthorizationUri(true);
            Uri authorizationUri = authorizationTuple.Item1;
            string state = authorizationTuple.Item2;
            string codeVerifier = authorizationTuple.Item3;

            var authorizationResult = await webUi.AcquireAuthorizationAsync(
                                       authorizationUri,
                                       _requestParams.RedirectUri,
                                       _requestParams.RequestContext,
                                       cancellationToken).ConfigureAwait(false);

            VerifyAuthorizationResult(authorizationResult, state);

            return new Tuple<AuthorizationResult, string>(authorizationResult, codeVerifier);

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

        private IWebUI CreateWebAuthenticationDialog()
        {
            if (_interactiveParameters.CustomWebUi != null)
            {
                return new CustomWebUiHandler(_interactiveParameters.CustomWebUi);
            }

            CoreUIParent coreUiParent = _interactiveParameters.UiParent;

#if WINDOWS_APP || DESKTOP
            // hidden web view can be used in both WinRT and desktop applications.
            //coreUiParent.UseHiddenBrowser = _interactiveParameters.Prompt.Equals(Prompt.Never);
#if WINDOWS_APP
            coreUiParent.UseCorporateNetwork = _serviceBundlePublic.Config.UseCorporateNetwork;
#endif
#endif            
            return _serviceBundlePublic.PlatformProxyPublic.GetWebUiFactory((ApplicationConfigurationPublic)_requestParams.AppConfig)
                .CreateAuthenticationDialog(
                coreUiParent,
                _interactiveParameters.UseEmbeddedWebView,
                _requestParams.RequestContext);
        }
    }
}

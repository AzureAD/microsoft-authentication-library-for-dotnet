// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Connectivity;
using Windows.Security.Authentication.Web;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.UI;
using System.Threading;

namespace Microsoft.Identity.Client.Platforms.uap
{
    internal class WebUI : IWebUI
    {
        private readonly bool _useCorporateNetwork;
        private readonly bool _silentMode;

        public RequestContext RequestContext { get; set; }

        public WebUI(CoreUIParent parent, RequestContext requestContext)
        {
            _useCorporateNetwork = parent.UseCorporateNetwork;
            _silentMode = parent.UseHiddenBrowser;
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            bool ssoMode = string.Equals(redirectUri.OriginalString, Constants.UapWEBRedirectUri, StringComparison.OrdinalIgnoreCase);

            WebAuthenticationResult webAuthenticationResult;
            WebAuthenticationOptions options = (_useCorporateNetwork &&
                                                (ssoMode || redirectUri.Scheme == Constants.MsAppScheme))
                ? WebAuthenticationOptions.UseCorporateNetwork
                : WebAuthenticationOptions.None;

            if (_silentMode)
            {
                options |= WebAuthenticationOptions.SilentMode;
            }

            try
            {
                webAuthenticationResult = await CoreApplication.MainView.CoreWindow.Dispatcher.RunTaskAsync(
                        async () =>
                        {
                            if (ssoMode)
                            {
                                return await
                                    WebAuthenticationBroker.AuthenticateAsync(options, authorizationUri)
                                        .AsTask()
                                        .ConfigureAwait(false);
                            }
                            else
                            {
                                return await WebAuthenticationBroker
                                    .AuthenticateAsync(options, authorizationUri, redirectUri)
                                    .AsTask()
                                    .ConfigureAwait(false);
                            }
                        })
                    .ConfigureAwait(false);
            }

            catch (Exception ex)
            {
                requestContext.Logger.ErrorPii(ex);
                throw new MsalException(MsalError.AuthenticationUiFailedError, "WAB authentication failed",
                    ex);
            }

            AuthorizationResult result = ProcessAuthorizationResult(webAuthenticationResult);

            return result;
        }

        private static AuthorizationResult ProcessAuthorizationResult(WebAuthenticationResult webAuthenticationResult)
        {
            AuthorizationResult result;
            switch (webAuthenticationResult.ResponseStatus)
            {
            case WebAuthenticationStatus.Success:
                result = AuthorizationResult.FromUri(webAuthenticationResult.ResponseData);
                break;
            case WebAuthenticationStatus.ErrorHttp:
                result = AuthorizationResult.FromStatus(AuthorizationStatus.ErrorHttp);
                result.Code = webAuthenticationResult.ResponseErrorDetail.ToString(CultureInfo.InvariantCulture);
                break;
            case WebAuthenticationStatus.UserCancel:
                result = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
                break;
            default:
                result = AuthorizationResult.FromStatus(AuthorizationStatus.UnknownError);
                break;
            }

            return result;
        }

        public Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
            return redirectUri;
        }
    }
}

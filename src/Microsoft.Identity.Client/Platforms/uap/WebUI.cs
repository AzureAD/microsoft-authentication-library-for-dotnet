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
        private readonly bool useCorporateNetwork;
        private readonly bool silentMode;

        public RequestContext RequestContext { get; set; }

        public WebUI(CoreUIParent parent, RequestContext requestContext)
        {
            useCorporateNetwork = parent.UseCorporateNetwork;
            silentMode = parent.UseHiddenBrowser;
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            bool ssoMode = string.Equals(redirectUri.OriginalString, Constants.UapWEBRedirectUri, StringComparison.OrdinalIgnoreCase);

            WebAuthenticationResult webAuthenticationResult;
            WebAuthenticationOptions options = (useCorporateNetwork &&
                                                (ssoMode || redirectUri.Scheme == Constants.MsAppScheme))
                ? WebAuthenticationOptions.UseCorporateNetwork
                : WebAuthenticationOptions.None;

            ThrowOnNetworkDown();
            if (silentMode)
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

            AuthorizationResult result = ProcessAuthorizationResult(webAuthenticationResult, requestContext);

            return result;
        }

        private void ThrowOnNetworkDown()
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            var isConnected = (profile != null
                               && profile.GetNetworkConnectivityLevel() ==
                               NetworkConnectivityLevel.InternetAccess);
            if (!isConnected)
            {
                throw new MsalClientException(MsalError.NetworkNotAvailableError, MsalErrorMessage.NetworkNotAvailable);
            }
        }

        private static AuthorizationResult ProcessAuthorizationResult(WebAuthenticationResult webAuthenticationResult,
            RequestContext requestContext)
        {
            AuthorizationResult result;
            switch (webAuthenticationResult.ResponseStatus)
            {
            case WebAuthenticationStatus.Success:
                result = AuthorizationResult.FromUri(webAuthenticationResult.ResponseData);
                break;
            case WebAuthenticationStatus.ErrorHttp:
                result = new AuthorizationResult(AuthorizationStatus.ErrorHttp)
                {
                    Code = webAuthenticationResult.ResponseErrorDetail.ToString(CultureInfo.InvariantCulture)
                };
                break;
            case WebAuthenticationStatus.UserCancel:
                result = new AuthorizationResult(AuthorizationStatus.UserCancel);
                break;
            default:
                result = new AuthorizationResult(AuthorizationStatus.UnknownError);
                break;
            }

            return result;
        }


        public void ValidateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
        }
    }
}

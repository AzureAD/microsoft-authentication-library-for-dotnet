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
using Microsoft.Identity.Client.Utils;
using System.Threading;


namespace Microsoft.Identity.Client.Platforms.uap
{
    internal class WebUI : IWebUI
    {
        private const int WABRetryAttempts = 2;

        private readonly bool _useCorporateNetwork;
        private readonly bool _silentMode;
        private readonly RequestContext _requestContext;

        public WebUI(CoreUIParent parent, RequestContext requestContext)
        {
            _useCorporateNetwork = parent.UseCorporateNetwork;
            _silentMode = parent.UseHiddenBrowser;
            _requestContext = requestContext;
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
                webAuthenticationResult =   await
                RetryOperationHelper.ExecuteWithRetryAsync(
                    () => InvokeWABOnMainThreadAsync(authorizationUri, redirectUri, ssoMode, options),
                    WABRetryAttempts,
                    onAttemptFailed: (attemptNumber, exception) =>
                    {
                        _requestContext.Logger.Warning($"Attempt {attemptNumber} to call WAB failed");
                        _requestContext.Logger.WarningPii(exception);
                    })
                .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                requestContext.Logger.ErrorPii(ex);
                throw new MsalException(
                    MsalError.AuthenticationUiFailedError,
                    "Web Authentication Broker (WAB) authentication failed. To collect WAB logs, please follow https://aka.ms/msal-net-wab-logs",
                    ex);
            }

            AuthorizationResult result = ProcessAuthorizationResult(webAuthenticationResult);
            return result;
        }


        public async Task<WebAuthenticationResult> InvokeWABOnMainThreadAsync(Uri authorizationUri,
            Uri redirectUri,
            bool ssoMode,
            WebAuthenticationOptions options)
        {
            return await CoreApplication.MainView.CoreWindow.Dispatcher.RunTaskAsync(
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

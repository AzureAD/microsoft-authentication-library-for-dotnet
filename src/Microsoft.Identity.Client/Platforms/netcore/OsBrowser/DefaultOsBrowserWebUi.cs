// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.netcore.OsBrowser
{
    internal class DefaultOsBrowserWebUi : IWebUI
    {
        // TODO (bogavril): Make these configurable
        private const string CloseWindowSuccessHtml = @"<html>
  <head><title>Authentication Complete</title></head>
  <body>
    Authentication complete. You can return to the application. Feel free to close this browser tab.
  </body>
</html>";

        private const string CloseWindowFailureHtml = @"<html>
  <head><title>Authentication Failed</title></head>
  <body>
    Authentication failed. You can return to the application. Feel free to close this browser tab.
</br></br></br></br>
    Error details: error {0} error_description: {1}
  </body>
</html>";

        private readonly ITcpInterceptor _tcpInterceptor;
        private readonly ICoreLogger _logger;
        private readonly IPlatformProxy _platformProxy;

        public DefaultOsBrowserWebUi(
            ICoreLogger logger,
            IPlatformProxy platformProxy,
            /* for test */ ITcpInterceptor tcpInterceptor = null)
        {
            _logger = logger;
            _platformProxy = platformProxy;
            _tcpInterceptor = tcpInterceptor ?? new TcpInterceptor(_logger);
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            var authCodeUri = await InterceptAuthorizationUriAsync(
                authorizationUri,
                redirectUri,
                cancellationToken)
                .ConfigureAwait(true);


            if (!authCodeUri.Authority.Equals(redirectUri.Authority, StringComparison.OrdinalIgnoreCase) ||
               !authCodeUri.AbsolutePath.Equals(redirectUri.AbsolutePath))
            {
                throw new MsalClientException(
                    MsalError.CustomWebUiRedirectUriMismatch,
                    MsalErrorMessage.CustomWebUiRedirectUriMismatch(
                        authCodeUri.AbsolutePath,
                        redirectUri.AbsolutePath));
            }

            return AuthorizationResult.FromUri(authCodeUri.OriginalString);
        }

        public void ValidateRedirectUri(Uri redirectUri)
        {
            if (!redirectUri.IsLoopback)
            {
                throw new MsalClientException(
                    MsalError.LoopbackRedirectUri,
                    "Only loopback redirect uri is supported with this WebUI. Configure http://localhost or http://localhost:port during app registration. See https://aka.ms/msal-net-os-browser for details");
            }

            int port = redirectUri.Port;
            if (port < 1 || port == 80)
            {
                // TODO: bogavril - generate a port if one is not given.
                throw new MsalClientException(
                     MsalError.LoopbackRedirectUri,
                    "Please configure a redirect uri with a valid, non-default, port number, i.e. > 0, not 80");
            }
        }


        private async Task<Uri> InterceptAuthorizationUriAsync(
            Uri authorizationUri,
            Uri redirectUri,
            CancellationToken cancellationToken)
        {
            await _platformProxy.StartDefaultOsBrowserAsync(authorizationUri.ToString()).ConfigureAwait(false);

            return await _tcpInterceptor.ListenToSingleRequestAndRespondAsync(
                redirectUri.Port,
                GetMessageToShowInBroswerAfterAuth,
                cancellationToken)
            .ConfigureAwait(false);
        }

        private static string GetMessageToShowInBroswerAfterAuth(Uri authCodeUri)
        {
            // Parse the uri to understand if an error was returned. This is done just to show the user a nice error message in the browser.
            var authorizationResult = AuthorizationResult.FromUri(authCodeUri.OriginalString);

            if (!string.IsNullOrEmpty(authorizationResult.Error))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    CloseWindowFailureHtml,
                    authorizationResult.Error,
                    authorizationResult.ErrorDescription);
            }

            return CloseWindowSuccessHtml;
        }
    }
}

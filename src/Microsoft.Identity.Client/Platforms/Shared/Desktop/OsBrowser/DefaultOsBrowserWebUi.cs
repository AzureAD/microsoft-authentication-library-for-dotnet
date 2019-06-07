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

namespace Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser
{
    internal class DefaultOsBrowserWebUi : IWebUI
    {
        internal const string DefaultSuccessHtml = @"<html>
  <head><title>Authentication Complete</title></head>
  <body>
    Authentication complete. You can return to the application. Feel free to close this browser tab.
  </body>
</html>";

        internal const string DefaultFailureHtml = @"<html>
  <head><title>Authentication Failed</title></head>
  <body>
    Authentication failed. You can return to the application. Feel free to close this browser tab.
</br></br></br></br>
    Error details: error {0} error_description: {1}
  </body>
</html>";

        private readonly ITcpInterceptor _tcpInterceptor;
        private readonly ICoreLogger _logger;
        private readonly SystemWebViewOptions _webViewOptions;
        private readonly IPlatformProxy _platformProxy;

        public DefaultOsBrowserWebUi(
            IPlatformProxy proxy,
            ICoreLogger logger,
            SystemWebViewOptions webViewOptions,
            /* for test */ ITcpInterceptor tcpInterceptor = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webViewOptions = webViewOptions;
            _platformProxy = proxy ?? throw new ArgumentNullException(nameof(proxy));

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
                    MsalError.LoopbackResponseUriMismatch,
                    MsalErrorMessage.RedirectUriMismatch(
                        authCodeUri.AbsolutePath,
                        redirectUri.AbsolutePath));
            }

            return AuthorizationResult.FromUri(authCodeUri.OriginalString);
        }

        public Uri UpdateRedirectUri(Uri redirectUri)
        {
            if (!redirectUri.IsLoopback)
            {
                throw new MsalClientException(
                    MsalError.LoopbackRedirectUri,
                    string.Format(CultureInfo.InvariantCulture,
                        "Only loopback redirect uri is supported, but {0} was found. " +
                        "Configure http://localhost or http://localhost:port both during app registration and when you create the PublicClientApplication object. " +
                        "See https://aka.ms/msal-net-os-browser for details", redirectUri.AbsoluteUri));
            }

            // AAD does not allow https:\\localhost redirects from any port
            if (redirectUri.Scheme != "http")
            {
                throw new MsalClientException(
                    MsalError.LoopbackRedirectUri,
                    string.Format(CultureInfo.InvariantCulture,
                        "Only http uri scheme is supported, but {0} was found. " +
                        "Configure http://localhost or http://localhost:port both during app registration and when you create the PublicClientApplication object. " +
                        "See https://aka.ms/msal-net-os-browser for details", redirectUri.Scheme));
            }

            return FindFreeLocalhostRedirectUri(redirectUri);
        }

        private static Uri FindFreeLocalhostRedirectUri(Uri redirectUri)
        {
            if (redirectUri.Port > 0 && redirectUri.Port != 80)
            {
                return redirectUri;
            }


            TcpListener listner = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listner.Start();
                int port = ((IPEndPoint)listner.LocalEndpoint).Port;
                return new Uri("http://localhost:" + port);
            }
            finally
            {
                listner?.Stop();
            }
        }

        private async Task<Uri> InterceptAuthorizationUriAsync(
            Uri authorizationUri,
            Uri redirectUri,
            CancellationToken cancellationToken)
        {
            Func<Uri, Task> defaultBrowserAction = (Uri u) => _platformProxy.StartDefaultOsBrowserAsync(u.AbsoluteUri);
            Func<Uri, Task> openBrowserAction = _webViewOptions?.OpenBrowserAsync ?? defaultBrowserAction;

            await openBrowserAction(authorizationUri).ConfigureAwait(false);

            return await _tcpInterceptor.ListenToSingleRequestAndRespondAsync(
                redirectUri.Port,
                GetResponseMessage,
                cancellationToken)
            .ConfigureAwait(false);
        }

        internal /* internal for testing only */ string GetResponseMessage(Uri authCodeUri)
        {
            // Parse the uri to understand if an error was returned. This is done just to show the user a nice error message in the browser.
            var authorizationResult = AuthorizationResult.FromUri(authCodeUri.OriginalString);

            if (!string.IsNullOrEmpty(authorizationResult.Error))
            {
                _logger.Warning($"Default OS Browser intercepted an Uri with an error: " +
                    $"{authorizationResult.Error} {authorizationResult.ErrorDescription}");

                string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        _webViewOptions?.HtmlMessageError ?? DefaultFailureHtml,
                        authorizationResult.Error,
                        authorizationResult.ErrorDescription);

                return GetMessage(_webViewOptions?.BrowserRedirectError, errorMessage);
            }

            return GetMessage(
                _webViewOptions?.BrowserRedirectSuccess,
                _webViewOptions?.HtmlMessageSuccess ?? DefaultSuccessHtml);
        }

        private string GetMessage(Uri redirectUri, string message)
        {
            if (redirectUri != null)
            {
                return $"HTTP/1.1 302 Found\r\nLocation: {redirectUri.ToString()}";
            }

            return $"HTTP/1.1 200 OK\r\n\r\n{message}";
        }
    }
}

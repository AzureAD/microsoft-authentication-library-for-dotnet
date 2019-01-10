using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;
using OpenQA.Selenium;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    internal class SeleniumWebUI : IWebUI
    {
        private readonly Action<IWebDriver> _seleniumAutomationLogic;
        private readonly TimeSpan _timeout;

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
  </body>
</html>";

        public SeleniumWebUI(Action<IWebDriver> seleniumAutomationLogic, TimeSpan timeout)
        {
            _seleniumAutomationLogic = seleniumAutomationLogic;
            _timeout = timeout;
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext)
        {
            if (redirectUri.IsDefaultPort)
            {
                throw new InvalidOperationException("Cannot listen to localhost (no port), please call UpdateRedirectUri to get a free localhost:port address");
            }

            AuthorizationResult result = await SeleniumAcquireAuthAsync(
                authorizationUri,
                redirectUri)
                .ConfigureAwait(true);

            return result;
        }

        public void ValidateRedirectUri(Uri redirectUri)
        {
            if (!redirectUri.IsLoopback)
            {
                throw new ArgumentException("Only loopback redirect uri");
            }

            if (redirectUri.IsDefaultPort)
            {
                throw new ArgumentException("Port required");
            }
        }

        private IWebDriver InitDriverAndGoToUrl(string url)
        {
            IWebDriver driver = null;
            try
            {
                driver = SeleniumExtensions.CreateDefaultWebDriver();
                driver.Navigate().GoToUrl(url);

                return driver;
            }
            catch (Exception)
            {
                driver?.Dispose();
                throw;
            }
        }

        private async Task<AuthorizationResult> SeleniumAcquireAuthAsync(
            Uri authorizationUri,
            Uri redirectUri)
        {
            using (var driver = InitDriverAndGoToUrl(authorizationUri.OriginalString))
            using (var listener = new SingleMessageTcpListener(redirectUri.Port)) // starts listening
            {
                AuthorizationResult authResult = null;
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(_timeout);

                var listenForAuthCodeTask = listener.ListenToSingleRequestAndRespondAsync(
                    (uri) =>
                    {
                        Trace.WriteLine("Intercepted an auth code url: " + uri.ToString());
                        authResult = new AuthorizationResult(AuthorizationStatus.Success, uri.ToString());
                        switch (authResult.Status)
                        {
                            case AuthorizationStatus.Success:
                                return CloseWindowSuccessHtml;
                            default:
                                return CloseWindowFailureHtml;
                        }
                    },
                    cancellationTokenSource.Token);

                try
                {

                    // Run the tcp listener and the selenium automation in parallel
                    Task seleniumAutomationTask = Task.Run(() =>
                    {
                        _seleniumAutomationLogic(driver);
                    });

                    await Task.WhenAll(seleniumAutomationTask, listenForAuthCodeTask).ConfigureAwait(false);
                    return authResult;

                }
                catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    var result = new AuthorizationResult(AuthorizationStatus.UserCancel)
                    {
                        ErrorDescription = "Listening for an auth code was cancelled or has timed out.",
                        Error = "system_browser_cancel_or_timeout_exception"
                    };

                    return await Task.FromResult(result).ConfigureAwait(false);
                }
                catch (SocketException ex)
                {
                    var result = new AuthorizationResult(AuthorizationStatus.UnknownError)
                    {
                        ErrorDescription = ex.Message + " socket error code: " + ex.SocketErrorCode,
                        Error = "system_browser_socket_exception"
                    };

                    return await Task.FromResult(result).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var result = new AuthorizationResult(AuthorizationStatus.UnknownError)
                    {
                        ErrorDescription = ex.Message,
                        Error = "system_browser_waiting_exception"
                    };

                    return await Task.FromResult(result).ConfigureAwait(false);
                }
            }
        }
    }
}

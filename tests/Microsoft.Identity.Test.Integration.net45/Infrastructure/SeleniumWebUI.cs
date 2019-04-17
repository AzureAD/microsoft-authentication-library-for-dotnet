using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.UI;
using OpenQA.Selenium;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    internal class SeleniumWebUI : ICustomWebUi
    {
        private readonly Action<IWebDriver> _seleniumAutomationLogic;

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

        public SeleniumWebUI(Action<IWebDriver> seleniumAutomationLogic)
        {
            _seleniumAutomationLogic = seleniumAutomationLogic;
        }


        public async Task<Uri> AcquireAuthorizationCodeAsync(
            Uri authorizationUri,
            Uri redirectUri, 
            CancellationToken cancellationToken)
        {
            if (redirectUri.IsDefaultPort)
            {
                throw new InvalidOperationException("Cannot listen to localhost (no port), please call UpdateRedirectUri to get a free localhost:port address");
            }

            Uri result = await SeleniumAcquireAuthAsync(
                authorizationUri,
                redirectUri,
                cancellationToken)
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

        public static string FindFreeLocalhostRedirectUri()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return "http://localhost:" + port;
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

        private async Task<Uri> SeleniumAcquireAuthAsync(
            Uri authorizationUri,
            Uri redirectUri, 
            CancellationToken cancellationToken)
        {
            using (var driver = InitDriverAndGoToUrl(authorizationUri.OriginalString))
            using (var listener = new SingleMessageTcpListener(redirectUri.Port)) // starts listening
            {
                Uri authCodeUri = null;
                var listenForAuthCodeTask = listener.ListenToSingleRequestAndRespondAsync(
                    (uri) =>
                    {
                        Trace.WriteLine("Intercepted an auth code url: " + uri.ToString());
                        authCodeUri = uri;

                        return GetMessageToShowInBroswerAfterAuth(uri);
                    },
                    cancellationToken);

                try
                {

                    // Run the tcp listener and the selenium automation in parallel
                    var seleniumAutomationTask = Task.Run(() =>
                    {
                        _seleniumAutomationLogic(driver);
                    });

                    await Task.WhenAll(seleniumAutomationTask, listenForAuthCodeTask).ConfigureAwait(false);
                    return authCodeUri;

                }
                catch (SocketException ex)
                {
                    throw new MsalClientException(
                        "selenium_ui_socket_error",
                        "A socket exception occured " + ex.Message + " socket error code " + ex.SocketErrorCode);
                }
            }
        }

        private static string GetMessageToShowInBroswerAfterAuth(Uri uri)
        {
            // Parse the uri to understand if an error was returned. This is done just to show the user a nice error message in the browser.
            var authCodeQueryKeyValue = HttpUtility.ParseQueryString(uri.Query);
            string errorString = authCodeQueryKeyValue.Get("error");
            if (!string.IsNullOrEmpty(errorString))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    CloseWindowFailureHtml,
                    errorString,
                    authCodeQueryKeyValue.Get("error_description"));
            }

            return CloseWindowSuccessHtml;
        }
    }
}

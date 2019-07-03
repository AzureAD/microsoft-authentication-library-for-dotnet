// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    internal class SeleniumWebUI : ICustomWebUi
    {
        private readonly Action<IWebDriver> _seleniumAutomationLogic;
        private readonly TestContext _testContext;
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

        public SeleniumWebUI(Action<IWebDriver> seleniumAutomationLogic, TestContext testContext)
        {
            _seleniumAutomationLogic = seleniumAutomationLogic;
            _testContext = testContext;
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
            NullLogger nullLogger = new NullLogger();
            using (var driver = InitDriverAndGoToUrl(authorizationUri.OriginalString))
            {
                var listener = new TcpInterceptor(nullLogger);
                Uri authCodeUri = null;
                var listenForAuthCodeTask = listener.ListenToSingleRequestAndRespondAsync(
                    redirectUri.Port,
                    (uri) =>
                    {
                        Trace.WriteLine("Intercepted an auth code url: " + uri.ToString());
                        authCodeUri = uri;

                        return GetMessageToShowInBroswerAfterAuth(uri);
                    },
                    cancellationToken);

                // Run the TCP listener and the selenium automation in parallel
                var seleniumAutomationTask = Task.Factory.StartNew(() => _seleniumAutomationLogic(driver));

                await Task.WhenAny(seleniumAutomationTask, listenForAuthCodeTask)
                    .ConfigureAwait(false);

                if (listenForAuthCodeTask.Status == TaskStatus.RanToCompletion)
                {
                    return authCodeUri;
                }

                if (seleniumAutomationTask.IsFaulted)
                {
                    Trace.WriteLine("The selenium automation failed: " + seleniumAutomationTask.Exception.Message);
                    RecordException(driver, seleniumAutomationTask.Exception);
                    throw seleniumAutomationTask.Exception;
                }

                if (listenForAuthCodeTask.IsCanceled)
                {
                    Trace.WriteLine("The TCP listener has timed out (or was canceled).");
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Assert.Fail("The TCP listener is in a canceled state, but cancellation has not been requested!");
                    }

                    throw new OperationCanceledException(cancellationToken);
                }

                if (listenForAuthCodeTask.IsFaulted )
                {
                    Trace.WriteLine("The TCP listener failed.");
                    RecordException(driver, listenForAuthCodeTask.Exception);
                    throw listenForAuthCodeTask.Exception;
                }

                throw new InvalidOperationException(
                    $"Unknown exception: selenium status: {seleniumAutomationTask.Status} TCP listener status: {listenForAuthCodeTask.Status}. " +
                    $"Possible cause - the redirect Uri used is not the one configured." +
                    $" A screen shot will be stored in the test results for you to inspect.");
            }
        }

        private void RecordException(IWebDriver driver, Exception ex)
        {
            Trace.WriteLine("Error occurred while acquiring auth. Possible cause: the browser never finished auth or the Selenium automation failed. A screenshot may be available");
            Trace.WriteLine($"Exception: {ex.GetType()} with message {ex.ToString()}");
            if (ex is AggregateException aggEx)
            {
                foreach (var e in aggEx.Flatten().InnerExceptions)
                {
                    Trace.WriteLine($"Aggregate exception detail: {e.ToString()}");
                }
            }
            Trace.WriteLine("Page source:");
            Trace.WriteLine(driver?.PageSource);
            driver.SaveScreenshot(_testContext);
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

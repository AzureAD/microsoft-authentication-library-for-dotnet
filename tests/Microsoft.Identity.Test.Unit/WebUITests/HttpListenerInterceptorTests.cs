// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Shared.DefaultOSBrowser;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class HttpListenerInterceptorTests
    {
        [TestMethod]
        [DataRow("GET")]
        [DataRow("POST")]
        public async Task HttpListenerRejectsNonFormPostRequestsAsync(string method)
        {
            var listenerInterceptor = new HttpListenerInterceptor(Substitute.For<ILoggerAdapter>());
            int port = FindFreeLocalhostPort();
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Task<AuthorizationResponse> listenTask = listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                port, string.Empty, _ => new MessageAndHttpCode(HttpStatusCode.OK, "OK"), cancellation.Token);

            try
            {
                using var httpClient = new HttpClient(new HttpClientHandler { UseProxy = false });
                string uri = $"http://localhost:{port}/";
                using var request = new HttpRequestMessage(new HttpMethod(method), uri);
                using HttpResponseMessage rejection = await httpClient.SendAsync(
                    request, cancellation.Token).ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.BadRequest, rejection.StatusCode);
                Assert.IsFalse(listenTask.IsCompleted);

                using var content = new StringContent(
                    "code=auth_code&state=expected_state",
                    System.Text.Encoding.UTF8,
                    "application/x-www-form-urlencoded");
                using HttpResponseMessage response = await httpClient.PostAsync(
                    uri, content, cancellation.Token).ConfigureAwait(false);
                AuthorizationResponse callback = await listenTask.ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.IsTrue(callback.IsFormPost);
            }
            finally
            {
                cancellation.Cancel();
            }
        }

        [TestMethod]
        public async Task Cancellation_BeforeTopLevelCall_Async()
        {

            HttpListenerInterceptor listenerInterceptor = new HttpListenerInterceptor(
                Substitute.For<ILoggerAdapter>());
            CancellationTokenSource cts = new CancellationTokenSource();
            listenerInterceptor.TestBeforeTopLevelCall = () => cts.Cancel();

            int port = FindFreeLocalhostPort();

            // Start the listener in the background
            await AssertException.TaskThrowsAsync<OperationCanceledException>(
                () => listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                    port,
                    string.Empty,
                    (_) => { return new MessageAndHttpCode(HttpStatusCode.OK, "OK"); },
                    cts.Token))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Cancellation_BeforeStart_Async()
        {

            HttpListenerInterceptor listenerInterceptor = new HttpListenerInterceptor(
                Substitute.For<ILoggerAdapter>());
            CancellationTokenSource cts = new CancellationTokenSource();
            int port = FindFreeLocalhostPort();

            listenerInterceptor.TestBeforeStart = (_) => cts.Cancel();

            // Start the listener in the background
            await AssertException.TaskThrowsAsync<OperationCanceledException>(
                () => listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                    port,
                    string.Empty,
                    (_) => { return new MessageAndHttpCode(HttpStatusCode.OK, "OK"); },
                    cts.Token))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ValidateHttpListenerRedirectUriAsync()
        {
            HttpListenerInterceptor listenerInterceptor = new(Substitute.For<ILoggerAdapter>());

            int port = FindFreeLocalhostPort();
            listenerInterceptor.TestBeforeStart = (url) => Assert.AreEqual($"http://localhost:{port}/TestPath/", url);

            // Start listener in the background
            Task<AuthorizationResponse> listenTask = listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                port,
                "/TestPath/",
                (_) => new MessageAndHttpCode(HttpStatusCode.OK, "OK"),
                CancellationToken.None);

            // Ensure the listener is bound before making the request
            await EnsureListenerIsReady(port, TimeSpan.FromSeconds(2)).ConfigureAwait(false);

            // Issue an HTTP POST request (form_post requires POST)
            await SendPostMessageToPortAsync(port, "TestPath", "code=test_code&state=test_state").ConfigureAwait(false);

            // Wait for listener to handle request with a timeout
            bool completed = (await Task.WhenAny(listenTask, Task.Delay(5000)).ConfigureAwait(false)) == listenTask;

            // Assert
            Assert.IsTrue(completed, "Listener did not complete within timeout.");
            Assert.StartsWith($"http://localhost:{port}/TestPath/", listenTask.Result.RequestUri.ToString(), "Request URI should include the custom path");
        }

        [TestMethod]
        [DataRow(null, false)]
        [DataRow("", false)]
        [DataRow("false", false)]
        [DataRow("0", false)]
        [DataRow("yes", false)]
        [DataRow(" true ", false)]
        [DataRow("true", true)]
        [DataRow("TRUE", true)]
        [DataRow("TrUe", true)]
        [DataRow("1", true)]
        [DoNotParallelize]
        public async Task PortForwarding_EnvironmentVariableControlsListenerPrefixes_Async(string value, bool enabled)
        {
            // Arrange
            const string EnvVariable = "MSAL_ALLOW_SYSTEM_BROWSER_PORT_FORWARDING";
            string previousValue = Environment.GetEnvironmentVariable(EnvVariable);
            try
            {
                Environment.SetEnvironmentVariable(EnvVariable, value);
                bool expectWildcard = enabled && DesktopOsHelper.IsLinux();
                int port = FindFreeLocalhostPort();
                List<string> listenerUrls = new();
                HttpListenerInterceptor listenerInterceptor = new(Substitute.For<ILoggerAdapter>());
                using CancellationTokenSource cts = new();
                listenerInterceptor.TestBeforeGetContext = () => cts.Cancel();
                listenerInterceptor.TestBeforeStart = url =>
                {
                    listenerUrls.Add(url);
                    if (!expectWildcard || url == $"http://*:{port}/")
                    {
                        cts.Cancel();
                        cts.Token.ThrowIfCancellationRequested();
                    }
                };

                // Act
                await AssertException.TaskThrowsAsync<OperationCanceledException>(
                    () => listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                        port,
                        string.Empty,
                        (_) => new MessageAndHttpCode(HttpStatusCode.OK, "OK"),
                        cts.Token))
                    .ConfigureAwait(false);

                // Assert
                if (expectWildcard)
                {
                    CollectionAssert.Contains(listenerUrls, $"http://*:{port}/");
                }
                else
                {
                    CollectionAssert.AreEqual(new[] { $"http://localhost:{port}/" }, listenerUrls);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvVariable, previousValue);
            }
        }

#if NET_CORE
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task PortForwarding_PreservesOnlyIpv6Localhost_Async(bool localhostResolvesToIpv6)
        {
            if (!OperatingSystem.IsLinux())
            {
                Assert.Inconclusive("Wildcard HttpListener behavior is verified on Linux.");
            }

            // Arrange
            using CancellationTokenSource cts = new();
            int port = FindFreeLocalhostPort();
            List<string> listenerUrls = new();
            HttpListenerInterceptor listenerInterceptor = new(
                Substitute.For<ILoggerAdapter>(),
                allowPortForwarding: true,
                localhostResolvesToIpv6: () => localhostResolvesToIpv6);

            listenerInterceptor.TestBeforeStart = url =>
            {
                listenerUrls.Add(url);
                if (url == $"http://*:{port}/TestPath/")
                {
                    cts.Cancel();
                    cts.Token.ThrowIfCancellationRequested();
                }
            };

            // Act
            await AssertException.TaskThrowsAsync<OperationCanceledException>(
                () => listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                    port,
                    "/TestPath/",
                    (_) => new MessageAndHttpCode(HttpStatusCode.OK, "OK"),
                    cts.Token))
                .ConfigureAwait(false);

            // Assert
            CollectionAssert.AreEqual(
                localhostResolvesToIpv6 ? new[]
                {
                    $"http://localhost:{port}/TestPath/",
                    $"http://*:{port}/TestPath/"
                } : new[] { $"http://*:{port}/TestPath/" },
                listenerUrls);
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task PortForwarding_AcceptsIpv4ForwardedCallback_Async()
        {
            if (!OperatingSystem.IsLinux())
            {
                Assert.Inconclusive("Wildcard HttpListener behavior is verified on Linux.");
            }

            // Arrange
            using EnvVariableContext envVariableContext = new();
            Environment.SetEnvironmentVariable("MSAL_ALLOW_SYSTEM_BROWSER_PORT_FORWARDING", "true");
            int port = FindFreeLocalhostPort();
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
            HttpListenerInterceptor listenerInterceptor = new(Substitute.For<ILoggerAdapter>());

            Task<AuthorizationResponse> listenTask = listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                port,
                string.Empty,
                (_) => new MessageAndHttpCode(HttpStatusCode.OK, "OK"),
                cts.Token);

            using HttpClient httpClient = new();
            using HttpRequestMessage request = new(HttpMethod.Post, $"http://127.0.0.1:{port}/");
            request.Headers.Host = $"localhost:{port}";
            request.Content = new StringContent(
                "code=test_code&state=test_state",
                System.Text.Encoding.UTF8,
                "application/x-www-form-urlencoded");

            // Act
            using HttpResponseMessage response = await httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
            AuthorizationResponse authorizationResponse = await listenTask.ConfigureAwait(false);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("localhost", authorizationResponse.RequestUri.Host);
            Assert.IsTrue(authorizationResponse.IsFormPost);
        }
#endif

        /// <summary>
        /// Ensures the HTTP listener is ready by checking the port binding.
        /// Fixes: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5152
        /// </summary>
        private static async Task EnsureListenerIsReady(int port, TimeSpan timeout)
        {
            using CancellationTokenSource cts = new(timeout);
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    using TcpClient client = new();
                    await client.ConnectAsync("localhost", port).ConfigureAwait(false);
                    return; // If connection succeeds, listener is ready
                }
                catch
                {
                    await Task.Delay(100).ConfigureAwait(false); // Retry after delay
                }
            }
            throw new TimeoutException($"Listener did not start within {timeout.TotalSeconds} seconds.");
        }

        [TestMethod]
        public async Task Cancellation_BeforeGetContext_Async()
        {

            HttpListenerInterceptor listenerInterceptor = new HttpListenerInterceptor(
                Substitute.For<ILoggerAdapter>());
            CancellationTokenSource cts = new CancellationTokenSource();
            int port = FindFreeLocalhostPort();

            listenerInterceptor.TestBeforeGetContext = () => cts.Cancel();

            // Start the listener in the background
            await AssertException.TaskThrowsAsync<OperationCanceledException>(
                () => listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                    port,
                    string.Empty,
                    (_) => { return new MessageAndHttpCode(HttpStatusCode.OK, "OK"); },
                    cts.Token))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HttpListenerHandlesPostDataAsync()
        {
            HttpListenerInterceptor listenerInterceptor = new HttpListenerInterceptor(
                Substitute.For<ILoggerAdapter>());

            int port = FindFreeLocalhostPort();

            // Start the listener in the background
            Task<AuthorizationResponse> listenTask = listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                port,
                string.Empty,
                (_) => { return new MessageAndHttpCode(HttpStatusCode.OK, "OK"); },
                CancellationToken.None);

            // Issue an HTTP POST request with form data (simulating form_post response mode)
            await SendPostMessageToPortAsync(port, string.Empty, "code=auth_code_value&state=state_value").ConfigureAwait(false);

            // Wait for the listener to complete
            listenTask.Wait(2000 /* 2s timeout */);

            // Assert
            Assert.IsTrue(listenTask.IsCompleted);
            Assert.IsNotNull(listenTask.Result);
            Assert.IsTrue(listenTask.Result.IsFormPost, "Response should be identified as form_post");
            Assert.IsNotNull(listenTask.Result.PostData, "POST data should be captured");
            
            // Verify the POST data contains the expected values
            string postDataString = System.Text.Encoding.UTF8.GetString(listenTask.Result.PostData);
            Assert.Contains("code=auth_code_value", postDataString);
            Assert.Contains("state=state_value", postDataString);
            
            // Verify the request URI is clean (no query params)
            Assert.AreEqual("/", listenTask.Result.RequestUri.AbsolutePath);
            Assert.IsTrue(string.IsNullOrEmpty(listenTask.Result.RequestUri.Query) || 
                         listenTask.Result.RequestUri.Query == "?", 
                         "Request URI should not contain query parameters when using form_post");
        }

        [TestMethod]
        [DataRow(null, false)]
        [DataRow("incorrect_state", false)]
        [DataRow("incorrect_state", true)]
        public async Task HttpListener_RejectsInvalidStateAndContinuesAsync(string state, bool cancelWaiting)
        {
            var logger = Substitute.For<ILoggerAdapter>();
            var listenerInterceptor = new HttpListenerInterceptor(logger);
            var webUi = new DefaultOsBrowserWebUi(
                Substitute.For<IPlatformProxy>(), logger, null, listenerInterceptor);
            var requestContext = new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid(), null);
            int port = FindFreeLocalhostPort();
            string uri = $"http://localhost:{port}/";
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Task<AuthorizationResult> authorizationTask = webUi.AcquireAuthorizationAsync(
                new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/authorize?state=expected_state"),
                new Uri(uri),
                requestContext,
                cancellation.Token);

            try
            {
                using var httpClient = new HttpClient(new HttpClientHandler { UseProxy = false });
                string errorData = "error=access_denied&error_description=" +
                    Uri.EscapeDataString("<b>must_not_be_reflected</b>");
                if (state is not null)
                {
                    errorData += "&state=" + Uri.EscapeDataString(state);
                }

                using var errorContent = new StringContent(
                    errorData, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                using HttpResponseMessage rejection = await httpClient.PostAsync(
                    uri, errorContent, cancellation.Token).ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.BadRequest, rejection.StatusCode);
                Assert.AreEqual(string.Empty, await rejection.Content.ReadAsStringAsync().ConfigureAwait(false));
                Assert.IsFalse(authorizationTask.IsCompleted);

                if (cancelWaiting)
                {
                    cancellation.Cancel();
                    await AssertException.TaskThrowsAsync<OperationCanceledException>(
                        () => authorizationTask).ConfigureAwait(false);
                    return;
                }

                using var successContent = new StringContent(
                    "code=auth_code&state=expected_state",
                    System.Text.Encoding.UTF8,
                    "application/x-www-form-urlencoded");
                using HttpResponseMessage response = await httpClient.PostAsync(
                    uri, successContent, cancellation.Token).ConfigureAwait(false);
                AuthorizationResult result = await authorizationTask.ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(AuthorizationStatus.Success, result.Status);
                Assert.AreEqual("auth_code", result.Code);
                Assert.AreEqual("expected_state", result.State);
            }
            finally
            {
                cancellation.Cancel();
            }
        }

        private async Task SendPostMessageToPortAsync(int port, string path, string postData)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var content = new StringContent(postData, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                string uri = string.IsNullOrEmpty(path) 
                    ? $"http://localhost:{port}/" 
                    : $"http://localhost:{port}/{path}/";
                await httpClient.PostAsync(uri, content).ConfigureAwait(false);
            }
        }

        private static int FindFreeLocalhostPort()
        {
            TcpListener listner = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listner.Start();
                int port = ((IPEndPoint)listner.LocalEndpoint).Port;
                return port;
            }
            finally
            {
                listner?.Stop();
            }
        }
    }
}

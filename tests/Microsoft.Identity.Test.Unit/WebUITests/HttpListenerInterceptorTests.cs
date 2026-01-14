// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.Shared.DefaultOSBrowser;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class HttpListenerInterceptorTests
    {
        [TestMethod]
        public async Task HttpListenerRejectsGetRequestAsync()
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

            // Give listener more time to start accepting connections
            await Task.Delay(500).ConfigureAwait(false);

            // Issue an HTTP GET request (should be rejected for security)
            // We don't care if the HTTP client throws - we care that the listener rejects it
            try
            {
                await SendMessageToPortAsync(port, string.Empty).ConfigureAwait(false);
            }
            catch
            {
                // The HTTP client may throw if the listener closes the connection
                // This is expected and fine - we'll verify the listener's behavior
            }

            // Wait for the listener to complete or fault (without throwing)
            await Task.WhenAny(listenTask, Task.Delay(5000)).ConfigureAwait(false);

            // Assert - should throw security exception
            Assert.IsTrue(listenTask.IsCompleted, "Listener task should complete within timeout");
            Assert.IsTrue(listenTask.IsFaulted, "GET request should cause the task to fault");
            Assert.IsNotNull(listenTask.Exception, "Exception should be captured");
            Assert.IsInstanceOfType(listenTask.Exception.InnerException, typeof(MsalClientException));
            
            var msalEx = (MsalClientException)listenTask.Exception.InnerException;
            Assert.AreEqual(MsalError.AuthenticationFailed, msalEx.ErrorCode);
            Assert.IsTrue(msalEx.Message.Contains("Expected POST request"), "Error message should explain POST is required");
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
            Assert.IsTrue(listenTask.Result.RequestUri.ToString().StartsWith($"http://localhost:{port}/TestPath/"), 
                "Request URI should include the custom path");
        }

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
            Assert.IsTrue(postDataString.Contains("code=auth_code_value"));
            Assert.IsTrue(postDataString.Contains("state=state_value"));
            
            // Verify the request URI is clean (no query params)
            Assert.AreEqual("/", listenTask.Result.RequestUri.AbsolutePath);
            Assert.IsTrue(string.IsNullOrEmpty(listenTask.Result.RequestUri.Query) || 
                         listenTask.Result.RequestUri.Query == "?", 
                         "Request URI should not contain query parameters when using form_post");
        }

        private async Task SendMessageToPortAsync(int port, string path)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                await httpClient.GetAsync(GetLocalhostUriWithParams(port, path)).ConfigureAwait(false);
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

        private static string GetLocalhostUriWithParams(int port, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "http://localhost:" + port + "/?param1=val1";
            }

            return "http://localhost:" + port + "/" + path + "/?param1=val1";
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

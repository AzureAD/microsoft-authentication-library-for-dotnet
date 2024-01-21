// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
        public async Task HttpListenerCompletesAsync()
        {

            HttpListenerInterceptor listenerInterceptor = new HttpListenerInterceptor(
                Substitute.For<ILoggerAdapter>());

            int port = FindFreeLocalhostPort();

            // Start the listener in the background
            Task<Uri> listenTask = listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                port,
                string.Empty,
                (_) => { return new MessageAndHttpCode(HttpStatusCode.OK, "OK"); },
                CancellationToken.None);

            // Issue an HTTP request on the main thread
            await SendMessageToPortAsync(port, string.Empty).ConfigureAwait(false);

            // Wait for the listener to do its stuff
            listenTask.Wait(2000 /* 2s timeout */);

            // Assert
            Assert.IsTrue(listenTask.IsCompleted);
            Assert.AreEqual(GetLocalhostUriWithParams(port, string.Empty), listenTask.Result.ToString());
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
            HttpListenerInterceptor listenerInterceptor = new HttpListenerInterceptor(
                Substitute.For<ILoggerAdapter>());

            int port = FindFreeLocalhostPort();

            listenerInterceptor.TestBeforeStart = (url) => Assert.AreEqual(@"http://localhost:" + port + @"/TestPath/", url);

            // Start the listener in the background
            Task<Uri> listenTask = listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                port,
                "/TestPath/",
                (_) => { return new MessageAndHttpCode(HttpStatusCode.OK, "OK"); },
                CancellationToken.None);

            // Issue an HTTP request on the main thread
            await SendMessageToPortAsync(port, "TestPath").ConfigureAwait(false);

            // Wait for the listener to do its stuff
            listenTask.Wait(2000 /* 2s timeout */);

            // Assert
            Assert.IsTrue(listenTask.IsCompleted);
            Assert.AreEqual(GetLocalhostUriWithParams(port, "TestPath"), listenTask.Result.ToString());
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

        private async Task SendMessageToPortAsync(int port, string path)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                await httpClient.GetAsync(GetLocalhostUriWithParams(port, path)).ConfigureAwait(false);
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

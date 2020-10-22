// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Shared.DefaultOSBrowser;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
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
        private readonly RequestContext _requestContext = new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid());

        [TestMethod]
        public async Task HttpListenerCompletes()
        {

            HttpListenerInterceptor listenerInterceptor = new HttpListenerInterceptor(
                Substitute.For<ICoreLogger>());

            int port = FindFreeLocalhostPort();

            // Start the listener in the background
            Task<Uri> listenTask = listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                port,
                (u) => { return new MessageAndHttpCode(HttpStatusCode.OK, "ok"); },
                CancellationToken.None);

            // Issue an http request on the main thread
            await SendMessageToPortAsync(port).ConfigureAwait(false);

            // Wait for the listner to do its stuff
            listenTask.Wait(1000 /* 1s timeout */);

            // Assert
            Assert.IsTrue(listenTask.IsCompleted);
            Assert.AreEqual(GetLocalhostUriWithParams(port), listenTask.Result.ToString());
        }

        [TestMethod]
        public async Task Cancellation_BeforeTopLevelCall_Async()
        {

            HttpListenerInterceptor listenerInterceptor = new HttpListenerInterceptor(
                Substitute.For<ICoreLogger>());
            CancellationTokenSource cts = new CancellationTokenSource();
            listenerInterceptor.TestBeforeTopLevelCall = () => cts.Cancel();

            int port = FindFreeLocalhostPort();

            // Start the listener in the background
            await AssertException.TaskThrowsAsync<OperationCanceledException>(
                () => listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                    port,
                    (u) => { return new MessageAndHttpCode(HttpStatusCode.OK, "ok"); },
                    cts.Token))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Cancellation_BeforeStart_Async()
        {

            HttpListenerInterceptor listenerInterceptor = new HttpListenerInterceptor(
                Substitute.For<ICoreLogger>());
            CancellationTokenSource cts = new CancellationTokenSource();
            int port = FindFreeLocalhostPort();

            listenerInterceptor.TestBeforeStart = () => cts.Cancel();

            // Start the listener in the background
            await AssertException.TaskThrowsAsync<OperationCanceledException>(
                () => listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                    port,
                    (u) => { return new MessageAndHttpCode(HttpStatusCode.OK, "ok"); },
                    cts.Token))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Cancellation_BeforeGetContext_Async()
        {

            HttpListenerInterceptor listenerInterceptor = new HttpListenerInterceptor(
                Substitute.For<ICoreLogger>());
            CancellationTokenSource cts = new CancellationTokenSource();
            int port = FindFreeLocalhostPort();

            listenerInterceptor.TestBeforeGetContext = () => cts.Cancel();

            // Start the listener in the background
            await AssertException.TaskThrowsAsync<OperationCanceledException>(
                () => listenerInterceptor.ListenToSingleRequestAndRespondAsync(
                    port,
                    (u) => { return new MessageAndHttpCode(HttpStatusCode.OK, "ok"); },
                    cts.Token))
                .ConfigureAwait(false);
        }

        private async Task SendMessageToPortAsync(int port)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                await httpClient.GetAsync(GetLocalhostUriWithParams(port)).ConfigureAwait(false);
            }
        }

        private static string GetLocalhostUriWithParams(int port)
        {
            return "http://localhost:" + port + "/?param1=val1";
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

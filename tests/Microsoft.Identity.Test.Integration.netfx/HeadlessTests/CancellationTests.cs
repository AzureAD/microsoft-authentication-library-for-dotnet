// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License.

#if NET_CORE // run these only on .net core as they do not hit any platform specific part

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.Platforms.netcore;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{

    [TestClass]
    public class CancellationTests
    {
        [TestMethod]
        public async Task DefaultOsBrowser_IsCancellable_AfterAWhile_Async()
        {
            // Arrange
            var redirectUri = SeleniumWebUI.FindFreeLocalhostRedirectUri();
            var cts = new CancellationTokenSource();
            IPublicClientApplication pca = InitWithCustomPlatformProxy(redirectUri);

            // Act - start with cancellation not requested
            var tokenTask = pca // do not wait for this to finish
                .AcquireTokenInteractive(TestConstants.s_graphScopes)
                .WithUseEmbeddedWebView(false)
                .ExecuteAsync(cts.Token)
                .ConfigureAwait(false);

            // Wait a bit to allow the webUI to start listening 
            await Task.Delay(300).ConfigureAwait(false);

            // Cancel token acquisition while waiting for the browser to respond
            cts.Cancel();

            await ValidateOperationCancelledAsync(redirectUri, cts, tokenTask).ConfigureAwait(false);
            ValidatePortIsFree(new Uri(redirectUri).Port);
        }

        [TestMethod]
        public async Task DefaultOsBrowser_IsCancellable_StartsCancelled_Async()
        {
            // Arrange
            var redirectUri = SeleniumWebUI.FindFreeLocalhostRedirectUri();
            var cts = new CancellationTokenSource();
            IPublicClientApplication pca = InitWithCustomPlatformProxy(redirectUri);

            // Act Cancel token acquisition mediately
            cts.Cancel();

            var tokenTask = pca // do not wait for this to finish
                .AcquireTokenInteractive(TestConstants.s_graphScopes)
                .WithUseEmbeddedWebView(false)
                .ExecuteAsync(cts.Token)
                .ConfigureAwait(false);

            await ValidateOperationCancelledAsync(redirectUri, cts, tokenTask).ConfigureAwait(false);
            ValidatePortIsFree(new Uri(redirectUri).Port);
        }
        
        private async Task ValidateOperationCancelledAsync(string redirectUri, CancellationTokenSource cts, System.Runtime.CompilerServices.ConfiguredTaskAwaitable<AuthenticationResult> tokenTask)
        {
            // Assert
            var ex = await AssertException.TaskThrowsAsync<OperationCanceledException>(async () =>
            {
                await tokenTask;
                return;
            }).ConfigureAwait(false);
            Assert.AreEqual(cts.Token, ex.CancellationToken, "Cancellation exceptions SHOULD expose the original cancellation token");
        }

        private static IPublicClientApplication InitWithCustomPlatformProxy(string redirectUri)
        {
            // Use a real platform proxy but block StartDefaultOsBrowserAsync as we do not want an actual
            // browser to pop-up during tests
            var platformProxy = Substitute.ForPartsOf<NetCorePlatformProxy>(new NullLogger());
            platformProxy.WhenForAnyArgs(x => x.StartDefaultOsBrowserAsync(default, false)).DoNotCallBase();

            IPublicClientApplication pca = PublicClientApplicationBuilder
                    .Create("1d18b3b0-251b-4714-a02a-9956cec86c2d") // Any app that accepts http://localhost redirect
                    .WithRedirectUri(redirectUri)
                    .WithPlatformProxy(platformProxy)
                    .WithTestLogging()
                    .Build();
            return pca;
        }

        private void ValidatePortIsFree(int port)
        {
            TcpListener l = null;
            try
            {
                // will throw if port is in use
                l = new TcpListener(IPAddress.Loopback, port);
                l.Start();
            }
            finally
            {
                l?.Stop();
            }
        }
    }
}
#endif


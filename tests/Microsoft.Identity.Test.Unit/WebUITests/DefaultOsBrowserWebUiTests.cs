// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    internal class TestTcpInterceptor : IUriInterceptor
    {
        private readonly Uri _expectedUri;
        public Func<Uri, string> ResponseProducer { get; }

        public TestTcpInterceptor(Uri expectedUri)
        {
            _expectedUri = expectedUri;
        }

        public Task<Uri> ListenToSingleRequestAndRespondAsync(int port, string path, Func<Uri, MessageAndHttpCode> responseProducer, CancellationToken cancellationToken)
        {
            return Task.FromResult(_expectedUri);
        }
    }

    [TestClass]
    public class DefaultOsBrowserWebUiTests : TestBase
    {
        private const string TestAuthorizationRequestUri = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?scope=offline_access+openid+profile+user.read&response_type=code&client_id=1d18b3b0-251b-4714-a02a-9956cec86c2d&redirect_uri=http%3A%2F%2Flocalhost%3A50997&client-request-id=ea9aa98c-b190-40a3-96a7-1d11978ad131&x-client-SKU=MSAL.NetCore&x-client-Ver=3.0.4.0&x-client-OS=Microsoft+Windows+10.0.18362+&prompt=select_account&code_challenge=Zz9TikND5pVrkTRYWb6UfJwBr_hTacy5-rc9aBD_G8c&code_challenge_method=S256&state=901e7d87-6f49-4f9f-9fa7-e6b8c32d5b9595bc1797-dacc-4ff1-b9e9-0df81be286c7";
        private const string TestRedirectUri = "http://localhost:50997";
        private const string TestAuthorizationResponseUri = "http://localhost:50997/?code=OAQABAAIAAADCoMpjJXrxTq9VG9te-7FX1n0H3n1cOvvLstfUYt_wAqcm96iwQKYzTWLqkz44aUnx4mswa7tn53DFy03fIJie9zOUjk5y6R9vU-rhCSUTLTJR6wUdqsbZfqgRpcCHRPHgmOFk7c3MqJ6WF5Y9AfQgXLaXsZN5vy7ZqS9viU0-NXxKDuBx17yqsT0FPvuoO_0yEZkuVkwd_x_fuUpejHqmORRPfdS-rN6e-7TwfbpsjvUl_eZ2BbzOSJu9rRltWqK-cBVkBhmt3jYEXVWsuTFRD9GHPELscdMJxkwqeOyA8-Lt6zCskKQMq_aAwSPR34CxA9YXoLy-psqjeMDLA5ieP5rmdoNcGBPSXS-imNMKfFSxHN_df6rqpQCOShJ_SmuBFY6qfcARgXpAlobRiUHat-K5heDVJTude47uE_NCSdmRJVZzY1dOeVEJ6f6O1TgR8EHq_MOSyc9HTUU0CpYvf8zePZIjn4jFPv4CZwvdmc4sOCntWrPxxj0JfRval58-aueRgnyhkm9G23FG4oCWWjydaKp5EytHhyYYf_qztsycUkL3Z2Ox7brQ8_Sj1IQr14J3G2FUYgwjuvi6RYK3cvXPM6oUrhOlQcvx03y10xAtizogcA5UR2m8GIpDkm4GEMYX4yYcvBUI6y0qKHmjnZuS5UsymUhUbNG8kEsnI0WTODZ4zYlEHweTsTXq1QNawZqxAW-ZsQ9EbrEbuDFaybJtNYFuHkm1kUjUwpsbZXFLnTUI6CKKDNlUdvPpbiENgapB_p_AgLl3L5KihfY8AkVbVgHZVAcpDClEu_autQZa2jGvPEQka-oKpHqIFZbDEi4qB4yrkU_hDsjf-EqnIAA&state=901e7d87-6f49-4f9f-9fa7-e6b8c32d5b9595bc1797-dacc-4ff1-b9e9-0df81be286c7&session_state=1b37b349-61fe-4ad5-a049-9f8eadfded26";
        private const string TestErrorAuthorizationResponseUri = "http://localhost:50997/?error=errorMsg&error_description=errorDesc";
        private const int TestPort = 50997;
        private IUriInterceptor _tcpInterceptor;
        private IPlatformProxy _platformProxy;
        private ILoggerAdapter _logger;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _tcpInterceptor = Substitute.For<IUriInterceptor>();
            _platformProxy = Substitute.For<IPlatformProxy>();
            _logger = Substitute.For<ILoggerAdapter>();

        }

        private DefaultOsBrowserWebUi CreateTestWebUI(SystemWebViewOptions options = null)
        {
            return new DefaultOsBrowserWebUi(_platformProxy, _logger, options, _tcpInterceptor);
        }

        [TestMethod]
        public async Task DefaultOsBrowserWebUi_HappyPath_Async()
        {
            var webUI = CreateTestWebUI();
            AuthorizationResult authorizationResult = await AcquireAuthCodeAsync(webUI)
               .ConfigureAwait(false);

            // Assert
            Assert.AreEqual(AuthorizationStatus.Success, authorizationResult.Status);
            Assert.IsFalse(string.IsNullOrEmpty(authorizationResult.Code));

            await _tcpInterceptor.Received(1).ListenToSingleRequestAndRespondAsync(
                TestPort, "/", Arg.Any<Func<Uri, MessageAndHttpCode>>(), CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory(TestCategories.Regression)] //#1773
        public async Task HttpListenerException_Cancellation_Async()
        {
            var webUI = CreateTestWebUI();
            var requestContext = new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid());

            CancellationTokenSource cts = new CancellationTokenSource();
            _tcpInterceptor.When(x => x.ListenToSingleRequestAndRespondAsync(
                TestPort,
                "/",
                Arg.Any<Func<Uri, MessageAndHttpCode>>(),
                cts.Token))
               .Do(_ =>
               {
                   cts.Cancel();
                   throw new HttpListenerException();
               });

            // Act
            await AssertException.TaskThrowsAsync<OperationCanceledException>(
                () => webUI.AcquireAuthorizationAsync(
                    new Uri(TestAuthorizationRequestUri),
                    new Uri(TestRedirectUri),
                    requestContext,
                    cts.Token))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DefaultOsBrowserWebUi_CustomBrowser_Async()
        {
            bool customOpenBrowserCalled = false;
            var options = new SystemWebViewOptions()
            {
                OpenBrowserAsync = (Uri _) =>
                {
                    customOpenBrowserCalled = true;
                    return Task.FromResult(0);
                }
            };

            var webUI = CreateTestWebUI(options);
            var requestContext = new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid());
            var responseUri = new Uri(TestAuthorizationResponseUri);

            _tcpInterceptor.ListenToSingleRequestAndRespondAsync(
                TestPort,
                "/",
                Arg.Any<Func<Uri, MessageAndHttpCode>>(),
                CancellationToken.None)
               .Returns(Task.FromResult(responseUri));

            // Act
            AuthorizationResult authorizationResult = await webUI.AcquireAuthorizationAsync(
                new Uri(TestAuthorizationRequestUri),
                new Uri(TestRedirectUri),
                requestContext,
                CancellationToken.None).ConfigureAwait(false);

            // Assert that we didn't open the browser using platform proxy
            await _platformProxy.DidNotReceiveWithAnyArgs().StartDefaultOsBrowserAsync(default, requestContext.ServiceBundle.Config.IsBrokerEnabled)
                .ConfigureAwait(false);

            await _tcpInterceptor.Received(1).ListenToSingleRequestAndRespondAsync(
                TestPort, "/", Arg.Any<Func<Uri, MessageAndHttpCode>>(), CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(customOpenBrowserCalled);
        }

        [TestMethod]
        public async Task DefaultOsBrowserWebUi_ReturnUriInvalid_Async()
        {
            string differentPortRedirectUri = TestAuthorizationResponseUri.Replace(TestRedirectUri, "http://localhost:1111");

            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => AcquireAuthCodeAsync(CreateTestWebUI(), responseUriString: differentPortRedirectUri))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.LoopbackResponseUriMismatch, ex.ErrorCode);
        }

        private async Task<AuthorizationResult> AcquireAuthCodeAsync(
            IWebUI webUI,
            string redirectUri = TestRedirectUri,
            string requestUri = TestAuthorizationRequestUri,
            string responseUriString = TestAuthorizationResponseUri)
        {
            // Arrange
            var requestContext = new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid());
            var responseUri = new Uri(responseUriString);

            _tcpInterceptor.ListenToSingleRequestAndRespondAsync(
                TestPort,
                "/",
                Arg.Any<Func<Uri, MessageAndHttpCode>>(),
                CancellationToken.None)
               .Returns(Task.FromResult(responseUri));

            // Act
            AuthorizationResult authorizationResult = await webUI.AcquireAuthorizationAsync(
                new Uri(requestUri),
                new Uri(redirectUri),
                requestContext,
                CancellationToken.None).ConfigureAwait(false);

            // Assert that we opened the browser
            await _platformProxy.Received(1).StartDefaultOsBrowserAsync(requestUri, requestContext.ServiceBundle.Config.IsBrokerEnabled)
                .ConfigureAwait(false);

            return authorizationResult;

        }

        [TestMethod]
        public void NewRedirectUriCanBeGenerated()
        {
            // Arrange
            var tcpInterceptor = Substitute.For<IUriInterceptor>();

            IWebUI webUi = new DefaultOsBrowserWebUi(_platformProxy, _logger, null, tcpInterceptor);

            AssertNewUriIsGenerated(webUi, "http://localhost:0"); // no port
            AssertNewUriIsGenerated(webUi, "http://localhost:80"); // default port

        }

        [TestMethod]
        public void ValidateRedirectUri()
        {
            // Arrange
            var tcpInterceptor = Substitute.For<IUriInterceptor>();

            IWebUI webUi = new DefaultOsBrowserWebUi(_platformProxy, _logger, null, tcpInterceptor);

            // Act
            webUi.UpdateRedirectUri(new Uri("http://localhost:12345"));
            webUi.UpdateRedirectUri(new Uri("http://127.0.0.1:54321"));

            AssertInvalidRedirectUri(webUi, "https://localhost"); // https
            AssertInvalidRedirectUri(webUi, "http://www.bing.com"); // not localhost
            AssertInvalidRedirectUri(webUi, "http://www.bing.com:1234"); // not localhost

        }

        [TestMethod]
        public void ValidateResponseMessageFromOptions()
        {
            ValidateResponse(
                options: null,
                successResponse: true,
                expectedMessage: DefaultOsBrowserWebUi.DefaultSuccessHtml,
                expectedRedirect: null);

            ValidateResponse(
               options: new SystemWebViewOptions(),
               successResponse: true,
               expectedMessage: DefaultOsBrowserWebUi.DefaultSuccessHtml,
               expectedRedirect: null);

            ValidateResponse(
                options: new SystemWebViewOptions() { HtmlMessageSuccess = "all good", HtmlMessageError = "not cool" },
                successResponse: true,
                expectedMessage: "all good",
                expectedRedirect: null);

            ValidateResponse(
               options: new SystemWebViewOptions() { HtmlMessageSuccess = "all good", HtmlMessageError = "not cool" },
               successResponse: false,
               expectedMessage: "not cool",
               expectedRedirect: null);

            ValidateResponse(
               options: new SystemWebViewOptions() { HtmlMessageSuccess = "all good", BrowserRedirectSuccess = new Uri("http://bing.com") },
               successResponse: false,
               expectedMessage: string.Format(
                   CultureInfo.InvariantCulture,
                   DefaultOsBrowserWebUi.DefaultFailureHtml,
                   "errorMsg",
                   "errorDesc"),
               expectedRedirect: null);

            // Failure HTML with string template
            ValidateResponse(
                options: new SystemWebViewOptions() { HtmlMessageSuccess = "all good", HtmlMessageError = "Failed with {0} description {1}" },
                successResponse: false,
                expectedMessage: "Failed with errorMsg description errorDesc",
                expectedRedirect: null);

            ValidateResponse(
               options: new SystemWebViewOptions() { HtmlMessageSuccess = "all good", BrowserRedirectSuccess = new Uri("http://bing.com") },
               successResponse: true,
               expectedMessage: null,
               expectedRedirect: "http://bing.com/");

            ValidateResponse(
                options: new SystemWebViewOptions()
                {
                    HtmlMessageSuccess = "all good",
                    BrowserRedirectError = new Uri("http://contoso.com"),
                    BrowserRedirectSuccess = new Uri("http://bing.com")
                },
                successResponse: true,
                expectedMessage: null,
                expectedRedirect: "http://bing.com/");

        }

        private void ValidateResponse(SystemWebViewOptions options, bool successResponse, string expectedMessage, string expectedRedirect)
        {
            // Arrange
            var tcpInterceptor = Substitute.For<IUriInterceptor>();
            var webUi = new DefaultOsBrowserWebUi(_platformProxy, _logger, options, tcpInterceptor);

            Uri successAuthCodeUri = successResponse ?
                new Uri(TestAuthorizationResponseUri) :
                new Uri(TestErrorAuthorizationResponseUri);

            // Act
            MessageAndHttpCode messageAndCode = webUi.GetResponseMessage(successAuthCodeUri);

            // Assert
            if (expectedMessage != null)
            {
                Assert.AreEqual(messageAndCode.HttpCode, HttpStatusCode.OK);
                Assert.IsTrue(messageAndCode.Message.Equals(expectedMessage, StringComparison.OrdinalIgnoreCase));
            }

            if (expectedRedirect != null)
            {
                Assert.AreEqual(messageAndCode.HttpCode, HttpStatusCode.Found);
                Assert.IsTrue(messageAndCode.Message.Equals(expectedRedirect, StringComparison.OrdinalIgnoreCase));
            }
        }

        private static void AssertInvalidRedirectUri(IWebUI webUI, string uri)
        {
            var ex = AssertException.Throws<MsalClientException>(() => webUI.UpdateRedirectUri(new Uri(uri)));
            Assert.AreEqual(MsalError.LoopbackRedirectUri, ex.ErrorCode);
        }

        private static void AssertNewUriIsGenerated(IWebUI webUI, string uri)
        {
            var newUri = webUI.UpdateRedirectUri(new Uri(uri));
            Assert.IsFalse(newUri.IsDefaultPort);
            Assert.IsTrue(newUri.IsLoopback);

            AssertPortIsFree(newUri.Port);
        }

        private static void AssertPortIsFree(int port)
        {
            var listner = new TcpListener(IPAddress.Loopback, port);
            try
            {
                listner.Start();
            }
            catch (Exception e)
            {
                Assert.Fail($"Port {port} does not seem to be free, " + e.Message);
            }
            finally
            {
                listner?.Stop();
            }
        }
    }
}

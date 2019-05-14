// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if NET_CORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.netcore.OsBrowser;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class DefaultOsBrowserWebUiTests
    {
        private const string TestAuthorizationRequestUri = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?scope=offline_access+openid+profile+user.read&response_type=code&client_id=1d18b3b0-251b-4714-a02a-9956cec86c2d&redirect_uri=http%3A%2F%2Flocalhost%3A50997&client-request-id=ea9aa98c-b190-40a3-96a7-1d11978ad131&x-client-SKU=MSAL.NetCore&x-client-Ver=3.0.4.0&x-client-OS=Microsoft+Windows+10.0.18362+&prompt=select_account&code_challenge=Zz9TikND5pVrkTRYWb6UfJwBr_hTacy5-rc9aBD_G8c&code_challenge_method=S256&state=901e7d87-6f49-4f9f-9fa7-e6b8c32d5b9595bc1797-dacc-4ff1-b9e9-0df81be286c7";
        private const string TestRedirectUri = "http://localhost:50997";
        private const string TestAuthorizationResponseUri = "http://localhost:50997/?code=OAQABAAIAAADCoMpjJXrxTq9VG9te-7FX1n0H3n1cOvvLstfUYt_wAqcm96iwQKYzTWLqkz44aUnx4mswa7tn53DFy03fIJie9zOUjk5y6R9vU-rhCSUTLTJR6wUdqsbZfqgRpcCHRPHgmOFk7c3MqJ6WF5Y9AfQgXLaXsZN5vy7ZqS9viU0-NXxKDuBx17yqsT0FPvuoO_0yEZkuVkwd_x_fuUpejHqmORRPfdS-rN6e-7TwfbpsjvUl_eZ2BbzOSJu9rRltWqK-cBVkBhmt3jYEXVWsuTFRD9GHPELscdMJxkwqeOyA8-Lt6zCskKQMq_aAwSPR34CxA9YXoLy-psqjeMDLA5ieP5rmdoNcGBPSXS-imNMKfFSxHN_df6rqpQCOShJ_SmuBFY6qfcARgXpAlobRiUHat-K5heDVJTude47uE_NCSdmRJVZzY1dOeVEJ6f6O1TgR8EHq_MOSyc9HTUU0CpYvf8zePZIjn4jFPv4CZwvdmc4sOCntWrPxxj0JfRval58-aueRgnyhkm9G23FG4oCWWjydaKp5EytHhyYYf_qztsycUkL3Z2Ox7brQ8_Sj1IQr14J3G2FUYgwjuvi6RYK3cvXPM6oUrhOlQcvx03y10xAtizogcA5UR2m8GIpDkm4GEMYX4yYcvBUI6y0qKHmjnZuS5UsymUhUbNG8kEsnI0WTODZ4zYlEHweTsTXq1QNawZqxAW-ZsQ9EbrEbuDFaybJtNYFuHkm1kUjUwpsbZXFLnTUI6CKKDNlUdvPpbiENgapB_p_AgLl3L5KihfY8AkVbVgHZVAcpDClEu_autQZa2jGvPEQka-oKpHqIFZbDEi4qB4yrkU_hDsjf-EqnIAA&state=901e7d87-6f49-4f9f-9fa7-e6b8c32d5b9595bc1797-dacc-4ff1-b9e9-0df81be286c7&session_state=1b37b349-61fe-4ad5-a049-9f8eadfded26";
        private const int TestPort = 50997;

        ITcpInterceptor _tcpInterceptor;
        IPlatformProxy _platformProxy;
        ICoreLogger _logger;


        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            _tcpInterceptor = Substitute.For<ITcpInterceptor>();
            _platformProxy = Substitute.For<IPlatformProxy>();
            _logger = Substitute.For<ICoreLogger>();
        }

        [TestMethod]
        public async Task DefaultOsBrowserWebUi_HappyPath_Async()
        {
            AuthorizationResult authorizationResult = await AcquireAuthCodeAsync()
               .ConfigureAwait(false);

            // Assert
            Assert.AreEqual(AuthorizationStatus.Success, authorizationResult.Status);
            Assert.IsFalse(string.IsNullOrEmpty(authorizationResult.Code));

            await _tcpInterceptor.Received(1).ListenToSingleRequestAndRespondAsync(
                TestPort, Arg.Any<Func<Uri, string>>(), CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]  // TODO: bogavril - expect this case to be removed - MSAL should accept different port
        public async Task DefaultOsBrowserWebUi_ReturnUriInvalid_Async()
        {
            string differentPortRedirectUri = TestAuthorizationResponseUri.Replace(TestRedirectUri, "http://localhost:1111");

            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => AcquireAuthCodeAsync(responseUriString: differentPortRedirectUri))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.LoopbackResponseUriMisatch, ex.ErrorCode);
        }

        private async Task<AuthorizationResult> AcquireAuthCodeAsync(
            string redirectUri = TestRedirectUri,
            string requestUri = TestAuthorizationRequestUri,
            string responseUriString = TestAuthorizationResponseUri)
        {
            // Arrange
            var requestContext = new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid());
            var responseUri = new Uri(responseUriString);
            IWebUI webUI = new DefaultOsBrowserWebUi(_platformProxy, _logger, _tcpInterceptor);

            _tcpInterceptor.ListenToSingleRequestAndRespondAsync(
                TestPort,
                Arg.Any<Func<Uri, string>>(),
                CancellationToken.None)
               .Returns(Task.FromResult(responseUri));

            // Act
            AuthorizationResult authorizationResult = await webUI.AcquireAuthorizationAsync(
                new Uri(requestUri),
                new Uri(redirectUri),
                requestContext,
                CancellationToken.None).ConfigureAwait(false);

            // Assert that we opened the browser
            await _platformProxy.Received(1).StartDefaultOsBrowserAsync(requestUri)
                .ConfigureAwait(false);

            return authorizationResult;

        }

        [TestMethod]
        public void ValidateRedirectUri()
        {
            // Arrange
            var tcpInterceptor = Substitute.For<ITcpInterceptor>();

            IWebUI webUi = new DefaultOsBrowserWebUi(_platformProxy, _logger, tcpInterceptor);

            // Act
            webUi.ValidateRedirectUri(new Uri("http://localhost:12345"));
            webUi.ValidateRedirectUri(new Uri("http://127.0.0.1:54321"));

            AssertInvalidRedirectUri(webUi, "http://localhost"); // no port
            AssertInvalidRedirectUri(webUi, "http://localhost:0"); // no port
            AssertInvalidRedirectUri(webUi, "http://localhost:80"); // default port

            AssertInvalidRedirectUri(webUi, "http://www.bing.com:1234"); // not localhost

        }

        private static void AssertInvalidRedirectUri(IWebUI webUI, string uri)
        {
            var ex = AssertException.Throws<MsalClientException>(() => webUI.ValidateRedirectUri(new Uri(uri)));
            Assert.AreEqual(MsalError.LoopbackRedirectUri, ex.ErrorCode);
        }
    }
}

#endif

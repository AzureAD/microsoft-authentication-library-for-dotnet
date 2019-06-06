// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET_CORE

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class HttpResponseParserTest
    {
        [TestMethod]
        public void CanParseValidHttpGet()
        {
            // Arrange
            const string ValidTcpMessage = @"
                {GET /?code=_some-code_ HTTP/1.1
                 Host: localhost:9001
                 Accept-Language: en-GB,en;q=0.9,en-US;q=0.8,ro;q=0.7,fr;q=0.6
                 Connection: keep-alive
                 Upgrade-Insecure-Requests: 1
                 User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36
                 Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8
                 Accept-Encoding: gzip, deflate, br
                 ... won't parse this far";

            var logger = Substitute.For<ICoreLogger>();

            // Act
            Uri actualUri = HttpResponseParser.ExtractUriFromHttpRequest(ValidTcpMessage, logger);

            // Assert
            logger.DidNotReceiveWithAnyArgs();
            Assert.AreEqual("http://localhost:9001/?code=_some-code_", actualUri.AbsoluteUri);
        }

        [TestMethod]
        public void WillRejectPostAndNoHost()
        {
            const string PostTcpMessage = @"
                {POST /?code=_some-code_ HTTP/1.1
                 Host: localhost:9001
                 Accept-Language: en-GB,en;q=0.9,en-US;q=0.8,ro;q=0.7,fr;q=0.6
                 Connection: keep-alive
                 Upgrade-Insecure-Requests: 1
                 User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36
                 Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8
                 Accept-Encoding: gzip, deflate, br
                 ... won't parse this far";

            AssertBadTcpMeesage(PostTcpMessage);

            const string NoHost = @"
                {Get /?code=_some-code_ HTTP/1.1
                 Accept-Language: en-GB,en;q=0.9,en-US;q=0.8,ro;q=0.7,fr;q=0.6
                 Connection: keep-alive
                 ... won't parse this far";

            AssertBadTcpMeesage(NoHost);

            const string NoHttp = @"
                {Get /?code=_some-code_
                 Host: localhost:9001
                 Accept-Language: en-GB,en;q=0.9,en-US;q=0.8,ro;q=0.7,fr;q=0.6
                 Connection: keep-alive
                 ... won't parse this far";

            AssertBadTcpMeesage(NoHttp);

        }

        private void AssertBadTcpMeesage(string message)
        {
            // Arrange
            var logger = Substitute.For<ICoreLogger>();

            // Act
            var ex = AssertException.Throws<MsalClientException>(
                () => HttpResponseParser.ExtractUriFromHttpRequest(message, logger));

            // Assert
            Assert.AreEqual(MsalError.InvalidAuthorizationUri, ex.ErrorCode);
            logger.Received(1).ErrorPii(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}

#endif

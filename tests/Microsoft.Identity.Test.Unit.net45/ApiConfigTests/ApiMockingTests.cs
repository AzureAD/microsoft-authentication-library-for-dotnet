// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.ApiConfigTests
{
    [TestClass]
    public class ApiMockingTests
    {
        private const string ExpectedAccessToken = "yay access token";

        private IPublicClientApplication _pca;
        private IConfidentialClientApplication _cca;

        [TestInitialize]
        public void TestInitialize()
        {
            _pca = Substitute.For<IPublicClientApplication>();
            _cca = Substitute.For<IConfidentialClientApplication>();
        }

        private AuthenticationResult CreateExpectedAuthenticationResult()
        {
            return new AuthenticationResult(
                ExpectedAccessToken,
                false,
                string.Empty,
                new DateTimeOffset(),
                new DateTimeOffset(),
                string.Empty,
                null,
                string.Empty,
                new List<string>());
        }

        [TestMethod]
        public async Task MockAcquireTokenSilentOnPublicClientAsync()
        {
            // arrange
            _pca.ExecuteAsync(Arg.Any<AcquireTokenSilentParameterBuilder>(), CancellationToken.None)
                .Returns<AuthenticationResult>(CreateExpectedAuthenticationResult());

            // act
            var result = await _pca.ExecuteAsync(_pca.AcquireTokenSilent(new List<string> { "hello" }, string.Empty), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.AreEqual(ExpectedAccessToken, result.AccessToken);
        }

        [TestMethod]
        public async Task MockAcquireTokenByIntegratedWindowsAuthAsync()
        {
            // arrange
            _pca.ExecuteAsync(Arg.Any<AcquireTokenByIntegratedWindowsAuthParameterBuilder>(), CancellationToken.None)
                .Returns<AuthenticationResult>(CreateExpectedAuthenticationResult());

            // act
            var result = await _pca.ExecuteAsync(_pca.AcquireTokenByIntegratedWindowsAuth(new List<string> { "hello" }), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.AreEqual(ExpectedAccessToken, result.AccessToken);
        }

        [TestMethod]
        public async Task MockAcquireTokenByUsernamePasswordAsync()
        {
            // arrange
            _pca.ExecuteAsync(Arg.Any<AcquireTokenByUsernamePasswordParameterBuilder>(), CancellationToken.None)
                .Returns<AuthenticationResult>(CreateExpectedAuthenticationResult());

            // act
            var result = await _pca.ExecuteAsync(_pca.AcquireTokenByUsernamePassword(new List<string> { "hello" }, string.Empty, null), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.AreEqual(ExpectedAccessToken, result.AccessToken);
        }

        [TestMethod]
        public async Task MockAcquireTokenInteractiveAsync()
        {
            // arrange
            _pca.ExecuteAsync(Arg.Any<AcquireTokenInteractiveParameterBuilder>(), CancellationToken.None)
                .Returns<AuthenticationResult>(CreateExpectedAuthenticationResult());

            // act
            var result = await _pca.ExecuteAsync(_pca.AcquireTokenInteractive(new List<string> { "hello" }, null), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.AreEqual(ExpectedAccessToken, result.AccessToken);
        }

        [TestMethod]
        public async Task MockAcquireTokenSilentOnConfidentialClientAsync()
        {
            // arrange
            _cca.ExecuteAsync(Arg.Any<AcquireTokenSilentParameterBuilder>(), CancellationToken.None)
                .Returns<AuthenticationResult>(CreateExpectedAuthenticationResult());

            // act
            var result = await _cca.ExecuteAsync(_cca.AcquireTokenSilent(new List<string> { "hello" }, string.Empty), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.AreEqual(ExpectedAccessToken, result.AccessToken);
        }

        [TestMethod]
        public async Task MockAcquireTokenOnBehalfOfAsync()
        {
            // arrange
            _cca.ExecuteAsync(Arg.Any<AcquireTokenOnBehalfOfParameterBuilder>(), CancellationToken.None)
                .Returns<AuthenticationResult>(CreateExpectedAuthenticationResult());

            // act
            var result = await _cca.ExecuteAsync(_cca.AcquireTokenOnBehalfOf(new List<string> { "hello" }, null), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.AreEqual(ExpectedAccessToken, result.AccessToken);
        }

        [TestMethod]
        public async Task MockAcquireTokenForClientAsync()
        {
            // arrange
            _cca.ExecuteAsync(Arg.Any<AcquireTokenForClientParameterBuilder>(), CancellationToken.None)
                .Returns<AuthenticationResult>(CreateExpectedAuthenticationResult());

            // act
            var result = await _cca.ExecuteAsync(_cca.AcquireTokenForClient(new List<string> { "hello" }), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.AreEqual(ExpectedAccessToken, result.AccessToken);
        }

        [TestMethod]
        public async Task MockAcquireTokenByAuthorizationCodeAsync()
        {
            // arrange
            _cca.ExecuteAsync(Arg.Any<AcquireTokenByAuthorizationCodeParameterBuilder>(), CancellationToken.None)
                .Returns<AuthenticationResult>(CreateExpectedAuthenticationResult());

            // act
            var result = await _cca.ExecuteAsync(_cca.AcquireTokenByAuthorizationCode(new List<string> { "hello" }, string.Empty), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.AreEqual(ExpectedAccessToken, result.AccessToken);
        }

        [TestMethod]
        public async Task MockGetAuthorizationRequestUrlAsync()
        {
            Uri expectedUri = new Uri("http://foo.bar/baz");

            // arrange
            _cca.ExecuteAsync(Arg.Any<GetAuthorizationRequestUrlParameterBuilder>(), CancellationToken.None)
                .Returns<Uri>(expectedUri);

            // act
            var result = await _cca.ExecuteAsync(_cca.GetAuthorizationRequestUrl(new List<string> { "hello" }), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.AreEqual(expectedUri, result);
        }

    }
}


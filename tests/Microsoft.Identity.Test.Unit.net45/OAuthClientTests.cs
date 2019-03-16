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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Unit.RequestsTests.InteractiveRequestTests;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class OAuthClientTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
        }

        [TestMethod]
        public void RedirectUriContainsFragmentErrorTest()
        {
            try
            {
                using (var harness = new MockHttpAndServiceBundle())
                {
                    AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                        MsalTestConstants.AuthorityHomeTenant,
                        MsalTestConstants.Scope,
                        null,
                        extraQueryParameters: new Dictionary<string, string>
                        {
                            {"extra", "qp"}
                        });
                    parameters.RedirectUri = new Uri("some://uri#fragment=not-so-good");
                    parameters.LoginHint = MsalTestConstants.DisplayableId;
                    var interactiveParameters = new AcquireTokenInteractiveParameters
                    {
                        Prompt = Prompt.ForceLogin,
                        ExtraScopesToConsent = MsalTestConstants.ScopeForAnotherResource.ToArray(),
                    };

                    new InteractiveRequest(
                        harness.ServiceBundle,
                        parameters,
                        interactiveParameters,
                        new MockWebUI());

                    Assert.Fail("ArgumentException should be thrown here");
                }
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.Contains(MsalErrorMessage.RedirectUriContainsFragment));
            }
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenItCannotParseJsonResponse()
        {
            ValidateOathClient(
                MockHelpers.CreateTooManyRequestsNonJsonResponse(),
                exception =>
                {
                    MsalServiceException serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual(429, serverEx.StatusCode);
                    Assert.AreEqual(MockHelpers.TooManyRequestsContent, serverEx.ResponseBody);
                    Assert.AreEqual(MockHelpers.TestRetryAfterDuration, serverEx.Headers.RetryAfter.Delta);
                    Assert.AreEqual(MsalError.NonParsableOAuthError, serverEx.ErrorCode);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenItCanParseJsonResponse()
        {
            ValidateOathClient(
                MockHelpers.CreateTooManyRequestsJsonResponse(),
                exception =>
                {
                    MsalServiceException serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual(429, serverEx.StatusCode);
                    Assert.AreEqual(MockHelpers.TestRetryAfterDuration, serverEx.Headers.RetryAfter.Delta);
                    Assert.AreEqual("Server overload", serverEx.ErrorCode);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenEntireResponseIsNull()
        {
            ValidateOathClient(
                null,
                exception =>
                {
                    var innerException = exception.InnerException as InvalidOperationException;
                    Assert.IsNotNull(innerException);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenResponseIsEmpty()
        {
            ValidateOathClient(
                MockHelpers.CreateEmptyResponseMessage(),
                exception =>
                {
                    var serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual((int)HttpStatusCode.BadRequest, serverEx.StatusCode);
                    Assert.IsNotNull(serverEx.ResponseBody);
                    Assert.AreEqual(MsalError.HttpStatusCodeNotOk, serverEx.ErrorCode);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenResponseIsNull()
        {
            ValidateOathClient(
                MockHelpers.CreateNullResponseMessage(),
                exception =>
                {
                    var serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual((int)HttpStatusCode.BadRequest, serverEx.StatusCode);
                    Assert.IsNull(serverEx.ResponseBody);
                    Assert.AreEqual(MsalError.HttpStatusCodeNotOk, serverEx.ErrorCode);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenResponseDoesNotContainAnErrorField()
        {
            ValidateOathClient(
                MockHelpers.CreateNoErrorFieldResponseMessage(),
                exception =>
                {
                    var serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual((int)HttpStatusCode.BadRequest, serverEx.StatusCode);
                    Assert.IsNotNull(serverEx.ResponseBody);
                    Assert.AreEqual(MsalError.HttpStatusCodeNotOk, serverEx.ErrorCode);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenResponseIsHttpNotFound()
        {
            ValidateOathClient(
                MockHelpers.CreateHttpStatusNotFoundResponseMessage(),
                exception =>
                {
                    var serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual((int)HttpStatusCode.NotFound, serverEx.StatusCode);
                    Assert.IsNotNull(serverEx.ResponseBody);
                    Assert.AreEqual(MsalError.HttpStatusNotFound, serverEx.ErrorCode);
                });
        }

        private static void ValidateOathClient(HttpResponseMessage httpResponseMessage, Action<Exception> validationHandler)
        {
            using (MockHttpAndServiceBundle harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = httpResponseMessage
                    });

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityHomeTenant,
                    MsalTestConstants.Scope,
                    null);
                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = MsalTestConstants.DisplayableId;
                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = MsalTestConstants.ScopeForAnotherResource.ToArray(),
                };

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    new MockWebUI());

                try
                {
                    request.ExecuteAsync(CancellationToken.None).Wait();
                    Assert.Fail("MsalException should have been thrown here");
                }
                catch (Exception exc)
                {
                    validationHandler(exc);
                }
            }
        }

    }
}

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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal static class MockHttpManagerExtensions
    {
        public static void AddInstanceDiscoveryMockHandler(this MockHttpManager httpManager)
        {
            AddInstanceDiscoveryMockHandler(httpManager, MsalTestConstants.AuthorityCommonTenant);
        }

        public static void AddInstanceDiscoveryMockHandler(this MockHttpManager httpManager, string url)
        {
            httpManager.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(MsalTestConstants.GetDiscoveryEndpoint(url)));
        }

        public static void AddSuccessTokenResponseMockHandlerForPost(
            this MockHttpManager httpManager,
            string authority,
            IDictionary<string, string> bodyParameters = null,
            IDictionary<string, string> queryParameters = null)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedUrl = authority + "oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ExpectedPostData = bodyParameters,
                    ExpectedQueryParams = queryParameters,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });
        }

        public static void AddSuccessTokenResponseMockHandlerForGet(
            this MockHttpManager httpManager,
            IDictionary<string, string> bodyParameters = null,
            IDictionary<string, string> queryParameters = null)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Get,
                    ExpectedPostData = bodyParameters,
                    ExpectedQueryParams = queryParameters,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });
        }

        public static void AddResiliencyMessageMockHandler(
            this MockHttpManager httpManager,
            HttpMethod httpMethod,
            HttpStatusCode httpStatusCode)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedMethod = httpMethod,
                    ResponseMessage = MockHelpers.CreateResiliencyMessage(httpStatusCode)
                });
        }

        public static void AddRequestTimeoutResponseMessageMockHandler(this MockHttpManager httpManager, HttpMethod httpMethod)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedMethod = httpMethod,
                    ResponseMessage = MockHelpers.CreateRequestTimeoutResponseMessage(),
                    ExceptionToThrow = new TaskCanceledException("request timed out")
                });
        }

        public static void AddMockHandlerForTenantEndpointDiscovery(this MockHttpManager httpManager, string authority, string qp = "")
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    ExpectedUrl = authority + "v2.0/.well-known/openid-configuration",
                    ExpectedMethod = HttpMethod.Get,
                    ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(authority, qp)
                });
        }

        public static void AddMockHandlerContentNotFound(this MockHttpManager httpManager, HttpMethod httpMethod, string url = "")
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedUrl = url,
                    ExpectedMethod = httpMethod,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("Not found")
                    }
                });
        }

        public static void AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(this MockHttpManager httpManager)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
                });
        }

        public static void AddFailingRequest(this MockHttpManager httpManager, Exception exceptionToThrow)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("Foo")
                    },
                    ExceptionToThrow = new InvalidOperationException("Error")
                });
        }
    }
}
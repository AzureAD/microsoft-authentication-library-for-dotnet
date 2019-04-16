// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        public static void AddResponseMockHandlerForPost(
            this MockHttpManager httpManager,
            HttpResponseMessage responseMessage,
            IDictionary<string, string> bodyParameters = null,
            IDictionary<string, string> queryParameters = null)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ExpectedPostData = bodyParameters,
                    ExpectedQueryParams = queryParameters,
                    ResponseMessage = responseMessage
                });
        }

        public static void AddSuccessTokenResponseMockHandlerForPost(
            this MockHttpManager httpManager,
            string authority,
            IDictionary<string, string> bodyParameters = null,
            IDictionary<string, string> queryParameters = null,
            bool foci = false)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedUrl = authority + "oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ExpectedPostData = bodyParameters,
                    ExpectedQueryParams = queryParameters,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(foci)
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

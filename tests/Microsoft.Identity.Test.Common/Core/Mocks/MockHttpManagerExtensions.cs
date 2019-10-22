// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal static class MockHttpManagerExtensions
    {
        public static void AddInstanceDiscoveryMockHandler(this MockHttpManager httpManager)
        {
            AddInstanceDiscoveryMockHandler(httpManager, TestConstants.AuthorityCommonTenant);
        }

        public static void AddInstanceDiscoveryMockHandler(this MockHttpManager httpManager, string authority)
        {
            Uri authorityURI = new Uri(authority);
            string discoveryHost = KnownMetadataProvider.IsKnownEnvironment(authorityURI.Host)
                                       ? authorityURI.Host
                                       : AadAuthority.DefaultTrustedHost;

            string discoveryEndpoint = UriBuilderExtensions.GetHttpsUriWithOptionalPort($"https://{discoveryHost}/common/discovery/instance", authorityURI.Port);

            httpManager.AddMockHandler(
                MockHelpers.CreateInstanceDiscoveryMockHandler(discoveryEndpoint));
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

        public static void AddSuccessfulTokenResponseWithHttpTelemetryMockHandlerForPost(
           this MockHttpManager httpManager,
           string authority,
           IDictionary<string, string> bodyParameters = null,
           IDictionary<string, string> queryParameters = null,
           IDictionary<string, string> httpTelemetryHeaders = null,
           bool foci = false)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedUrl = authority + "oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ExpectedPostData = bodyParameters,
                    ExpectedQueryParams = queryParameters,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(foci),
                    HttpTelemetryHeaders = httpTelemetryHeaders
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

        public static MockHttpMessageHandler AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(this MockHttpManager httpManager)
        {
            var handler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            };

            httpManager.AddMockHandler(handler);

            return handler;
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

        public static void AddAdfs2019MockHandler(this MockHttpManager httpManager)
        {
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ExpectedUrl = "https://fs.contoso.com/.well-known/webfinger",
                    ExpectedQueryParams = new Dictionary<string, string>
                    {
                            {"resource", "https://fs.contoso.com"},
                            {"rel", "http://schemas.microsoft.com/rel/trusted-realm"}
                    },
                    ResponseMessage = MockHelpers.CreateSuccessWebFingerResponseMessage("https://fs.contoso.com")
                });

            //add mock response for tenant endpoint discovery
            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.OnPremiseAuthority)
            });

            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateAdfsSuccessTokenResponseMessage()
            });
        }
    }
}

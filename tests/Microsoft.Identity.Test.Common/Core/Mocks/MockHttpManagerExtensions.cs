﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
        public static void AddInstanceDiscoveryMockHandler(
            this MockHttpManager httpManager, 
            string authority = TestConstants.AuthorityCommonTenant, 
            Uri customDiscoveryEndpoint = null, 
            string instanceMetadataContent = null)
        {
            Uri authorityURI = new Uri(authority);

            string discoveryEndpoint;

            if (customDiscoveryEndpoint == null)
            {
                string discoveryHost = KnownMetadataProvider.IsKnownEnvironment(authorityURI.Host)
                                           ? authorityURI.Host
                                           : AadAuthority.DefaultTrustedHost;

                discoveryEndpoint = UriBuilderExtensions.GetHttpsUriWithOptionalPort($"https://{discoveryHost}/common/discovery/instance", authorityURI.Port);
            }
            else
            {
                discoveryEndpoint = customDiscoveryEndpoint.AbsoluteUri;
            }

            httpManager.AddMockHandler(
                MockHelpers.CreateInstanceDiscoveryMockHandler(
                    discoveryEndpoint, 
                    instanceMetadataContent ?? TestConstants.DiscoveryJsonResponse));
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

        public static MockHttpMessageHandler AddFailureTokenEndpointResponse(
           this MockHttpManager httpManager,
           string error,
           string authority = TestConstants.AuthorityCommonTenant, 
           string correlationId = null)
        {
            var handler = new MockHttpMessageHandler()
            {
                ExpectedUrl = authority + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateFailureTokenResponseMessage(
                    error, 
                    correlationId: correlationId)
            };
            httpManager.AddMockHandler(handler);
            return handler;
        }

        public static MockHttpMessageHandler AddSuccessTokenResponseMockHandlerForPost(
            this MockHttpManager httpManager,
            string authority = TestConstants.AuthorityCommonTenant,
            IDictionary<string, string> bodyParameters = null,
            IDictionary<string, string> queryParameters = null,
            bool foci = false, 
            HttpResponseMessage responseMessage = null)
        {
            var handler = new MockHttpMessageHandler()
            {
                ExpectedUrl = authority + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = bodyParameters,
                ExpectedQueryParams = queryParameters,
                ResponseMessage = responseMessage ?? MockHelpers.CreateSuccessTokenResponseMessage(foci)
            };
            httpManager.AddMockHandler(handler);
            return handler;
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

        public static HttpResponseMessage AddResiliencyMessageMockHandler(
            this MockHttpManager httpManager,
            HttpMethod httpMethod,
            HttpStatusCode httpStatusCode)
        {
            var response = MockHelpers.CreateResiliencyMessage(httpStatusCode);
            httpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedMethod = httpMethod,
                    ResponseMessage = response
                });
            return response;
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

        public static MockHttpMessageHandler AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
            this MockHttpManager httpManager)
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

        public static HttpResponseMessage AddAllMocks(this MockHttpManager httpManager, TokenResponseType aadResponse)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerForTenantEndpointDiscovery(
                TestConstants.AuthorityUtidTenant);

            return AddTokenResponse(httpManager, aadResponse);
        }

        public static HttpResponseMessage AddTokenResponse(
            this MockHttpManager httpManager, 
            TokenResponseType responseType)
        {
            HttpResponseMessage responseMessage;

            switch (responseType)
            {
                case TokenResponseType.Valid:
                    responseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                       TestConstants.UniqueId,
                       TestConstants.DisplayableId,
                       TestConstants.s_scope.ToArray());
                    var refreshHandler = new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage =responseMessage
                    };
                    httpManager.AddMockHandler(refreshHandler);
                    break;
                case TokenResponseType.Invalid_AADUnavailable503:
                    responseMessage = MockHelpers.CreateFailureMessage(
                            System.Net.HttpStatusCode.ServiceUnavailable, "service down");                   
                    var refreshHandler1 = new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = responseMessage
                    };

                    // MSAL retries once for errors in the 500 - 600 range
                    httpManager.AddMockHandler(refreshHandler1);
                    break;
                case TokenResponseType.InvalidGrant:

                    responseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage();
                    var handler = new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = responseMessage
                    };

                    httpManager.AddMockHandler(handler);
                    break;
                case TokenResponseType.InvalidClient:                    

                    responseMessage = MockHelpers.CreateInvalidClientResponseMessage();
                    handler = new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = responseMessage
                    };

                    httpManager.AddMockHandler(handler);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return responseMessage;
        }

        public static HttpResponseMessage AddTokenErrorResponse(
            this MockHttpManager httpManager, 
            string error, 
            HttpStatusCode? customStatusCode)
        {
            var responseMessage = MockHelpers.CreateFailureTokenResponseMessage(error, customStatusCode: customStatusCode);
            var handler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = responseMessage
            };
            httpManager.AddMockHandler(handler);
            return responseMessage;
        }
    }

    public enum TokenResponseType
    {
        Valid,
        Invalid_AADUnavailable503,
        /// <summary>
        /// Results in a UI Required Exception
        /// </summary>
        InvalidGrant, 

        /// <summary>
        /// Normal server exception
        /// </summary>
        InvalidClient
    }
}

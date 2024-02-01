// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal static class MockHttpCreator
    {
        public static HttpResponseMessage GetSuccessfulCredentialResponse()
        {
            string successResponse = "{\"client_id\":\"2d0d13ad-3a4d-4cfd-98f8-f20621d55ded\"," +
                                     "\"credential\":\"accesstoken\"," +
                                     "\"expires_on\":" + (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600) + "," +
                                     "\"identity_type\":\"SystemAssigned\"," +
                                     "\"refresh_in\":" + ((DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600) / 2) + "," +
                                     "\"regional_token_url\":\"https://centraluseuap.mtlsauth.microsoft.com\"," +
                                     "\"tenant_id\":\"72f988bf-86f1-41af-91ab-2d7cd011db47\"}";

            return CreateSuccessResponseMessage(successResponse);
        }

        public static HttpResponseMessage GetMsiSuccessfulResponse(int expiresInHours = 1)
        {
            string expiresOn = Client.Utils.DateTimeHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow.AddHours(expiresInHours));
            string msiSuccessResponse = "{\"access_token\":\"" + TestConstants.ATSecret + "\",\"expires_on\":\"" + expiresOn + "\",\"resource\":\"https://management.azure.com/\",\"token_type\":" +
                                        "\"Bearer\",\"client_id\":\"client_id\"}";

            return CreateSuccessResponseMessage(msiSuccessResponse);
        }

        public static HttpResponseMessage GetSuccessfulMtlsResponse()
        {
            string mtlsResponse = "{\"token_type\":\"Bearer\",\"expires_in\":86399,\"ext_expires_in\":86399,\"access_token\":\"some-token\"}";

            return CreateSuccessResponseMessage(mtlsResponse);
        }


        public static HttpResponseMessage CreateSuccessResponseMessage(string successResponse)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent(successResponse);
            responseMessage.Content = content;
            return responseMessage;
        }

        public static MockHttpMessageHandler CreateManagedIdentityCredentialHandler()
        {
            var handler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = GetSuccessfulCredentialResponse(),
            };

            return handler;
        }

        public static MockHttpMessageHandler CreateManagedIdentityMsiTokenHandler()
        {
            var handler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = GetMsiSuccessfulResponse(),
            };

            return handler;
        }

        public static MockHttpMessageHandler CreateMtlsCredentialHandler(X509Certificate2 mtlsBindingCert)
        {
            var handler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = GetSuccessfulCredentialResponse(),
            };

            // Add the certificate to the handler if provided
            if (mtlsBindingCert != null)
            {
                handler.AddClientCertificate(mtlsBindingCert);
            }

            return handler;
        }

        public static MockHttpMessageHandler CreateMtlsTokenHandler()
        {
            var handler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = GetSuccessfulMtlsResponse(),
            };

            return handler;
        }

        public static MockHttpMessageHandler CreateCredentialTokenHandler()
        {
            var handler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = GetSuccessfulCredentialResponse(),
            };

            return handler;
        }

        public static void AddClientCertificate(this MockHttpMessageHandler handler, X509Certificate2 certificate)
        {   
            handler.ClientCertificates.Add(certificate);
        }
    }
}

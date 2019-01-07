//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal static class MockHelpers
    {
        public const string TooManyRequestsContent = "Too many requests error";
        public static readonly TimeSpan TestRetryAfterDuration = TimeSpan.FromSeconds(120);

        //public static readonly string TokenResponseTemplate =
        //    "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":" +
        //    "\"{0}\",\"access_token\":\"some-access-token\"" +
        //    ",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"client_info\"" +
        //    ":\"{2}\",\"id_token\"" +
        //    ":\"{1}\",\"id_token_expires_in\":\"3600\"}";

        public static readonly string DefaultTokenResponse =
            "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":" +
            "\"r1/scope1 r1/scope2\",\"access_token\":\"some-access-token\"" +
            ",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"client_info\"" +
            ":\"" + CreateClientInfo() + "\",\"id_token\"" +
            ":\"" + CreateIdToken(CoreTestConstants.UniqueId, CoreTestConstants.DisplayableId) +
            "\",\"id_token_expires_in\":\"3600\"}";

        public static string CreateClientInfo()
        {
            return CreateClientInfo(CoreTestConstants.Uid, CoreTestConstants.Utid);
        }

        public static string CreateClientInfo(string uid, string utid)
        {
            return Base64UrlHelpers.Encode("{\"uid\":\"" + uid + "\",\"utid\":\"" + utid + "\"}");
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static HttpResponseMessage CreateResiliencyMessage(HttpStatusCode statusCode)
        {
            HttpResponseMessage responseMessage = null;
            HttpContent content = null;

            responseMessage = new HttpResponseMessage(statusCode);
            content = new StringContent("Server Error 500-599");

            if (responseMessage != null)
            {
                responseMessage.Content = content;
            }
            return responseMessage;
        }

        public static HttpResponseMessage CreateRequestTimeoutResponseMessage()
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            HttpContent content = new StringContent("Request Timed Out.");
            responseMessage.Content = content;
            return responseMessage;
        }

        internal static HttpResponseMessage CreateFailureMessage(HttpStatusCode code, string message)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(code);
            HttpContent content = new StringContent(message);
            responseMessage.Content = content;
            return responseMessage;
        }

        public static HttpResponseMessage CreateSuccessTokenResponseMessage(string scopes, string idToken, string clientInfo)
        {
            return CreateSuccessResponseMessage(string.Format(CultureInfo.InvariantCulture,
                "{{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":" +
                "\"{0}\",\"access_token\":\"some-access-token\"" +
                ",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"client_info\"" +
                ":\"{2}\",\"id_token\"" +
                ":\"{1}\",\"id_token_expires_in\":\"3600\"}}",
                scopes, idToken, clientInfo));
        }

        public static HttpResponseMessage CreateSuccessTokenResponseMessage()
        {
            return CreateSuccessResponseMessage(DefaultTokenResponse);
        }

        public static HttpResponseMessage CreateInvalidGrantTokenResponseMessage()
        {
            return CreateFailureMessage(HttpStatusCode.BadRequest,
                "{\"error\":\"invalid_grant\",\"error_description\":\"AADSTS70002: Error " +
                "validating credentials.AADSTS70008: The provided access grant is expired " +
                "or revoked.Trace ID: f7ec686c-9196-4220-a754-cd9197de44e9Correlation ID: " +
                "04bb0cae-580b-49ac-9a10-b6c3316b1eaaTimestamp: 2015-09-16 07:24:55Z\"," +
                "\"error_codes\":[70002,70008],\"timestamp\":\"2015-09-16 07:24:55Z\"," +
                "\"trace_id\":\"f7ec686c-9196-4220-a754-cd9197de44e9\",\"correlation_id\":" +
                "\"04bb0cae-580b-49ac-9a10-b6c3316b1eaa\"}");
        }

        public static HttpResponseMessage CreateInvalidRequestTokenResponseMessage()
        {
            return CreateFailureMessage(HttpStatusCode.BadRequest,
                "{\"error\":\"invalid_request\",\"error_description\":\"AADSTS90010: " +
                "The grant type is not supported over the /common or /consumers endpoints. " +
                "Please use the /organizations or tenant-specific endpoint." +
                "Trace ID: dd25f4fb-3e8d-458e-90e7-179524ce0000Correlation ID: " +
                "f11508ab-067f-40d4-83cb-ccc67bf57e45Timestamp: 2018-09-22 00:50:11Z\"," +
                "\"error_codes\":[90010],\"timestamp\":\"2018-09-22 00:50:11Z\"," +
                "\"trace_id\":\"dd25f4fb-3e8d-458e-90e7-179524ce0000\",\"correlation_id\":" +
                "\"f11508ab-067f-40d4-83cb-ccc67bf57e45\"}");
        }

        public static HttpResponseMessage CreateSuccessfulClientCredentialTokenResponseMessage()
        {
            return CreateSuccessfulClientCredentialTokenResponseMessage("header.payload.signature");
        }

        public static HttpResponseMessage CreateSuccessfulClientCredentialTokenResponseMessage(string token)
        {
            return CreateSuccessResponseMessage(
                "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"" + token + "\"}");
        }

        public static HttpResponseMessage CreateSuccessTokenResponseMessage(string uniqueId, string displayableId, string[] scope)
        {
            string idToken = CreateIdToken(uniqueId, displayableId, CoreTestConstants.Utid);
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":\"" +
                                  scope.AsSingleString() +
                                  "\",\"access_token\":\"some-access-token\",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"id_token\":\"" +
                                  idToken +
                                  "\",\"id_token_expires_in\":\"3600\",\"client_info\":\"" + CreateClientInfo() + "\"}");
            responseMessage.Content = content;
            return responseMessage;
        }

        public static string CreateIdToken(string uniqueId, string displayableId)
        {
            return CreateIdToken(uniqueId, displayableId, CoreTestConstants.Utid);
        }

        public static string CreateIdToken(string uniqueId, string displayableId, string tenantId)
        {
            string id = "{\"aud\": \"e854a4a7-6c34-449c-b237-fc7a28093d84\"," +
                        "\"iss\": \"https://login.microsoftonline.com/6c3d51dd-f0e5-4959-b4ea-a80c4e36fe5e/v2.0/\"," +
                        "\"iat\": 1455833828," +
                        "\"nbf\": 1455833828," +
                        "\"exp\": 1455837728," +
                        "\"ipaddr\": \"131.107.159.117\"," +
                        "\"name\": \"Marrrrrio Bossy\"," +
                        "\"oid\": \"" + uniqueId + "\"," +
                        "\"preferred_username\": \"" + displayableId + "\"," +
                        "\"sub\": \"K4_SGGxKqW1SxUAmhg6C1F6VPiFzcx-Qd80ehIEdFus\"," +
                        "\"tid\": \"" + tenantId + "\"," +
                        "\"ver\": \"2.0\"}";
            return string.Format(CultureInfo.InvariantCulture, "someheader.{0}.somesignature", Base64UrlHelpers.Encode(id));
        }

        public static HttpResponseMessage CreateSuccessWebFingerResponseMessage(string href)
        {
            return
                CreateSuccessResponseMessage(
                    "{\"subject\": \"https://fabrikam.com\",\"links\": [{\"rel\": " +
                    "\"http://schemas.microsoft.com/rel/trusted-realm\"," +
                    "\"href\": \"" + href + "\"}]}");
        }

        public static HttpResponseMessage CreateSuccessWebFingerResponseMessage()
        {
            return
                CreateSuccessWebFingerResponseMessage("https://fs.contoso.com");
        }

        public static HttpResponseMessage CreateSuccessResponseMessage(string sucessResponse)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent(sucessResponse);
            responseMessage.Content = content;
            return responseMessage;
        }

        public static HttpResponseMessage CreateTooManyRequestsNonJsonResponse()
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent(TooManyRequestsContent)
            };
            httpResponse.Headers.RetryAfter = new RetryConditionHeaderValue(TestRetryAfterDuration);

            return httpResponse;
        }

        public static HttpResponseMessage CreateTooManyRequestsJsonResponse()
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent("{\"error\":\"Server overload\",\"error_description\":\"429: " +
                TooManyRequestsContent + "\", " +
                "\"error_codes\":[90010],\"timestamp\":\"2018-09-22 00:50:11Z\"," +
                "\"trace_id\":\"dd25f4fb-3e8d-458e-90e7-179524ce0000\",\"correlation_id\":" +
                "\"f11508ab-067f-40d4-83cb-ccc67bf57e45\"}")
            };
            httpResponse.Headers.RetryAfter = new RetryConditionHeaderValue(TestRetryAfterDuration);

            return httpResponse;
        }

        public static HttpResponseMessage CreateOpenIdConfigurationResponse(string authority, string qp = "")
        {
            var authorityUri = new Uri(authority);
            string path = authorityUri.AbsolutePath.Substring(1);
            string tenant = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));
            if (tenant.ToLowerInvariant().Equals("common", StringComparison.OrdinalIgnoreCase))
            {
                tenant = "{tenant}";
            }

            if (!string.IsNullOrEmpty(qp))
            {
                qp = "?" + qp;
            }

            return CreateSuccessResponseMessage(string.Format(CultureInfo.InvariantCulture,
                "{{\"authorization_endpoint\":\"{0}oauth2/v2.0/authorize{2}\",\"token_endpoint\":\"{0}oauth2/v2.0/token{2}\",\"issuer\":\"https://sts.windows.net/{1}\"}}",
                authority, tenant, qp));
        }

        public static HttpMessageHandler CreateInstanceDiscoveryMockHandler(string url)
        {
            return CreateInstanceDiscoveryMockHandler(url,
                @"{
                        ""tenant_discovery_endpoint"":""https://login.microsoftonline.com/tenant/.well-known/openid-configuration"",
                        ""api-version"":""1.1"",
                        ""metadata"":[
                            {
                            ""preferred_network"":""login.microsoftonline.com"",
                            ""preferred_cache"":""login.windows.net"",
                            ""aliases"":[
                                ""login.microsoftonline.com"",
                                ""login.windows.net"",
                                ""login.microsoft.com"",
                                ""sts.windows.net""]},
                            {
                            ""preferred_network"":""login.partner.microsoftonline.cn"",
                            ""preferred_cache"":""login.partner.microsoftonline.cn"",
                            ""aliases"":[
                                ""login.partner.microsoftonline.cn"",
                                ""login.chinacloudapi.cn""]},
                            {
                            ""preferred_network"":""login.microsoftonline.de"",
                            ""preferred_cache"":""login.microsoftonline.de"",
                            ""aliases"":[
                                    ""login.microsoftonline.de""]},
                            {
                            ""preferred_network"":""login.microsoftonline.us"",
                            ""preferred_cache"":""login.microsoftonline.us"",
                            ""aliases"":[
                                ""login.microsoftonline.us"",
                                ""login.usgovcloudapi.net""]},
                            {
                            ""preferred_network"":""login-us.microsoftonline.com"",
                            ""preferred_cache"":""login-us.microsoftonline.com"",
                            ""aliases"":[
                                ""login-us.microsoftonline.com""]}
                        ]
                }");
        }

        public static HttpMessageHandler CreateInstanceDiscoveryMockHandler(string url, string content)
        {
            return new MockHttpMessageHandler()
            {
                Url = url,
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content)
                }
            };
        }
    }
}

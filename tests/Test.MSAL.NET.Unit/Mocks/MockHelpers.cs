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
using Microsoft.Identity.Client.Internal;

namespace Test.MSAL.NET.Unit.Mocks
{
    internal static class MockHelpers
    {
        public const string DefaultIdToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6I" +
                                             "k1uQ19WWmNBVGZNNXBPWWlKSE1iYTlnb0VLWSIsImtpZCI" +
                                             "6Ik1uQ19WWmNBVGZNNXBPWWlKSE1iYTlnb0VLWSJ9." +
                                             "eyJhdWQiOiJlODU0YTRhNy02YzM0LTQ0OWMtYjIzNy1mYzd" +
                                             "hMjgwOTNkODQiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc" +
                                             "29mdG9ubGluZS5jb20vNmMzZDUxZGQtZjBlNS00OTU5LWI0ZW" +
                                             "EtYTgwYzRlMzZmZTVlL3YyLjAvIiwiaWF0IjoxNDU1ODMzODI" +
                                             "4LCJuYmYiOjE0NTU4MzM4MjgsImV4cCI6MTQ1NTgzNzcyOCwia" +
                                             "XBhZGRyIjoiMTMxLjEwNy4xNTkuMTE3IiwibmFtZSI6Ik1hcml" +
                                             "vIFJvc3NpIiwiaG9tZV9vaWQiOiJob21lX29pZCIsIm9pZCI6In" +
                                             "VuaXF1ZV9pZCIsInByZWZlcnJlZF91c2VybmFtZSI6ImRpc3Bs" +
                                             "YXlhYmxlQGlkLmNvbSIsInN1YiI6Iks0X1NHR3hLcVcxU3hVQW1" +
                                             "oZzZDMUY2VlBpRnpjeC1RZDgwZWhJRWRGdXMiLCJ0aWQiOiI2Y" +
                                             "zNkNTFkZC1mMGU1LTQ5NTktYjRlYS1hODBjNGUzNmZlNWUiLCJ2" +
                                             "ZXIiOiIyLjAifQ." +
                                             "AD4-sdfsfsdf";

        public static readonly string DefaultClientInfo = Base64UrlEncoder.Encode("{\"uid\":\"" + TestConstants.Uid + "\",\"utid\":\"" + TestConstants.Utid + "\"}");

        public static readonly string DefaultAccessTokenResponse =
            "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":" +
            "\"some-scope1 some-scope2\",\"access_token\":\"some-access-token\"" +
            ",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"client_info\"" +
            ":\"" + DefaultClientInfo + "\",\"id_token\"" +
            ":\""+DefaultIdToken+"\",\"id_token_expires_in\":\"3600\"}";
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

        public static HttpResponseMessage CreateSuccessTokenResponseMessage()
        {
            return CreateSuccessResponseMessage(DefaultAccessTokenResponse);
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

        public static HttpResponseMessage CreateSuccessfulClientCredentialTokenResponseMessage()
        {
            return CreateSuccessResponseMessage(
                "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"header.payload.signature\"}");
        }

        public static HttpResponseMessage CreateSuccessTokenResponseMessage(string uniqueId, string displayableId, string[] scope)
        {
            string idToken = CreateIdToken(uniqueId, displayableId);
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":\"" +
                                  scope.AsSingleString() +
                                  "\",\"access_token\":\"some-access-token\",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"id_token\":\"" +
                                  idToken +
                                  "\",\"id_token_expires_in\":\"3600\",\"client_info\":\"eyJ2ZXIiOiIxLjAiLCJuYW1lIjoiTWFyaW8gUm9zc2kiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJtYXJpb0BkZXZlbG9wZXJ0ZW5hbnQub25taWNyb3NvZnQuY29tIiwic3ViIjoiSzRfU0dHeEtxVzFTeFVBbWhnNkMxRjZWUGlGemN4LVFkODBlaElFZEZ1cyIsInRpZCI6IjZjM2Q1MWRkLWYwZTUtNDk1OS1iNGVhLWE4MGM0ZTM2ZmU1ZSJ9\"}");
            responseMessage.Content = content;
            return responseMessage;
        }

        public static string CreateIdToken(string uniqueId, string displayableId)
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
                        "\"tid\": \""+ TestConstants.IdentityProvider + "\"," +
                        "\"ver\": \"2.0\"}";
            return string.Format(CultureInfo.InvariantCulture, "someheader.{0}.somesignature", Base64UrlEncoder.Encode(id));
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

        public static HttpResponseMessage CreateOpenIdConfigurationResponse(string authority)
        {
            var authorityUri = new Uri(authority);
            string path = authorityUri.AbsolutePath.Substring(1);
            string tenant = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));
            if (tenant.ToLower(CultureInfo.InvariantCulture).Equals("common"))
            {
                tenant = "{tenant}";
            }

            return CreateSuccessResponseMessage(string.Format(CultureInfo.InvariantCulture,
                "{{\"authorization_endpoint\":\"{0}oauth2/v2.0/authorize\",\"token_endpoint\":\"{0}oauth2/v2.0/token\",\"issuer\":\"https://sts.windows.net/{1}\"}}",
                authority, tenant));
        }
    }
}

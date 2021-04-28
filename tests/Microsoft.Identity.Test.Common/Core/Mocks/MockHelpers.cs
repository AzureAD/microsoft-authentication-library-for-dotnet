// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal static class MockHelpers
    {
        public const string TooManyRequestsContent = "Too many requests error";
        public static readonly TimeSpan TestRetryAfterDuration = TimeSpan.FromSeconds(120);

      

        public static readonly string B2CTokenResponseWithoutAT =
            "{\"id_token\":\""+ CreateIdTokenForB2C(TestConstants.Uid, TestConstants.Utid, TestConstants.B2CSignUpSignIn) +"  \"," +
            "\"token_type\":\"Bearer\",\"not_before\":1585658742," +
            "\"client_info\":\"" + CreateClientInfo(TestConstants.Uid + "-" + TestConstants.B2CSignUpSignIn, TestConstants.Utid) + "\"," +
            "\"scope\":\"\"," +
            "\"refresh_token\":\"eyJraWQiOiJjcGltY29yZV8wOTI1MjAxNSIsInZlciI6IjEuMCIsInppcCI6IkRlZmxhdGUiLCJzZXIiOiIxLjAifQ..58S7QKY4AVcJS620.mMAGPkA5-v2QL4-kfB7sThyLQec7ZLyd2b-3-GBly5fLNVkbO9GVo9ZzqbaXbuzkNpj4iSITIRjfK4mBEcNU7s7EieHBbsRP8oee3feUuOzzAc61ZQBmTAkYsjEVa4iTSCxM-eU5n1fyZ1lIK6s33lOzylEs5pVT75HMvr_iLEd_2_QN0Y3ql2NVx1kPJsqk4TR0vfG2vum60sr5IBd2TcIamSAfByzfS6LUfVTicbVuWW7GHbJaQtFiE2tOhoJD_bePKGwWX-UwakMe3A4CfKbpT20OIs_o1UPcQUCGmn7XUjBrEPiaPcRHjVCes7ptGR4uTE7emHl9zHq4btl8poHg7iWG4gEmmp0FFvi6XhFOZosotSTTn72SdEkf-o93SmMrlxMRMMFdzEjqbyaiZSwirYfhbNMPcy_jeQ3BL0cr5UreIhxLkSj_xc9A3vDHVK8a3d6IcBa_x1Wwrt_mzEynI1ldgmQwxyda_Xti1JS3OdBQ0ZIkSiw6Z6l8Vmw-kGgkmWOfYjaFWI-vsV5TGYRUA7UnnbzXfbR1x1KwmVs28ssvl_6lsjqWrbBWMUduPGWA1THZzXEnf-MqA1cJfQRq.vRqgMxW_pIJoPUzNOxKUpQ\"," +
            "\"refresh_token_expires_in\":1209600}";

        public static readonly string DefaultAdfsTokenResponse =
            "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":" +
            "\"r1/scope1 r1/scope2\",\"access_token\":\"some-access-token\"" +
            ",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"id_token\"" +
            ":\"" + CreateAdfsIdToken(TestConstants.OnPremiseDisplayableId) +
            "\",\"id_token_expires_in\":\"3600\"}";

        public static string GetFociTokenResponse()
        {
            return 
            "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":" +
            "\"r1/scope1 r1/scope2\",\"access_token\":\"some-access-token\"" +
            ",\"foci\":\"1\"" +
            ",\"refresh_token\":\"" + Guid.NewGuid() + "\",\"client_info\"" +
            ":\"" + CreateClientInfo() + "\",\"id_token\"" +
            ":\"" + CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId) +
            "\",\"id_token_expires_in\":\"3600\"}";
        }

        public static readonly string DefaultEmtpyFailureErrorMessage =
            "{\"the-error-is-not-here\":\"erorwithouterrorfield\",\"error_description\":\"AADSTS991: " +
                                        "This is an error message which doesn't contain the error field. " +
                                        "Trace ID: dd25f4fb-3e8d-458e-90e7-179524ce0000Correlation ID: " +
                                        "f11508ab-067f-40d4-83cb-ccc67bf57e45Timestamp: 2018-09-22 00:50:11Z\"," +
                                        "\"error_codes\":[90010],\"timestamp\":\"2018-09-22 00:50:11Z\"," +
                                        "\"trace_id\":\"dd25f4fb-3e8d-458e-90e7-179524ce0000\",\"correlation_id\":" +
                                        "\"f11508ab-067f-40d4-83cb-ccc67bf57e45\"}";

        public static string GetDefaultTokenResponse()
        {
              return 
            "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":" +
            "\"r1/scope1 r1/scope2\",\"access_token\":\"" + TestConstants.ATSecret + "\"" +
            ",\"refresh_token\":\"" + Guid.NewGuid() +"\",\"client_info\"" +
            ":\"" + CreateClientInfo() + "\",\"id_token\"" +
            ":\"" + CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId) +
            "\",\"id_token_expires_in\":\"3600\"}";
        }

        public static string GetPopTokenResponse()
        {
            return
          "{\"token_type\":\"pop\",\"expires_in\":\"3599\",\"scope\":" +
          "\"r1/scope1 r1/scope2\",\"access_token\":\"" + TestConstants.ATSecret + "\"" +
          ",\"refresh_token\":\"" + Guid.NewGuid() + "\",\"client_info\"" +
          ":\"" + CreateClientInfo() + "\",\"id_token\"" +
          ":\"" + CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId) +
          "\",\"id_token_expires_in\":\"3600\"}";
        }

        public static string CreateClientInfo(string uid = TestConstants.Uid, string utid = TestConstants.Utid)
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
            HttpResponseMessage responseMessage = new HttpResponseMessage(statusCode);
            HttpContent content = new StringContent("Server Error 500-599");
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

        internal static HttpResponseMessage CreateNullMessage(HttpStatusCode code)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(code);
            responseMessage.Content = null;
            return responseMessage;
        }

        public static HttpResponseMessage CreateSuccessTokenResponseMessage(
            string scopes,
            string idToken,
            string clientInfo)
        {
            return CreateSuccessResponseMessage(string.Format(CultureInfo.InvariantCulture,
                "{{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":" +
                "\"{0}\",\"access_token\":\"some-access-token\"" +
                ",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"client_info\"" +
                ":\"{2}\",\"id_token\"" +
                ":\"{1}\",\"id_token_expires_in\":\"3600\"}}",
                scopes, idToken, clientInfo));
        }

        public static HttpResponseMessage CreateSuccessTokenResponseMessage(bool foci = false)
        {
            return CreateSuccessResponseMessage(
                foci ? GetFociTokenResponse() : GetDefaultTokenResponse());
        }        

        public static HttpResponseMessage CreateSuccessTokenResponseMessageWithUid(
            string uid, string utid, string displayableName)
        {
            string tokenResponse =
                "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":" +
                "\"r1/scope1 r1/scope2\",\"access_token\":\"some-access-token\"" +
                ",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"client_info\"" +
                ":\"" + CreateClientInfo(uid, utid) + "\",\"id_token\"" +
                ":\"" + CreateIdToken(uid, displayableName) +
                "\",\"id_token_expires_in\":\"3600\"}";

            return CreateSuccessResponseMessage(tokenResponse);
        }


        public static HttpResponseMessage CreateAdfsSuccessTokenResponseMessage()
        {
            return CreateSuccessResponseMessage(DefaultAdfsTokenResponse);
        }

        public static HttpResponseMessage CreateFailureTokenResponseMessage(
            string error,
            string subError = null,
            string correlationId = null,
            HttpStatusCode? customStatusCode = null)
        {
            string message = "{\"error\":\"" + error + "\",\"error_description\":\"AADSTS00000: Error for test." +
                "Trace ID: f7ec686c-9196-4220-a754-cd9197de44e9Correlation ID: " +
                "04bb0cae-580b-49ac-9a10-b6c3316b1eaaTimestamp: 2015-09-16 07:24:55Z\"," +
                "\"error_codes\":[70002,70008],\"timestamp\":\"2015-09-16 07:24:55Z\"," +
                "\"trace_id\":\"f7ec686c-9196-4220-a754-cd9197de44e9\"," +
                (subError != null ? ("\"suberror\":" + "\"" + subError + "\",") : "") +
                "\"correlation_id\":" +
                "\"" + (correlationId ?? "f11508ab-067f-40d4-83cb-ccc67bf57e45") + "\"}";

            var statusCode = customStatusCode.HasValue ? customStatusCode.Value : HttpStatusCode.BadRequest;
            return CreateFailureMessage(statusCode, message);
        }

        public static HttpResponseMessage CreateInvalidGrantTokenResponseMessage(string subError = null)
        {
            return CreateFailureMessage(HttpStatusCode.BadRequest,
                "{\"error\":\"invalid_grant\",\"error_description\":\"AADSTS70002: Error " +
                "validating credentials.AADSTS70008: The provided access grant is expired " +
                "or revoked.Trace ID: f7ec686c-9196-4220-a754-cd9197de44e9Correlation ID: " +
                "04bb0cae-580b-49ac-9a10-b6c3316b1eaaTimestamp: 2015-09-16 07:24:55Z\"," +
                "\"error_codes\":[70002,70008],\"timestamp\":\"2015-09-16 07:24:55Z\"," +
                "\"trace_id\":\"f7ec686c-9196-4220-a754-cd9197de44e9\"," +
                (subError != null ? ("\"suberror\":" + "\"" + subError + "\",") : "") +
                "\"correlation_id\":" +
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

        public static HttpResponseMessage CreateInvalidClientResponseMessage()
        {
            return CreateFailureMessage(HttpStatusCode.BadRequest,
                "{\"error\":\"invalid_client\",\"error_description\":\"AADSTS7000218: " +
                "The request body must contain the following parameter: " +
                "'client_assertion' or 'client_secret'." +
                "Trace ID: 21c3e4db - d2fd - 44f7 - a3e0 - 5939f84e6000" +
                "Correlation ID: 3d483b09 - 1198 - 4acb - 929f - c648674e32bd" +
                "Timestamp: 2019 - 07 - 12 19:24:42Z\"," +
                "\"error_codes\":[7000218],\"timestamp\":\"2019-07-12 19:24:42Z\"," +
                "\"trace_id\":\"21c3e4db-d2fd-44f7-a3e0-5939f84e6000\",\"correlation_id\":" +
                "\"3d483b09-1198-4acb-929f-c648674e32bd\"}");
        }



        public static HttpResponseMessage CreateNoErrorFieldResponseMessage()
        {
            return CreateFailureMessage(HttpStatusCode.BadRequest, DefaultEmtpyFailureErrorMessage);
        }

        public static HttpResponseMessage CreateHttpStatusNotFoundResponseMessage()
        {
            return CreateFailureMessage(HttpStatusCode.NotFound,
                                        "{\"the-error-is-not-here\":\"erorwithouterrorfield\",\"error_description\":\"AADSTS991: " +
                                        "This is an error message which doesn't contain the error field. " +
                                        "Trace ID: dd25f4fb-3e8d-458e-90e7-179524ce0000Correlation ID: " +
                                        "f11508ab-067f-40d4-83cb-ccc67bf57e45Timestamp: 2018-09-22 00:50:11Z\"," +
                                        "\"error_codes\":[90010],\"timestamp\":\"2018-09-22 00:50:11Z\"," +
                                        "\"trace_id\":\"dd25f4fb-3e8d-458e-90e7-179524ce0000\",\"correlation_id\":" +
                                        "\"f11508ab-067f-40d4-83cb-ccc67bf57e45\"}");
        }

        public static HttpResponseMessage CreateNullResponseMessage()
        {
            return CreateNullMessage(HttpStatusCode.BadRequest);
        }

        public static HttpResponseMessage CreateEmptyResponseMessage()
        {
            return CreateFailureMessage(HttpStatusCode.BadRequest, string.Empty);
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

        public static HttpResponseMessage CreateSuccessTokenResponseMessage(string uniqueId, string displayableId, string[] scope, bool foci = false)
        {
            string idToken = CreateIdToken(uniqueId, displayableId, TestConstants.Utid);
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            string stringContent = "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"refresh_in\":\"2400\",\"scope\":\"" +
                                  scope.AsSingleString() +
                                  "\",\"access_token\":\"some-access-token\",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"id_token\":\"" +
                                  idToken +
                                  (foci ? "\",\"foci\":\"1" : "") +
                                  "\",\"id_token_expires_in\":\"3600\",\"client_info\":\"" + CreateClientInfo() + "\"}";
            HttpContent content = new StringContent(stringContent);
            responseMessage.Content = content;
            return responseMessage;
        }

        public static string CreateIdToken(string uniqueId, string displayableId)
        {
            return CreateIdToken(uniqueId, displayableId, TestConstants.Utid);
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

        private static string CreateIdTokenForB2C(string uniqueId, string tenantId, string policy)
        {
            string id = "{" + 
                        "  \"exp\": 1585662342," + 
                        "  \"nbf\": 1585658742," + 
                        "  \"ver\": \"1.0\"," + 
                        $"  \"iss\": \"https://fabrikamb2c.b2clogin.com/{tenantId}/v2.0/\"," + 
                        "  \"sub\": \"52f6cad9-b822-4492-b742-e60cd2d55ee2\"," + 
                        "  \"aud\": \"841e1190-d73a-450c-9d68-f5cf16b78e81\"," + 
                        $"  \"acr\": \"{policy}\"," + 
                        "  \"iat\": 1585658742," + 
                        "  \"auth_time\": 1585658742," + 
                        "  \"idp\": \"live.com\"," + 
                        "  \"name\": \"John Bob\"," + 
                        "  \"oid\": \""+ uniqueId + "\"," + 
                        "  \"emails\": [" + 
                        "    \"john.bob@outlook.com\"" + 
                        "  ]}";

            return string.Format(CultureInfo.InvariantCulture, "someheader.{0}.somesignature", Base64UrlHelpers.Encode(id));

        }

        public static string CreateAdfsIdToken(string upn)
        {
            string id = "{\"aud\": \"e854a4a7-6c34-449c-b237-fc7a28093d84\"," +
                        "\"iss\": \"" + TestConstants.OnPremiseAuthority + "\"," +
                        "\"iat\": 1455833828," +
                        "\"nbf\": 1455833828," +
                        "\"exp\": 1455837728," +
                        "\"ipaddr\": \"131.107.159.117\"," +
                        "\"name\": \"Marrrrrio Bossy\"," +
                        "\"upn\": \"" + upn + "\"," +
                        "\"sub\": \"" + TestConstants.OnPremiseUniqueId + "\"}";

            return string.Format(CultureInfo.InvariantCulture, "someheader.{0}.somesignature", Base64UrlHelpers.Encode(id));
        }

        public static HttpResponseMessage CreateSuccessWebFingerResponseMessage(string href)
        {
            return
                CreateSuccessResponseMessage(
                    "{\"subject\": \"https://fs.contoso.com\",\"links\": [{\"rel\": " +
                    "\"http://schemas.microsoft.com/rel/trusted-realm\"," +
                    "\"href\": \"" + href + "\"}]}");
        }

        public static HttpResponseMessage CreateSuccessWebFingerResponseMessage()
        {
            return
                CreateSuccessWebFingerResponseMessage("https://fs.contoso.com");
        }

        public static HttpResponseMessage CreateSuccessResponseMessage(string successResponse)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent(successResponse);
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


        public static HttpResponseMessage CreatePKeyAuthChallengeResponse()
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(DefaultEmtpyFailureErrorMessage)
            };
            httpResponse.Headers.Add("WWW-Authenticate", @"PKeyAuth  Nonce=""nonce"",  Version=""1.0"", CertThumbprint=""thumbprint"",  Context=""context""");

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

        public static HttpResponseMessage CreateAdfsOpenIdConfigurationResponse(string authority, string qp = "")
        {
            var authorityUri = new Uri(authority);
            string path = authorityUri.AbsolutePath.Substring(1);

            if (!string.IsNullOrEmpty(qp))
            {
                qp = "?" + qp;
            }

            return CreateSuccessResponseMessage(string.Format(CultureInfo.InvariantCulture,
                "{{\"authorization_endpoint\":\"{0}oauth2/authorize\",\"token_endpoint\":\"{0}oauth2/token\",\"issuer\":\"{0}\"}}",
                authority, qp));
        }

        public static MockHttpMessageHandler CreateInstanceDiscoveryMockHandler(
            string discoveryEndpoint,
            string content = TestConstants.DiscoveryJsonResponse)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedUrl = discoveryEndpoint,
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content)
                }
            };
        }


    }
}

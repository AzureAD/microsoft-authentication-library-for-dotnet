using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using NSubstitute;

namespace Test.ADAL.NET.Unit.Mocks
{
    internal static class MockHelpers
    {
        public static void ConfigureMockWebUI(AuthorizationResult authorizationResult)
        {
            ConfigureMockWebUI(authorizationResult, new Dictionary<string, string>());
        }

        public static void ConfigureMockWebUI(AuthorizationResult authorizationResult, Dictionary<string, string> headersToValidate)
        {
            MockWebUI webUi = new MockWebUI();
            webUi.HeadersToValidate = headersToValidate;
            webUi.MockResult = authorizationResult;

            IWebUIFactory mockFactory = Substitute.For<IWebUIFactory>();
            mockFactory.CreateAuthenticationDialog(Arg.Any<IPlatformParameters>()).Returns(webUi);
            PlatformPlugin.WebUIFactory = mockFactory;
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
        
        public static HttpResponseMessage CreateSuccessTokenResponseMessage()
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"resource\":\"resource1\",\"access_token\":\"some-access-token\",\"refresh_token\":\"something-encrypted\",\"id_token\":\"" +
                                  CreateIdToken(TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId) +
                                  "\"}");
            responseMessage.Content = content;
            return responseMessage;
        }

        public static HttpResponseMessage CreateInvalidRequestTokenResponseMessage()
        {
            return
                CreateFailureResponseMessage(
                    "{\"error\":\"invalid_request\",\"error_description\":\"AADSTS70002: Some error message. Trace ID: f7ec686c-9196-4220-a754-cd9197de44e9 Correlation ID: 04bb0cae-580b-49ac-9a10-b6c3316b1eaa Timestamp: 2015-09-16 07:24:55Z\",\"error_codes\":[70002,70008],\"timestamp\":\"2015-09-16 07:24:55Z\",\"trace_id\":\"f7ec686c-9196-4220-a754-cd9197de44e9\",\"correlation_id\":\"04bb0cae-580b-49ac-9a10-b6c3316b1eaa\"}");
        }

        public static HttpResponseMessage CreateInvalidGrantTokenResponseMessage()
        {
            return
                CreateFailureResponseMessage(
                    "{\"error\":\"invalid_grant\",\"error_description\":\"AADSTS70002: Error validating credentials.AADSTS70008: The provided access grant is expired or revoked.Trace ID: f7ec686c-9196-4220-a754-cd9197de44e9Correlation ID: 04bb0cae-580b-49ac-9a10-b6c3316b1eaaTimestamp: 2015-09-16 07:24:55Z\",\"error_codes\":[70002,70008],\"timestamp\":\"2015-09-16 07:24:55Z\",\"trace_id\":\"f7ec686c-9196-4220-a754-cd9197de44e9\",\"correlation_id\":\"04bb0cae-580b-49ac-9a10-b6c3316b1eaa\"}");
        }

        public static HttpResponseMessage CreateFailureResponseMessage(string message)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            HttpContent content = new StringContent(message);
            responseMessage.Content = content;
            return responseMessage;
        }

        public static HttpResponseMessage CreateSuccessfulClientCredentialTokenResponseMessage()
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent(
                    "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"header.payload.signature\"}");
            responseMessage.Content = content;
            return responseMessage;
        }

        public static HttpResponseMessage CreateSuccessTokenResponseMessage(string uniqueId, string displayableId, string resource)
        {
            string idToken = string.Format(CultureInfo.InvariantCulture, "someheader.{0}.somesignature", CreateIdToken(uniqueId, displayableId));
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"resource\":\"" +
                                  resource +
                                  "\",\"access_token\":\"some-access-token\",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"id_token\":\"" +
                                  idToken +
                                  "\"}");
            responseMessage.Content = content;
            return responseMessage;
        }

        private static string CreateIdToken(string uniqueId, string displayableId)
        {
            string header = "{alg: \"none\","+
                             "typ:\"JWT\""+
                             "}";
            string payload = "{\"aud\": \"e854a4a7-6c34-449c-b237-fc7a28093d84\"," +
                        "\"iss\": \"https://login.microsoftonline.com/6c3d51dd-f0e5-4959-b4ea-a80c4e36fe5e/\"," +
                        "\"iat\": 1455833828," +
                        "\"nbf\": 1455833828," +
                        "\"exp\": 1455837728," +
                        "\"ipaddr\": \"131.107.159.117\"," +
                        "\"name\": \"Mario Rossi\"," +
                        "\"oid\": \"" + uniqueId + "\"," +
                        "\"upn\": \"" + displayableId + "\"," +
                        "\"sub\": \"werwerewrewrew-Qd80ehIEdFus\"," +
                        "\"tid\": \"some-tenant-id\"," +
                        "\"ver\": \"2.0\"}";

            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}.", Base64UrlEncoder.Encode(header), Base64UrlEncoder.Encode(payload));
        }
    }
}

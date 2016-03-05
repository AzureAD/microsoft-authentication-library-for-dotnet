using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Test.MSAL.NET.Unit.Mocks
{
    internal static class MockHelpers
    {
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
            HttpContent content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":\"some-scope1 some-scope2\",\"access_token\":\"some-access-token\",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"id_token\":\"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ik1uQ19WWmNBVGZNNXBPWWlKSE1iYTlnb0VLWSIsImtpZCI6Ik1uQ19WWmNBVGZNNXBPWWlKSE1iYTlnb0VLWSJ9.eyJhdWQiOiJlODU0YTRhNy02YzM0LTQ0OWMtYjIzNy1mYzdhMjgwOTNkODQiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNmMzZDUxZGQtZjBlNS00OTU5LWI0ZWEtYTgwYzRlMzZmZTVlL3YyLjAvIiwiaWF0IjoxNDU1ODMzODI4LCJuYmYiOjE0NTU4MzM4MjgsImV4cCI6MTQ1NTgzNzcyOCwiaXBhZGRyIjoiMTMxLjEwNy4xNTkuMTE3IiwibmFtZSI6Ik1hcmlvIFJvc3NpIiwib2lkIjoidW5pcXVlX2lkIiwicHJlZmVycmVkX3VzZXJuYW1lIjoiZGlzcGxheWFibGVAaWQuY29tIiwic3ViIjoiSzRfU0dHeEtxVzFTeFVBbWhnNkMxRjZWUGlGemN4LVFkODBlaElFZEZ1cyIsInRpZCI6IjZjM2Q1MWRkLWYwZTUtNDk1OS1iNGVhLWE4MGM0ZTM2ZmU1ZSIsInZlciI6IjIuMCJ9.Z6Xc_PzqTtB-2TjyZwPpFGgkAs47m95F_I-NHxtIJT-H20i_1kbcBdmJaj7lMjHhJwAAMM-tE-iBVF9f7jNmsDZAADt-HgtrrXaXxkIKMwQ_MuB-OI4uY9KYIurEqmkGvOlRUK1ZVNNf7IKE5pqNTOZzyFDEyG8SwSvAmN-J4VnrxFz3d47klHoKVKwLjWJDj7edR2UUkdUQ6ZRj7YBj9UjC8UrmVNLBmvyatPyu9KQxyNyJpmTBT2jDjMZ3J1Z5iL98zWw_Ez0-6W0ti87UaPreJO3hejqQE_pRa4rXMLpw3oAnyEE1H7n0F6tK_3lJndZi9uLTIsdSMEXVnZdoHg\",\"id_token_expires_in\":\"3600\",\"profile_info\":\"eyJ2ZXIiOiIxLjAiLCJuYW1lIjoiTWFyaW8gUm9zc2kiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJtYXJpb0BkZXZlbG9wZXJ0ZW5hbnQub25taWNyb3NvZnQuY29tIiwic3ViIjoiSzRfU0dHeEtxVzFTeFVBbWhnNkMxRjZWUGlGemN4LVFkODBlaElFZEZ1cyIsInRpZCI6IjZjM2Q1MWRkLWYwZTUtNDk1OS1iNGVhLWE4MGM0ZTM2ZmU1ZSJ9\"}");
            responseMessage.Content = content;
            return responseMessage;
        }

        public static HttpResponseMessage CreateInvalidGrantTokenResponseMessage()
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            HttpContent content = new StringContent("{\"error\":\"invalid_grant\",\"error_description\":\"AADSTS70002: Error validating credentials.AADSTS70008: The provided access grant is expired or revoked.Trace ID: f7ec686c-9196-4220-a754-cd9197de44e9Correlation ID: 04bb0cae-580b-49ac-9a10-b6c3316b1eaaTimestamp: 2015-09-16 07:24:55Z\",\"error_codes\":[70002,70008],\"timestamp\":\"2015-09-16 07:24:55Z\",\"trace_id\":\"f7ec686c-9196-4220-a754-cd9197de44e9\",\"correlation_id\":\"04bb0cae-580b-49ac-9a10-b6c3316b1eaa\"}");
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

        public static HttpResponseMessage CreateSuccessTokenResponseMessage(string uniqueId, string displayableId, string rootId, string[] scope)
        {
            string idToken = string.Format("someheader.{0}.somesignature", CreateIdToken(uniqueId, displayableId));
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":\"" +
                                  scope.AsSingleString() +
                                  "\",\"access_token\":\"some-access-token\",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"id_token\":\"" +
                                  idToken +
                                  "\",\"id_token_expires_in\":\"3600\",\"profile_info\":\"eyJ2ZXIiOiIxLjAiLCJuYW1lIjoiTWFyaW8gUm9zc2kiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJtYXJpb0BkZXZlbG9wZXJ0ZW5hbnQub25taWNyb3NvZnQuY29tIiwic3ViIjoiSzRfU0dHeEtxVzFTeFVBbWhnNkMxRjZWUGlGemN4LVFkODBlaElFZEZ1cyIsInRpZCI6IjZjM2Q1MWRkLWYwZTUtNDk1OS1iNGVhLWE4MGM0ZTM2ZmU1ZSJ9\",\"home_oid\":\"" +
                                  rootId + "\"}");
            responseMessage.Content = content;
            return responseMessage;
        }

        private static string CreateIdToken(string uniqueId, string displayableId)
        {
            string id = "{\"aud\": \"e854a4a7-6c34-449c-b237-fc7a28093d84\"," +
                        "\"iss\": \"https://login.microsoftonline.com/6c3d51dd-f0e5-4959-b4ea-a80c4e36fe5e/v2.0/\"," +
                        "\"iat\": 1455833828," +
                        "\"nbf\": 1455833828," +
                        "\"exp\": 1455837728," +
                        "\"ipaddr\": \"131.107.159.117\"," +
                        "\"name\": \"Mario Rossi\"," +
                        "\"oid\": \""+ uniqueId + "\"," +
                        "\"preferred_username\": \""+displayableId+"\"," +
                        "\"sub\": \"K4_SGGxKqW1SxUAmhg6C1F6VPiFzcx-Qd80ehIEdFus\"," +
                        "\"tid\": \"6c3d51dd-f0e5-4959-b4ea-a80c4e36fe5e\"," +
                        "\"ver\": \"2.0\"}";
            return Base64UrlEncoder.Encode(id);
        }
    }
}

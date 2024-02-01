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
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal static class MockHelpers
    {
        public const string TooManyRequestsContent = "Too many requests error";
        public static readonly TimeSpan TestRetryAfterDuration = TimeSpan.FromSeconds(120);

        public static readonly string B2CTokenResponseWithoutAT =
            "{\"id_token\":\"" + CreateIdTokenForB2C(TestConstants.Uid, TestConstants.Utid, TestConstants.B2CSignUpSignIn) + "  \"," +
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

        public static string GetTokenResponseWithNoOidClaim()
        {
            return "{\"token_type\": \"Bearer\"," +
                "\"scope\": \"https://management.core.windows.net//user_impersonation https://management.core.windows.net//.default\"," +
                "\"expires_in\": 4771," +
                "\"ext_expires_in\": 4771," +
                "\"access_token\": \"secret\"," +
                "\"refresh_token\": \"secret\"," +
                "\"id_token\": \"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6InVFQnBBVkVBTVBaUVpGS2lJRFVWdjFEdFRkayJ9.eyJhdWQiOiI0OTliODRhYy0xMzIxLTQyN2YtYWExNy0yNjdjYTY5NzU3OTgiLCJpc3MiOiJodHRwczovL2xvZ2luLndpbmRvd3MtcHBlLm5ldC85OGVjYjBlZi1iYjhkLTQyMTYtYjQ1YS03MGRmOTUwZGM2ZTMvdjIuMCIsImlhdCI6MTY4NDMxMzA1NCwibmJmIjoxNjg0MzEzMDU0LCJleHAiOjE2ODQzMTY5NTQsImFhaSI6InRlbmFudDogODExOTg5MGItNGMzZi00NjVkLTk4NDAtNTk5MTMxZDE0ZDk4LCBvYmplY3Q6IDRlNjJhMmI0LTBiZTYtNDI0Yi1hMDg0LWYyZmYwOTIxOGUyMyIsImF1dGhfdGltZSI6MTY4NDMxMzMzOCwiaG9tZV9vaWQiOiI0ZTYyYTJiNC0wYmU2LTQyNGItYTA4NC1mMmZmMDkyMThlMjMiLCJob21lX3B1aWQiOiIxMDAzREZGRDAwOURGODQ3IiwiaG9tZV90aWQiOiI4MTE5ODkwYi00YzNmLTQ2NWQtOTg0MC01OTkxMzFkMTRkOTgiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLXBwZS5uZXQvODExOTg5MGItNGMzZi00NjVkLTk4NDAtNTk5MTMxZDE0ZDk4LyIsIm5hbWUiOiJ0ZXN0X3Rlc3RfY3NwXzAyMiBUZWNobmljaWFuIiwibm9uY2UiOiIzMGJiYTU0Ny05MjViLTQxZWItOTFiYy05YTM1YTY3M2JmZDkiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJ1c2VyXzRlNjJhMmI0MGJlNjQyNGJhMDg0ZjJmZjA5MjE4ZTIzQHN1YmdkYXAuYWxpbmMueHl6IiwicmgiOiIwLkFBRUE3N0RzbUkyN0ZrSzBXbkRmbFEzRzQ2eUVtMGtoRTM5Q3FoY21mS2FYVjVnQkFKUS4iLCJzdWIiOiJBdWpMRFFwNXlSTVJjR3BQY0RCZnQ5TmI1dUZTS1l4RFpxNjUtZWJmSGxzIiwidGlkIjoiOThlY2IwZWYtYmI4ZC00MjE2LWI0NWEtNzBkZjk1MGRjNmUzIiwidXRpIjoiODZyeGRJTlhiMDJMY3RIMDltVW1BQSIsInZlciI6IjIuMCIsIndpZHMiOlsiODhkOGUzZTMtOGY1NS00YTFlLTk1M2EtOWI5ODk4Yjg4NzZiIiwiMDgzNzJiODctN2QwMi00ODJhLTllMDItZmIwM2VhNWZlMTkzIl0sInhtc19tcGNpIjoyNTkyMDAsInhtc19wY2kiOjM2MDAsInhtc193c2l0IjoxODAwfQ.no_signature\"," +
                "\"client_info\": \"eyJ1aWQiOiI0ZTYyYTJiNC0wYmU2LTQyNGItYTA4NC1mMmZmMDkyMThlMjMiLCJ1dGlkIjoiODExOTg5MGItNGMzZi00NjVkLTk4NDAtNTk5MTMxZDE0ZDk4In0\"}";
        }

        public static readonly string DefaultEmtpyFailureErrorMessage =
            "{\"the-error-is-not-here\":\"erorwithouterrorfield\",\"error_description\":\"AADSTS991: " +
                                        "This is an error message which doesn't contain the error field. " +
                                        "Trace ID: dd25f4fb-3e8d-458e-90e7-179524ce0000Correlation ID: " +
                                        "f11508ab-067f-40d4-83cb-ccc67bf57e45Timestamp: 2018-09-22 00:50:11Z\"," +
                                        "\"error_codes\":[90010],\"timestamp\":\"2018-09-22 00:50:11Z\"," +
                                        "\"trace_id\":\"dd25f4fb-3e8d-458e-90e7-179524ce0000\",\"correlation_id\":" +
                                        "\"f11508ab-067f-40d4-83cb-ccc67bf57e45\"}";

        public static string GetDefaultTokenResponse(string accessToken = TestConstants.ATSecret, string refreshToken = TestConstants.RTSecret)
        {
            return
          "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"refresh_in\":\"2400\",\"scope\":" +
          "\"r1/scope1 r1/scope2\",\"access_token\":\"" + accessToken + "\"" +
          ",\"refresh_token\":\"" + refreshToken + "\",\"client_info\"" +
          ":\"" + CreateClientInfo() + "\",\"id_token\"" +
          ":\"" + CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId) + "\"}";
        }

        public static string GetPopTokenResponse()
        {
            return
          "{\"token_type\":\"pop\",\"expires_in\":\"3599\",\"scope\":" +
          "\"r1/scope1 r1/scope2\",\"access_token\":\"" + TestConstants.ATSecret + "\"" +
          ",\"refresh_token\":\"" + TestConstants.RTSecret + "\",\"client_info\"" +
          ":\"" + CreateClientInfo() + "\",\"id_token\"" +
          ":\"" + CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId) +
          "\",\"id_token_expires_in\":\"3600\"}";
        }

        public static string GetHybridSpaTokenResponse(string spaCode)
        {
            return
            "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"refresh_in\":\"2400\",\"scope\":" +
            "\"r1/scope1 r1/scope2\",\"access_token\":\"" + TestConstants.ATSecret + "\"" +
            ",\"refresh_token\":\"" + TestConstants.RTSecret + "\",\"client_info\"" +
            ":\"" + CreateClientInfo() + "\",\"id_token\"" +
            ":\"" + CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId) +
            "\",\"spa_code\":\"" + spaCode + "\"" +
            ",\"id_token_expires_in\":\"3600\"}";
        }

        public static string GetBridgedHybridSpaTokenResponse(string spaAccountId)
        {
            return
            "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"refresh_in\":\"2400\",\"scope\":" +
            "\"r1/scope1 r1/scope2\",\"access_token\":\"" + TestConstants.ATSecret + "\"" +
            ",\"refresh_token\":\"" + TestConstants.RTSecret + "\",\"client_info\"" +
            ":\"" + CreateClientInfo() + "\",\"id_token\"" +
            ":\"" + CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId) +
            "\",\"spa_accountId\":\"" + spaAccountId + "\"" +
            ",\"id_token_expires_in\":\"3600\"}";
        }

        public static string GetMsiSuccessfulResponse(int expiresInHours = 1)
        {
            string expiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow.AddHours(expiresInHours));
            return
          "{\"access_token\":\"" + TestConstants.ATSecret + "\",\"expires_on\":\"" + expiresOn + "\",\"resource\":\"https://management.azure.com/\",\"token_type\":" +
          "\"Bearer\",\"client_id\":\"client_id\"}";
        }

        public static string GetMsiImdsSuccessfulResponse()
        {
            string expiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow.AddHours(1));
            return
          "{\"access_token\":\"" + TestConstants.ATSecret + "\",\"client_id\":\"client-id\"," +
          "\"expires_in\":\"12345\",\"expires_on\":\"" + expiresOn + "\",\"resource\":\"https://management.azure.com/\"," +
          "\"ext_expires_in\":\"12345\",\"token_type\":\"Bearer\"}";
        }

        public static string GetMsiErrorResponse()
        {
            return "{\"statusCode\":\"500\",\"message\":\"An unexpected error occured while fetching the AAD Token.\",\"correlationId\":\"7d0c9763-ff1d-4842-a3f3-6d49e64f4513\"}";
        }

        public static string GetSuccessfulCredentialResponse(
            string credential = "managed-identity-credential",
            ManagedIdentityIdType identityType = ManagedIdentityIdType.SystemAssigned,
            string client_id = "2d0d13ad-3a4d-4cfd-98f8-f20621d55ded",
            long expires_on = 0,
            string regional_token_url = "https://centraluseuap.mtlsauth.microsoft.com",
            string tenant_id = "72f988bf-86f1-41af-91ab-2d7cd011db47")
        {
            var identityTypeString = identityType.ToString();

            if (expires_on == 0)
            {
                long currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                expires_on = currentUnixTimestamp + 3600; // Add one hour (3600 seconds) for example
            }

            long refresh_in = expires_on / 2;

            return "{\"client_id\":\"" + client_id + "\",\"credential\":\"" + credential + "\",\"expires_on\":" + expires_on + ",\"identity_type\":\"" + identityTypeString + "\",\"refresh_in\":" + refresh_in + ",\"regional_token_url\":\"" + regional_token_url + "\",\"tenant_id\":\"" + tenant_id + "\"}";
        }

        public static string GetSuccessfulMtlsResponse()
        {
            return "{\"token_type\":\"Bearer\",\"expires_in\":86399,\"ext_expires_in\":86399,\"access_token\":\"some-token\"}";
        }

        public static string GetMtlsInvalidResourceError()
        {
            return @"{""error"":""invalid_resource"",
                       ""error_description"":""AADSTS500011: The resource principal named https://graph.microsoft.com/user.read was not found in the tenant named Cross Cloud B2B Test Tenant. This can happen if the application has not been installed by the administrator of the tenant or consented to by any user in the tenant. You might have sent your authentication request to the wrong tenant. Trace ID: 9d8cb0bf-7e34-40fd-babc-f6ff018a1800 Correlation ID: 42186e1b-17eb-46fb-b5b7-4c43cae4d336 Timestamp: 2023-12-08 22:20:25Z"",
                       ""error_codes"":[500011],
                       ""timestamp"":""2023-12-08 22:20:25Z"",
                       ""trace_id"":""9d8cb0bf-7e34-40fd-babc-f6ff018a1800"",
                       ""correlation_id"":""42186e1b-17eb-46fb-b5b7-4c43cae4d336"",
                       ""error_uri"":""https://eastus2euap.mtlsauth.microsoft.com/error?code=500011""}";
        }

        public static string GetMtlsInvalidScopeError70011()
        {
            return @"{""error"":""invalid_scope"",
                   ""error_description"":""AADSTS70011: The provided request must include a 'scope' input parameter. The provided value for the input parameter 'scope' is not valid. The scope user.read/.default is not valid. Trace ID: 9e8a0bd6-fb1b-45cf-8e00-95c2c73e1400 Correlation ID: 6ce4a5ab-87a1-4985-b06d-5ab08b5fa924 Timestamp: 2023-12-08 21:56:44Z"",
                   ""error_codes"":[70011],
                   ""timestamp"":""2023-12-08 21:56:44Z"",
                   ""trace_id"":""9e8a0bd6-fb1b-45cf-8e00-95c2c73e1400"",
                   ""correlation_id"":""6ce4a5ab-87a1-4985-b06d-5ab08b5fa924""}";
        }

        public static string GetMtlsInvalidScopeError1002012()
        {
            return @"{""error"":""invalid_scope"",
                    ""error_description"":""AADSTS1002012: The provided value for scope user.read is not valid. Client credential flows must have a scope value with /.default suffixed to the resource identifier (application ID URI). Trace ID: 8575f1d5-0144-4d71-87c8-2df9f1e30000 Correlation ID: a5469466-6c01-40e0-abf8-302d09c991e3 Timestamp: 2023-12-08 22:11:08Z"",
                    ""error_codes"":[1002012],
                    ""timestamp"":""2023-12-08 22:11:08Z"",
                    ""trace_id"":""8575f1d5-0144-4d71-87c8-2df9f1e30000"",
                    ""correlation_id"":""a5469466-6c01-40e0-abf8-302d09c991e3""}";
        }

        public static string GetMsiImdsErrorResponse()
        {
            return "{\"error\":\"invalid_resource\"," +
                "\"error_description\":\"AADSTS500011: The resource principal named user.read was not found in the tenant named Microsoft. " +
                "This can happen if the application has not been installed by the administrator of the tenant or consented to by any user in the tenant. " +
                "You might have sent your authentication request to the wrong tenant.\r\nTrace ID: 2dff494a-0226-4f41-8859-d9f560ca8903" +
                "\r\nCorrelation ID: 77145480-bc5a-4ebe-ae4d-e4a8b7d727cf\r\nTimestamp: 2022-11-10 23:12:37Z\"," +
                "\"error_codes\":[500011],\"timestamp\":\"2022-11-10 23:12:37Z\",\"trace_id\":\"2dff494a-0226-4f41-8859-d9f560ca8903\"," +
                "\"correlation_id\":\"77145480-bc5a-4ebe-ae4d-e4a8b7d727cf\",\"error_uri\":\"https://westus2.login.microsoft.com/error?code=500011\"}";
        }

        public static string InvalidTenantError900023()
        {
            return @"{
                ""error"":""invalid_request"",
                ""error_description"":""AADSTS900023: Specified tenant identifier 'invalid_tenant' is neither a valid DNS name, nor a valid external domain. Trace ID: f38df5f2-84c4-4195-bad6-8eca059b0b00 Correlation ID: e318f766-8581-445a-97fb-419f80d98d8b Timestamp: 2023-12-11 22:52:53Z"",
                ""error_codes"":[900023],
                ""timestamp"":""2023-12-11 22:52:53Z"",
                ""trace_id"":""f38df5f2-84c4-4195-bad6-8eca059b0b00"",
                ""correlation_id"":""e318f766-8581-445a-97fb-419f80d98d8b"",
                ""error_uri"":""https://centraluseuap.mtlsauth.microsoft.com/error?code=900023""
            }";
        }

        public static string WrongTenantError700016()
        {
            return @"{
                ""error"":""unauthorized_client"",
                ""error_description"":""AADSTS700016: Application with identifier '833aa854-2811-4f90-9620-c38070f595d7' was not found in the directory 'MSIDLAB4'. This can happen if the application has not been installed by the administrator of the tenant or consented to by any user in the tenant. You may have sent your authentication request to the wrong tenant. Trace ID: 68b0d98d-52e8-4e45-9282-9b3b09fc1800 Correlation ID: 75673189-3db2-408b-8384-16860ee0c0f0 Timestamp: 2023-12-11 22:54:25Z"",
                ""error_codes"":[700016],
                ""timestamp"":""2023-12-11 22:54:25Z"",
                ""trace_id"":""68b0d98d-52e8-4e45-9282-9b3b09fc1800"",
                ""correlation_id"":""75673189-3db2-408b-8384-16860ee0c0f0"",
                ""error_uri"":""https://centraluseuap.mtlsauth.microsoft.com/error?code=700016""
            }";
        }

        public static string WrongMtlsUrlError50171()
        {
            return @"{
                ""error"":""invalid_client"",
                ""error_description"":""AADSTS50171: The given audience can only be used in Mutual-TLS token calls. Trace ID: e350f752-0a39-43c2-a9a2-cbd7ff4a6f00 Correlation ID: 26bb13de-d2cf-4f8f-9f36-d7611c00fecb Timestamp: 2023-12-11 22:58:32Z"",
                ""error_codes"":[50171],
                ""timestamp"":""2023-12-11 22:58:32Z"",
                ""trace_id"":""e350f752-0a39-43c2-a9a2-cbd7ff4a6f00"",
                ""correlation_id"":""26bb13de-d2cf-4f8f-9f36-d7611c00fecb""
            }";
        }

        public static string SendTenantIdInCredentialValueError50027()
        {
            return @"{
                ""error"":""invalid_request"",
                ""error_description"":""AADSTS50027: JWT token is invalid or malformed. Trace ID: 6ca706cd-c0a1-4ec2-acb1-541b5a579a00 Correlation ID: 52955596-2fe6-43c6-b087-6038942c8254 Timestamp: 2023-12-11 23:02:08Z"",
                ""error_codes"":[50027],
                ""timestamp"":""2023-12-11 23:02:08Z"",
                ""trace_id"":""6ca706cd-c0a1-4ec2-acb1-541b5a579a00"",
                ""correlation_id"":""52955596-2fe6-43c6-b087-6038942c8254"",
                ""error_uri"":""https://mtlsauth.microsoft.com/error?code=50027""
            }";
        }

        public static string BadCredNoIssError90014()
        {
            return @"{
                ""error"":""invalid_request"",
                ""error_description"":""AADSTS90014: The required field 'iss' is missing from the credential. Ensure that you have all the necessary parameters for the login request. Trace ID: 605439e8-8f0e-43f5-9887-5281a05a5200 Correlation ID: abc63349-b90e-4b15-8fb7-edc9326ed3c8 Timestamp: 2023-12-11 23:14:38Z"",
                ""error_codes"":[90014],
                ""timestamp"":""2023-12-11 23:14:38Z"",
                ""trace_id"":""605439e8-8f0e-43f5-9887-5281a05a5200"",
                ""correlation_id"":""abc63349-b90e-4b15-8fb7-edc9326ed3c8"",
                ""error_uri"":""https://mtlsauth.microsoft.com/error?code=90014""
            }";
        }

        public static string BadCredNoAudError90014()
        {
            return @"{
                ""error"":""invalid_request"",
                ""error_description"":""AADSTS90014: The required field 'aud' is missing from the credential. Ensure that you have all the necessary parameters for the login request. Trace ID: 0b1cc102-98b7-4fa5-a11a-82520fa85a00 Correlation ID: 23811f20-96bb-4900-a1a3-6368ef8890b2 Timestamp: 2023-12-11 23:16:15Z"",
                ""error_codes"":[90014],
                ""timestamp"":""2023-12-11 23:16:15Z"",
                ""trace_id"":""0b1cc102-98b7-4fa5-a11a-82520fa85a00"",
                ""correlation_id"":""23811f20-96bb-4900-a1a3-6368ef8890b2"",
                ""error_uri"":""https://mtlsauth.microsoft.com/error?code=90014""
            }";
        }

        public static string BadCredBadAlgError5002738()
        {
            return @"{
                ""error"":""invalid_client"",
                ""error_description"":""AADSTS5002738: Invalid JWT token. 'HS256' is not a supported signature algorithm. Supported signing algorithms are: 'RS256,RS384,RS512' Trace ID: 2ed12465-8044-44af-bd27-b73b27e04a00 Correlation ID: bc26e294-ed13-4e6f-a225-28cdec2cc519 Timestamp: 2023-12-11 23:18:06Z"",
                ""error_codes"":[5002738],
                ""timestamp"":""2023-12-11 23:18:06Z"",
                ""trace_id"":""2ed12465-8044-44af-bd27-b73b27e04a00"",
                ""correlation_id"":""bc26e294-ed13-4e6f-a225-28cdec2cc519"",
                ""error_uri"":""https://mtlsauth.microsoft.com/error?code=5002738""
            }";
        }

        public static string BadCredMissingSha1Error5002723()
        {
            return @"{
                ""error"":""invalid_client"",
                ""error_description"":""AADSTS5002723: Invalid JWT token. No certificate SHA-1 thumbprint, certificate SHA-256 thumbprint, nor keyId specified in token header. Trace ID: 3ce71c90-8d35-4413-bedb-73337ec40c00 Correlation ID: 540e9fb1-db53-4b10-a0ca-047d03b97d10 Timestamp: 2023-12-11 23:51:16Z"",
                ""error_codes"":[5002723],
                ""timestamp"":""2023-12-11 23:51:16Z"",
                ""trace_id"":""3ce71c90-8d35-4413-bedb-73337ec40c00"",
                ""correlation_id"":""540e9fb1-db53-4b10-a0ca-047d03b97d10"",
                ""error_uri"":""https://mtlsauth.microsoft.com/error?code=5002723""
            }";
        }

        public static string BadTimeRangeError700024()
        {
            return @"{
                ""error"":""invalid_client"",
                ""error_description"":""AADSTS700024: Client assertion is not within its valid time range. Current time: 2023-12-11T23:52:19.6223401Z, assertion valid from 2018-01-18T01:30:22.0000000Z, expiry time of assertion 1970-01-01T00:00:00.0000000Z. Review the documentation at https://docs.microsoft.com/azure/active-directory/develop/active-directory-certificate-credentials . Trace ID: 2486d2c5-63a7-44f5-bb09-05e4c5494000 Correlation ID: fc5f1331-e3ef-44cb-b478-909a171010ab Timestamp: 2023-12-11 23:52:19Z"",
                ""error_codes"":[700024],
                ""timestamp"":""2023-12-11 23:52:19Z"",
                ""trace_id"":""2486d2c5-63a7-44f5-bb09-05e4c5494000"",
                ""correlation_id"":""fc5f1331-e3ef-44cb-b478-909a171010ab"",
                ""error_uri"":""https://mtlsauth.microsoft.com/error?code=700024""
            }";
        }

        public static string IdentifierMismatchError700021()
        {
            return @"{
                ""error"":""invalid_client"",
                ""error_description"":""AADSTS700021: Client assertion application identifier doesn't match 'client_id' parameter. Review the documentation at https://docs.microsoft.com/azure/active-directory/develop/active-directory-certificate-credentials . Trace ID: 1180e895-2f6b-4504-b0cf-f49632647100 Correlation ID: 88c237d8-7867-4e68-89e4-bc5a6d3b2159 Timestamp: 2023-12-11 23:55:14Z"",
                ""error_codes"":[700021],
                ""timestamp"":""2023-12-11 23:55:14Z"",
                ""trace_id"":""1180e895-2f6b-4504-b0cf-f49632647100"",
                ""correlation_id"":""88c237d8-7867-4e68-89e4-bc5a6d3b2159"",
                ""error_uri"":""https://mtlsauth.microsoft.com/error?code=700021""
            }";
        }

        public static string MissingCertError392200()
        {
            return @"{
                ""error"":""invalid_request"",
                ""error_description"":""AADSTS392200: Client certificate is missing from the request. Trace ID: 35f8d355-5be8-4028-83e5-aeb609b8d500 Correlation ID: e10c5bea-3b7e-42a2-a251-705d6e7aa48d Timestamp: 2023-12-12 00:11:34Z"",
                ""error_codes"":[392200],
                ""timestamp"":""2023-12-12 00:11:34Z"",
                ""trace_id"":""35f8d355-5be8-4028-83e5-aeb609b8d500"",
                ""correlation_id"":""e10c5bea-3b7e-42a2-a251-705d6e7aa48d"",
                ""error_uri"":""https://mtlsauth.microsoft.com/error?code=392200""
            }";
        }

        public static string ExpiredCertError392204()
        {
            return @"{
                ""error"":""invalid_client"",
                ""error_description"":""AADSTS392204: The provided client certificate has expired. Trace ID: 44b6984d-e6bd-4374-a9c7-5738ea6b6800 Correlation ID: 7279f188-cd3a-4f09-8236-fc7044d2080a Timestamp: 2023-12-12 00:18:55Z"",
                ""error_codes"":[392204],
                ""timestamp"":""2023-12-12 00:18:55Z"",
                ""trace_id"":""44b6984d-e6bd-4374-a9c7-5738ea6b6800"",
                ""correlation_id"":""7279f188-cd3a-4f09-8236-fc7044d2080a"",
                ""error_uri"":""https://mtlsauth.microsoft.com/error?code=392204""
            }";
        }

        public static string CertMismatchError500181()
        {
            return @"{
                ""error"":""invalid_request"",
                ""error_description"":""AADSTS500181: The TLS certificate provided does not match the certificate in the assertion. Trace ID: 2781e26e-d4ed-4947-9d95-11dfa81a5900 Correlation ID: e19df97b-3909-4c41-a439-91dc4ec8355b Timestamp: 2023-12-12 00:27:10Z"",
                ""error_codes"":[500181],
                ""timestamp"":""2023-12-12 00:27:10Z"",
                ""trace_id"":""2781e26e-d4ed-4947-9d95-11dfa81a5900"",
                ""correlation_id"":""e19df97b-3909-4c41-a439-91dc4ec8355b""
            }";
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

        public static HttpResponseMessage CreateServerErrorMessage(HttpStatusCode statusCode, int? retryAfter = null)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(statusCode);
            if (retryAfter != null)
            {
                responseMessage.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(retryAfter.Value));
            }
            responseMessage.Content = new StringContent("Server Error 500-599");
            return responseMessage;
        }

        public static HttpResponseMessage CreateRequestTimeoutResponseMessage()
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            responseMessage.Content = new StringContent("Request Timed Out.");
            return responseMessage;
        }

        internal static HttpResponseMessage CreateFailureMessage(HttpStatusCode code, string message)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(code);
            responseMessage.Content = new StringContent(message);
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

        public static HttpResponseMessage CreateSuccessTokenResponseMessage(bool foci = false, string accessToken = TestConstants.ATSecret, string refreshToken = TestConstants.RTSecret)
        {
            return CreateSuccessResponseMessage(
                foci ? GetFociTokenResponse() : GetDefaultTokenResponse(accessToken, refreshToken));
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

        public static HttpResponseMessage CreateSuccessfulClientCredentialTokenResponseMessage(
            string token = "header.payload.signature",
            string expiry = "3599",
            string tokenType = "Bearer")
        {
            return CreateSuccessResponseMessage(
                "{\"token_type\":\"" + tokenType + "\",\"expires_in\":\"" + expiry + "\",\"access_token\":\"" + token + "\"}");
        }

        public static HttpResponseMessage CreateSuccessTokenResponseMessage(
            string uniqueId,
            string displayableId,
            string[] scope,
            bool foci = false,
            string utid = TestConstants.Utid,
            string accessToken = "some-access-token",
            string refreshToken = "OAAsomethingencrypedQwgAA")
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            string stringContent = CreateSuccessTokenResponseString(uniqueId, displayableId, scope, foci, utid, accessToken, refreshToken);
            HttpContent content = new StringContent(stringContent);
            responseMessage.Content = content;
            return responseMessage;
        }

        public static string CreateSuccessTokenResponseString(string uniqueId,
            string displayableId,
            string[] scope,
            bool foci = false,
            string utid = TestConstants.Utid,
            string accessToken = "some-access-token",
            string refreshToken = "OAAsomethingencrypedQwgAA")
        {
            string idToken = CreateIdToken(uniqueId, displayableId, TestConstants.Utid);
            string stringContent = "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"refresh_in\":\"2400\",\"scope\":\"" +
                                  scope.AsSingleString() +
                                  "\",\"access_token\":\"" + accessToken + "\",\"refresh_token\":\"" + refreshToken + "\",\"id_token\":\"" +
                                  idToken +
                                  (foci ? "\",\"foci\":\"1" : "") +
                                  "\",\"id_token_expires_in\":\"3600\",\"client_info\":\"" + CreateClientInfo(uniqueId, utid) + "\"}";

            return stringContent;
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
                        "  \"oid\": \"" + uniqueId + "\"," +
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

        public static MsalTokenResponse CreateMsalRunTimeBrokerTokenResponse(string accessToken = null, string tokenType = null)
        {
            return new MsalTokenResponse()
            {
                AccessToken = accessToken ?? TestConstants.UserAccessToken,
                IdToken = null,
                CorrelationId = null,
                Scope = TestConstants.ScopeStr,
                ExpiresIn = 3600,
                ClientInfo = null,
                TokenType = tokenType ?? "Bearer",
                WamAccountId = TestConstants.LocalAccountId,
                TokenSource = TokenSource.Broker
            };
        }
    }
}

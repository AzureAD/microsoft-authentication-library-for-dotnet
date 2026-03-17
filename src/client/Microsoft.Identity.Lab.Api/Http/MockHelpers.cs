// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Castle.Core.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    /// <summary>
    /// Provides reusable mock helpers and canned HTTP/token responses for MSAL test scenarios.
    /// </summary>
    public static class MockHelpers
    {
        /// <summary>
        /// Gets the default plain-text content used for mocked HTTP 429 responses.
        /// </summary>
        public const string TooManyRequestsContent = "Too many requests error";

        /// <summary>
        /// Gets the default Retry-After duration used in mocked throttling responses.
        /// </summary>
        public static readonly TimeSpan TestRetryAfterDuration = TimeSpan.FromSeconds(120);

        /// <summary>
        /// Gets a B2C token response payload that intentionally does not include an access token.
        /// Useful for testing parsing and failure handling paths.
        /// </summary>
        public static readonly string B2CTokenResponseWithoutAT =
            "{\"id_token\":\"" + CreateIdTokenForB2C(TestConstants.Uid, TestConstants.Utid, TestConstants.B2CSignUpSignIn) + "  \"," +
            "\"token_type\":\"Bearer\",\"not_before\":1585658742," +
            "\"client_info\":\"" + CreateClientInfo(TestConstants.Uid + "-" + TestConstants.B2CSignUpSignIn, TestConstants.Utid) + "\"," +
            "\"scope\":\"\"," +
            "\"refresh_token\":\"eyJraWQiOiJjcGltY29yZV8wOTI1MjAxNSIsInZlciI6IjEuMCIsInppcCI6IkRlZmxhdGUiLCJzZXIiOiIxLjAifQ..58S7QKY4AVcJS620.mMAGPkA5-v2QL4-kfB7sThyLQec7ZLyd2b-3-GBly5fLNVkbO9GVo9ZzqbaXbuzkNpj4iSITIRjfK4mBEcNU7s7EieHBbsRP8oee3feUuOzzAc61ZQBmTAkYsjEVa4iTSCxM-eU5n1fyZ1lIK6s33lOzylEs5pVT75HMvr_iLEd_2_QN0Y3ql2NVx1kPJsqk4TR0vfG2vum60sr5IBd2TcIamSAfByzfS6LUfVTicbVuWW7GHbJaQtFiE2tOhoJD_bePKGwWX-UwakMe3A4CfKbpT20OIs_o1UPcQUCGmn7XUjBrEPiaPcRHjVCes7ptGR4uTE7emHl9zHq4btl8poHg7iWG4gEmmp0FFvi6XhFOZosotSTTn72SdEkf-o93SmMrlxMRMMFdzEjqbyaiZSwirYfhbNMPcy_jeQ3BL0cr5UreIhxLkSj_xc9A3vDHVK8a3d6IcBa_x1Wwrt_mzEynI1ldgmQwxyda_Xti1JS3OdBQ0ZIkSiw6Z6l8Vmw-kGgkmWOfYjaFWI-vsV5TGYRUA7UnnbzXfbR1x1KwmVs28ssvl_6lsjqWrbBWMUduPGWA1THZzXEnf-MqA1cJfQRq.vRqgMxW_pIJoPUzNOxKUpQ\"," +
            "\"refresh_token_expires_in\":1209600}";

        /// <summary>
        /// Gets a default ADFS token response payload for unit and integration tests.
        /// </summary>
        public static readonly string DefaultAdfsTokenResponse =
            "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":" +
            "\"r1/scope1 r1/scope2\",\"access_token\":\"some-access-token\"" +
            ",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"id_token\"" +
            ":\"" + CreateAdfsIdToken(TestConstants.OnPremiseDisplayableId) +
            "\",\"id_token_expires_in\":\"3600\"}";

        /// <summary>
        /// Gets an error payload that omits the standard <c>error</c> field.
        /// Useful for validating fallback error parsing logic.
        /// </summary>
        public static readonly string DefaultEmtpyFailureErrorMessage =
            "{\"the-error-is-not-here\":\"erorwithouterrorfield\",\"error_description\":\"AADSTS991: " +
                                        "This is an error message which doesn't contain the error field. " +
                                        "Trace ID: dd25f4fb-3e8d-458e-90e7-179524ce0000Correlation ID: " +
                                        "f11508ab-067f-40d4-83cb-ccc67bf57e45Timestamp: 2018-09-22 00:50:11Z\"," +
                                        "\"error_codes\":[90010],\"timestamp\":\"2018-09-22 00:50:11Z\"," +
                                        "\"trace_id\":\"dd25f4fb-3e8d-458e-90e7-179524ce0000\",\"correlation_id\":" +
                                        "\"f11508ab-067f-40d4-83cb-ccc67bf57e45\"}";

        /// <summary>
        /// Creates a token response payload that contains the <c>foci</c> marker.
        /// </summary>
        /// <returns>A JSON token response string for FOCI-enabled scenarios.</returns>
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

        /// <summary>
        /// Creates a token response whose ID token does not contain the <c>oid</c> claim.
        /// </summary>
        /// <returns>A JSON token response string missing the object identifier claim.</returns>
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

        /// <summary>
        /// Creates a default successful OAuth token response.
        /// </summary>
        /// <param name="accessToken">The access token value to include in the response.</param>
        /// <param name="refreshToken">The refresh token value to include in the response.</param>
        /// <returns>A JSON token response string.</returns>
        public static string GetDefaultTokenResponse(string accessToken = TestConstants.ATSecret, string refreshToken = TestConstants.RTSecret)
        {
            return
          "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"refresh_in\":\"2400\",\"scope\":" +
          "\"r1/scope1 r1/scope2\",\"access_token\":\"" + accessToken + "\"" +
          ",\"refresh_token\":\"" + refreshToken + "\",\"client_info\"" +
          ":\"" + CreateClientInfo() + "\",\"id_token\"" +
          ":\"" + CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId) + "\"}";
        }

        /// <summary>
        /// Creates a successful PoP token response.
        /// </summary>
        /// <returns>A JSON token response string containing a PoP token type.</returns>
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

        /// <summary>
        /// Creates a hybrid SPA token response that includes a <c>spa_code</c>.
        /// </summary>
        /// <param name="spaCode">The SPA code to include in the mocked response.</param>
        /// <returns>A JSON token response string.</returns>
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

        /// <summary>
        /// Creates a bridged hybrid SPA token response that includes a <c>spa_accountId</c>.
        /// </summary>
        /// <param name="spaAccountId">The SPA account identifier to include in the mocked response.</param>
        /// <returns>A JSON token response string.</returns>
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

        /// <summary>
        /// Creates a successful managed identity token response for IMDS v1 or v2 style payloads.
        /// </summary>
        /// <param name="expiresInHours">The number of hours from now until expiry.</param>
        /// <param name="useIsoFormat">Whether to emit the expiry value in ISO 8601 format instead of Unix time.</param>
        /// <param name="imdsV2">Whether to emit an IMDS v2-style response.</param>
        /// <returns>A JSON managed identity response string.</returns>
        public static string GetMsiSuccessfulResponse(
            int expiresInHours = 1,
            bool useIsoFormat = false,
            bool imdsV2 = false)
        {
            var expiresOnKey = imdsV2 ? "expires_in" : "expires_on";
            string expiresOnValue;
            if (useIsoFormat)
            {
                // Return ISO 8601 format
                expiresOnValue = DateTime.UtcNow.AddHours(expiresInHours).ToString("o", CultureInfo.InvariantCulture);
            }
            else
            {
                // Return Unix timestamp format
                expiresOnValue = DateTimeHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow.AddHours(expiresInHours));
            }

            var tokenType = imdsV2 ? "mtls_pop" : "Bearer";

            return
                "{\"access_token\":\"" + TestConstants.ATSecret + "\",\"" + expiresOnKey + "\":\"" + expiresOnValue + "\",\"resource\":\"https://management.azure.com/\"," +
                "\"token_type\":\"" + tokenType + "\",\"client_id\":\"client_id\"}";
        }

        /// <summary>
        /// Creates malformed JSON based on a managed identity success response.
        /// </summary>
        /// <returns>An invalid JSON string useful for parse-failure tests.</returns>
        public static string GetMsiErrorBadJson()
        {
            string successResponse = GetMsiSuccessfulResponse();
            return successResponse.Replace("{", "|");
        }

        /// <summary>
        /// Creates a managed identity error response tailored to a given source type.
        /// </summary>
        /// <param name="source">The managed identity source being simulated.</param>
        /// <returns>A JSON error payload for the requested managed identity source.</returns>
        public static string GetMsiErrorResponse(ManagedIdentitySource source)
        {
            switch (source)
            {
                case ManagedIdentitySource.AppService:
                    return "{\"statusCode\":500,\"message\":\"An unexpected error occured while fetching the AAD Token.\",\"correlationId\":\"4ce26535-1769-4001-96e3-9019ce00922d\"}";

                case ManagedIdentitySource.Imds:
                case ManagedIdentitySource.AzureArc:
                case ManagedIdentitySource.ServiceFabric:
                    return "{\"error\":\"invalid_resource\",\"error_description\":\"AADSTS500011: The resource principal named scope was not found in the tenant named Microsoft. This can happen if the application has not been installed by the administrator of the tenant or consented to by any user in the tenant. You might have sent your authentication request to the wrong tenant.\\r\\nTrace ID: GUID\\r\\nCorrelation ID: GUID\\r\\nTimestamp: 2024-02-14 23:11:50Z\",\"error_codes\":\"[500011]\",\"timestamp\":\"2022-11-10 23:11:50Z\",\"trace_id\":\"GUID\",\"correlation_id\":\"GUID\",\"error_uri\":\"errorUri\"}";

                case ManagedIdentitySource.CloudShell:
                    return "{\"error\":{\"code\":\"AudienceNotSupported\",\"message\":\"Audience scope is not a supported MSI token audience.Supported audiences:https://management.core.windows.net/,https://management.azure.com/,https://graph.windows.net/,https://vault.azure.net,https://datalake.azure.net/,https://outlook.office365.com/,https://graph.microsoft.com/,https://batch.core.windows.net/,https://analysis.windows.net/powerbi/api,https://storage.azure.com/,https://rest.media.azure.net,https://api.loganalytics.io,https://ossrdbms-aad.database.windows.net,https://www.yammer.com,https://digitaltwins.azure.net,0b07f429-9f4b-4714-9392-cc5e8e80c8b0,822c8694-ad95-4735-9c55-256f7db2f9b4,https://dev.azuresynapse.net,https://database.windows.net,https://quantum.microsoft.com,https://iothubs.azure.net,2ff814a6-3304-4ab8-85cb-cd0e6f879c1d,https://azuredatabricks.net/,ce34e7e5-485f-4d76-964f-b3d2b16d1e4f,https://azure-devices-provisioning.net,https://managedhsm.azure.net,499b84ac-1321-427f-aa17-267ca6975798,https://api.adu.microsoft.com/,https://purview.azure.net/,6dae42f8-4368-4678-94ff-3960e28e3630\"}}";

                default:
                    return "";
            }
        }

        /// <summary>
        /// Creates a representative IMDS error response payload.
        /// </summary>
        /// <returns>A JSON error string for IMDS resource resolution failures.</returns>
        public static string GetMsiImdsErrorResponse()
        {
            return "{\"error\":\"invalid_resource\"," +
                "\"error_description\":\"AADSTS500011: The resource principal named user.read was not found in the tenant named Microsoft. " +
                "This can happen if the application has not been installed by the administrator of the tenant or consented to by any user in the tenant. " +
                "You might have sent your authentication request to the wrong tenant.\\r\\nTrace ID: 2dff494a-0226-4f41-8859-d9f560ca8903" +
                "\\r\\nCorrelation ID: 77145480-bc5a-4ebe-ae4d-e4a8b7d727cf\\r\\nTimestamp: 2022-11-10 23:12:37Z\"," +
                "\"error_codes\":[500011],\"timestamp\":\"2022-11-10 23:12:37Z\",\"trace_id\":\"2dff494a-0226-4f41-8859-d9f560ca8903\"," +
                "\"correlation_id\":\"77145480-bc5a-4ebe-ae4d-e4a8b7d727cf\",\"error_uri\":\"https://westus2.login.microsoft.com/error?code=500011\"}";
        }

        /// <summary>
        /// Creates a Base64Url-encoded client_info payload.
        /// </summary>
        /// <param name="uid">The user identifier to encode.</param>
        /// <param name="utid">The tenant identifier to encode.</param>
        /// <param name="CreateClientInfoForS2S">Whether to create a service-to-service style payload instead of a user payload.</param>
        /// <returns>An encoded <c>client_info</c> string.</returns>
        public static string CreateClientInfo(string uid = TestConstants.Uid, string utid = TestConstants.Utid, bool CreateClientInfoForS2S = false)
        {
            if (CreateClientInfoForS2S)
            {
                return Base64UrlHelpers.Encode("{\"authz\":[\"value1\",\"value2\"]}");
            }

            return Base64UrlHelpers.Encode("{\"uid\":\"" + uid + "\",\"utid\":\"" + utid + "\"}");
        }

        /// <summary>
        /// Creates a readable stream from a string for HTTP/content testing scenarios.
        /// </summary>
        /// <param name="s">The string to place into the stream.</param>
        /// <returns>A stream positioned at the beginning of the provided content.</returns>
        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Creates a mocked server error HTTP response, optionally including a Retry-After header.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <param name="retryAfter">An optional Retry-After value in seconds.</param>
        /// <returns>A configured <see cref="HttpResponseMessage"/>.</returns>
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

        /// <summary>
        /// Creates a mocked HTTP 408 timeout response.
        /// </summary>
        /// <returns>A timeout <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage CreateRequestTimeoutResponseMessage()
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            responseMessage.Content = new StringContent("Request Timed Out.");
            return responseMessage;
        }

        /// <summary>
        /// Creates a successful token response message from explicit scopes, ID token, and client info values.
        /// </summary>
        /// <param name="scopes">The scopes to include in the response.</param>
        /// <param name="idToken">The ID token to include in the response.</param>
        /// <param name="clientInfo">The client_info payload to include in the response.</param>
        /// <returns>A success <see cref="HttpResponseMessage"/> containing a token payload.</returns>
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

        /// <summary>
        /// Creates a default successful token response message, optionally marked as FOCI-enabled.
        /// </summary>
        /// <param name="foci">Whether to include the FOCI marker in the response.</param>
        /// <param name="accessToken">The access token value to include.</param>
        /// <param name="refreshToken">The refresh token value to include.</param>
        /// <returns>A success <see cref="HttpResponseMessage"/> containing the token payload.</returns>
        internal static HttpResponseMessage CreateSuccessTokenResponseMessage(bool foci = false, string accessToken = TestConstants.ATSecret, string refreshToken = TestConstants.RTSecret)
        {
            return CreateSuccessResponseMessage(
                foci ? GetFociTokenResponse() : GetDefaultTokenResponse(accessToken, refreshToken));
        }

        /// <summary>
        /// Creates a successful token response message with custom UID, UTID, and displayable name values.
        /// </summary>
        /// <param name="uid">The user object identifier.</param>
        /// <param name="utid">The tenant identifier.</param>
        /// <param name="displayableName">The displayable username to encode into the ID token.</param>
        /// <returns>A success <see cref="HttpResponseMessage"/> containing the token payload.</returns>
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

        /// <summary>
        /// Creates a successful ADFS token response message.
        /// </summary>
        /// <returns>A success <see cref="HttpResponseMessage"/> containing the default ADFS payload.</returns>
        public static HttpResponseMessage CreateAdfsSuccessTokenResponseMessage()
        {
            return CreateSuccessResponseMessage(DefaultAdfsTokenResponse);
        }

        /// <summary>
        /// Creates a token failure response message with customizable error metadata.
        /// </summary>
        /// <param name="error">The OAuth error value.</param>
        /// <param name="subError">An optional suberror value.</param>
        /// <param name="correlationId">An optional correlation identifier.</param>
        /// <param name="customStatusCode">An optional custom HTTP status code.</param>
        /// <param name="errorCode">The AADSTS error code prefix to include in the description.</param>
        /// <returns>A failure <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage CreateFailureTokenResponseMessage(
            string error,
            string subError = null,
            string correlationId = null,
            HttpStatusCode? customStatusCode = null,
            string errorCode = "AADSTS00000")
        {
            string message = "{\"error\":\"" + error + "\",\"error_description\":\"" + errorCode + ": Error for test." +
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

        /// <summary>
        /// Creates an <c>invalid_grant</c> token response message, optionally including suberror and claims values.
        /// </summary>
        /// <param name="subError">An optional suberror value.</param>
        /// <param name="claims">Optional claims challenge content.</param>
        /// <returns>A failure <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage CreateInvalidGrantTokenResponseMessage(string subError = null, string claims = null)
        {
            return CreateFailureMessage(HttpStatusCode.BadRequest,
                "{\"error\":\"invalid_grant\",\"error_description\":\"AADSTS70002: Error " +
                "validating credentials.AADSTS70008: The provided access grant is expired " +
                "or revoked.Trace ID: f7ec686c-9196-4220-a754-cd9197de44e9Correlation ID: " +
                "04bb0cae-580b-49ac-9a10-b6c3316b1eaaTimestamp: 2015-09-16 07:24:55Z\"," +
                "\"error_codes\":[70002,70008],\"timestamp\":\"2015-09-16 07:24:55Z\"," +
                "\"trace_id\":\"f7ec686c-9196-4220-a754-cd9197de44e9\"," +
                (subError != null ? ("\"suberror\":" + "\"" + subError + "\",") : "") +
                (claims != null ? ("\"claims\":" + "\"" + claims + "\",") : "") +
                "\"correlation_id\":" +
                "\"04bb0cae-580b-49ac-9a10-b6c3316b1eaa\"}");
        }

        /// <summary>
        /// Creates an <c>invalid_request</c> token response message.
        /// </summary>
        /// <returns>A failure <see cref="HttpResponseMessage"/>.</returns>
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

        /// <summary>
        /// Creates an <c>invalid_client</c> token response message.
        /// </summary>
        /// <returns>A failure <see cref="HttpResponseMessage"/>.</returns>
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

        /// <summary>
        /// Creates a failure response that has no standard <c>error</c> field.
        /// </summary>
        /// <returns>A failure <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage CreateNoErrorFieldResponseMessage()
        {
            return CreateFailureMessage(HttpStatusCode.BadRequest, DefaultEmtpyFailureErrorMessage);
        }

        /// <summary>
        /// Creates a 404 failure response message with a non-standard error payload.
        /// </summary>
        /// <returns>A not-found <see cref="HttpResponseMessage"/>.</returns>
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

        /// <summary>
        /// Creates a response message whose content is explicitly <see langword="null"/>.
        /// </summary>
        /// <returns>A failure <see cref="HttpResponseMessage"/> with null content.</returns>
        public static HttpResponseMessage CreateNullResponseMessage()
        {
            return CreateNullMessage(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Creates a response message whose content is an empty string.
        /// </summary>
        /// <returns>A failure <see cref="HttpResponseMessage"/> with empty content.</returns>
        public static HttpResponseMessage CreateEmptyResponseMessage()
        {
            return CreateFailureMessage(HttpStatusCode.BadRequest, string.Empty);
        }

        /// <summary>
        /// Creates a successful client credential token response message.
        /// </summary>
        /// <param name="token">The access token value to return.</param>
        /// <param name="expiry">The expiry value, in seconds, to include in the response.</param>
        /// <param name="tokenType">The token type to include in the response.</param>
        /// <returns>A success <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage CreateSuccessfulClientCredentialTokenResponseMessage(
            string token = "header.payload.signature",
            string expiry = "3599",
            string tokenType = "Bearer")
        {
            return CreateSuccessResponseMessage(
                "{\"token_type\":\"" + tokenType + "\",\"expires_in\":\"" + expiry + "\",\"access_token\":\"" + token + "\",\"additional_param1\":\"value1\",\"additional_param2\":\"value2\",\"additional_param3\":\"value3\"}");
        }

        /// <summary>
        /// Creates a successful client credential token response message that includes <c>client_info</c>.
        /// </summary>
        /// <param name="token">The access token value to return.</param>
        /// <param name="expiry">The expiry value, in seconds, to include in the response.</param>
        /// <param name="tokenType">The token type to include in the response.</param>
        /// <param name="CreateClientInfoForS2S">Whether to emit service-to-service style client_info.</param>
        /// <returns>A success <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage CreateSuccessfulClientCredentialTokenResponseWithClientInfoMessage(
            string token = "header.payload.signature",
            string expiry = "3599",
            string tokenType = "Bearer",
            bool CreateClientInfoForS2S = false
            )
        {
            return CreateSuccessResponseMessage(
                "{\"token_type\":\"" + tokenType + "\",\"expires_in\":\"" + expiry + "\",\"access_token\":\"" + token + "\",\"additional_param1\":\"value1\",\"additional_param2\":\"value2\",\"additional_param3\":\"value3\",\"client_info\":\"" + CreateClientInfo(null, null, CreateClientInfoForS2S) + "\"}");
        }

        /// <summary>
        /// Creates a successful client credential token response message with caller-supplied additional parameters.
        /// </summary>
        /// <param name="token">The access token value to return.</param>
        /// <param name="expiry">The expiry value, in seconds, to include in the response.</param>
        /// <param name="tokenType">The token type to include in the response.</param>
        /// <param name="additionalparams">Raw JSON fragment containing additional properties to append.</param>
        /// <returns>A success <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage CreateSuccessfulClientCredentialTokenResponseWithAdditionalParamsMessage(
            string token = "header.payload.signature",
            string expiry = "3599",
            string tokenType = "Bearer",
            string additionalparams = ",\"additional_param1\":\"value1\",\"additional_param2\":\"value2\",\"additional_param3\":\"value3\",\"additional_param4\":[\"GUID\",\"GUID2\",\"GUID3\"],\"additional_param5\":{\"value5json\":\"value5\"}"
            )
        {
            return CreateSuccessResponseMessage(
                "{\"token_type\":\"" + tokenType + "\",\"expires_in\":\"" + expiry + "\",\"access_token\":\"" + token + "\"" + additionalparams + "}");
        }

        /// <summary>
        /// Creates a successful token response message using explicit token identity and scope values.
        /// </summary>
        /// <param name="uniqueId">The unique user/object identifier to embed in the ID token.</param>
        /// <param name="displayableId">The displayable identifier to embed in the ID token.</param>
        /// <param name="scope">The scopes to include in the response.</param>
        /// <param name="foci">Whether to include the FOCI marker.</param>
        /// <param name="utid">The tenant identifier to include in client_info.</param>
        /// <param name="accessToken">The access token value.</param>
        /// <param name="refreshToken">The refresh token value.</param>
        /// <returns>A success <see cref="HttpResponseMessage"/>.</returns>
        internal static HttpResponseMessage CreateSuccessTokenResponseMessage(
            string uniqueId,
            string displayableId,
            string[] scope,
            bool foci = false,
            string utid = TestConstants.Utid,
            string accessToken = TestConstants.ATSecret,
            string refreshToken = "OAAsomethingencrypedQwgAA")
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            string stringContent = CreateSuccessTokenResponseString(uniqueId, displayableId, scope, foci, utid, accessToken, refreshToken);
            HttpContent content = new StringContent(stringContent);
            responseMessage.Content = content;
            return responseMessage;
        }

        /// <summary>
        /// Creates the JSON content for a successful token response using explicit token identity and scope values.
        /// </summary>
        /// <param name="uniqueId">The unique user/object identifier to embed in the ID token.</param>
        /// <param name="displayableId">The displayable identifier to embed in the ID token.</param>
        /// <param name="scope">The scopes to include in the response.</param>
        /// <param name="foci">Whether to include the FOCI marker.</param>
        /// <param name="utid">The tenant identifier to include in client_info.</param>
        /// <param name="accessToken">The access token value.</param>
        /// <param name="refreshToken">The refresh token value.</param>
        /// <returns>A JSON token response string.</returns>
        public static string CreateSuccessTokenResponseString(string uniqueId,
            string displayableId,
            string[] scope,
            bool foci = false,
            string utid = TestConstants.Utid,
            string accessToken = TestConstants.ATSecret,
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

        /// <summary>
        /// Creates an ID token using the default tenant identifier.
        /// </summary>
        /// <param name="uniqueId">The unique user/object identifier to include as the OID claim.</param>
        /// <param name="displayableId">The displayable identifier to include as the preferred username claim.</param>
        /// <returns>A serialized mock ID token string.</returns>
        public static string CreateIdToken(string uniqueId, string displayableId)
        {
            return CreateIdToken(uniqueId, displayableId, TestConstants.Utid);
        }

        /// <summary>
        /// Creates an ID token using the provided tenant identifier.
        /// </summary>
        /// <param name="uniqueId">The unique user/object identifier to include as the OID claim.</param>
        /// <param name="displayableId">The displayable identifier to include as the preferred username claim.</param>
        /// <param name="tenantId">The tenant identifier to include as the TID claim.</param>
        /// <returns>A serialized mock ID token string.</returns>
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

        /// <summary>
        /// Creates a mock ADFS ID token.
        /// </summary>
        /// <param name="upn">The UPN value to include in the token.</param>
        /// <returns>A serialized mock ADFS ID token string.</returns>
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

        /// <summary>
        /// Creates a successful WebFinger response message for the specified trusted realm href.
        /// </summary>
        /// <param name="href">The trusted realm href to include in the response.</param>
        /// <returns>A success <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage CreateSuccessWebFingerResponseMessage(string href)
        {
            return
                CreateSuccessResponseMessage(
                    "{\"subject\": \"https://fs.contoso.com\",\"links\": [{\"rel\": " +
                    "\"http://schemas.microsoft.com/rel/trusted-realm\"," +
                    "\"href\": \"" + href + "\"}]}");
        }

        /// <summary>
        /// Creates a successful default WebFinger response message.
        /// </summary>
        /// <returns>A success <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage CreateSuccessWebFingerResponseMessage()
        {
            return
                CreateSuccessWebFingerResponseMessage("https://fs.contoso.com");
        }

        /// <summary>
        /// Creates a generic HTTP 200 response containing the provided success payload.
        /// </summary>
        /// <param name="successResponse">The response content to include.</param>
        /// <returns>A success <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage CreateSuccessResponseMessage(string successResponse)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent(successResponse);
            responseMessage.Content = content;
            return responseMessage;
        }

        /// <summary>
        /// Creates a non-JSON HTTP 429 throttling response.
        /// </summary>
        /// <returns>A throttling <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage CreateTooManyRequestsNonJsonResponse()
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent(TooManyRequestsContent)
            };
            httpResponse.Headers.RetryAfter = new RetryConditionHeaderValue(TestRetryAfterDuration);

            return httpResponse;
        }

        /// <summary>
        /// Creates a PKeyAuth challenge response with a <c>WWW-Authenticate</c> header.
        /// </summary>
        /// <returns>An unauthorized <see cref="HttpResponseMessage"/> containing a PKeyAuth challenge.</returns>
        public static HttpResponseMessage CreatePKeyAuthChallengeResponse()
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(DefaultEmtpyFailureErrorMessage)
            };
            httpResponse.Headers.Add("WWW-Authenticate", @"PKeyAuth  Nonce=""nonce"",  Version=""1.0"", CertThumbprint=""thumbprint"",  Context=""context""");

            return httpResponse;
        }

        /// <summary>
        /// Creates a JSON HTTP 429 throttling response.
        /// </summary>
        /// <returns>A throttling <see cref="HttpResponseMessage"/>.</returns>
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

        /// <summary>
        /// Creates an OpenID configuration response for the specified authority.
        /// </summary>
        /// <param name="authority">The authority base URL.</param>
        /// <param name="qp">Optional query parameters to append to authorization and token endpoints.</param>
        /// <returns>A success <see cref="HttpResponseMessage"/> containing OpenID metadata.</returns>
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

        /// <summary>
        /// Creates an ADFS OpenID configuration response for the specified authority.
        /// </summary>
        /// <param name="authority">The ADFS authority base URL.</param>
        /// <param name="qp">Optional query parameters to append.</param>
        /// <returns>A success <see cref="HttpResponseMessage"/> containing ADFS OpenID metadata.</returns>
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

        /// <summary>
        /// Creates a mock handler for instance discovery responses.
        /// </summary>
        /// <param name="discoveryEndpoint">The expected discovery endpoint URL.</param>
        /// <param name="content">The discovery payload to return.</param>
        /// <returns>A configured <see cref="MockHttpMessageHandler"/>.</returns>
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

        /// <summary>
        /// Creates a mock broker/MSAL runtime token response.
        /// </summary>
        /// <param name="accessToken">The access token value to use, or <see langword="null"/> to use the default test token.</param>
        /// <param name="tokenType">The token type to use, or <see langword="null"/> to use Bearer.</param>
        /// <returns>A configured <see cref="MsalTokenResponse"/>.</returns>
        internal static MsalTokenResponse CreateMsalRunTimeBrokerTokenResponse(string accessToken = null, string tokenType = null)
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

        /// <summary>
        /// Creates a mock IMDS probe handler for v1 or v2 probing scenarios.
        /// </summary>
        /// <param name="imdsVersion">The IMDS version being probed.</param>
        /// <param name="userAssignedIdentityId">The user-assigned identity identifier type, if any.</param>
        /// <param name="userAssignedId">The user-assigned identity value, if any.</param>
        /// <param name="success">Whether to simulate a successful probe pattern.</param>
        /// <param name="retry">Whether to simulate a retryable failure when <paramref name="success"/> is false.</param>
        /// <returns>A configured <see cref="MockHttpMessageHandler"/>.</returns>
        internal static MockHttpMessageHandler MockImdsProbe(
            ImdsVersion imdsVersion,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null,
            bool success = true,
            bool retry = false)
        {
            string apiVersionQueryParam;
            string imdsApiVersion;
            string imdsEndpoint;

            switch (imdsVersion)
            {
                case ImdsVersion.V2:
                    apiVersionQueryParam = ImdsV2ManagedIdentitySource.ApiVersionQueryParam;
                    imdsApiVersion = ImdsV2ManagedIdentitySource.ImdsV2ApiVersion;
                    imdsEndpoint = ImdsV2ManagedIdentitySource.CsrMetadataPath;
                    break;

                case ImdsVersion.V1:
                    apiVersionQueryParam = ImdsManagedIdentitySource.ApiVersionQueryParam;
                    imdsApiVersion = ImdsManagedIdentitySource.ImdsApiVersion;
                    imdsEndpoint = ImdsManagedIdentitySource.ImdsTokenPath;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(imdsVersion), imdsVersion, null);
            }

            HttpStatusCode statusCode;

            if (success)
            {
                statusCode = HttpStatusCode.BadRequest; // IMDS probe success returns 400 Bad Request
            }
            else
            {
                if (retry)
                {
                    statusCode = HttpStatusCode.InternalServerError;
                }
                else
                {
                    statusCode = HttpStatusCode.NotFound;
                }
            }

            IDictionary<string, string> expectedQueryParams = new Dictionary<string, string>();
            IDictionary<string, string> expectedRequestHeaders = new Dictionary<string, string>();
            IList<string> presentRequestHeaders = new List<string>
                {
                    OAuth2Header.XMsCorrelationId
                };

            if (userAssignedIdentityId != UserAssignedIdentityId.None && userAssignedId != null)
            {
                var userAssignedIdQueryParam = ImdsManagedIdentitySource.GetUserAssignedIdQueryParam(
                    (ManagedIdentityIdType)userAssignedIdentityId, userAssignedId, null);
                expectedQueryParams.Add(userAssignedIdQueryParam.Value.Key, userAssignedIdQueryParam.Value.Value);
            }
            expectedQueryParams.Add(apiVersionQueryParam, imdsApiVersion);

            var handler = new MockHttpMessageHandler()
            {
                ExpectedUrl = $"{ImdsManagedIdentitySource.DefaultImdsBaseEndpoint}{imdsEndpoint}",
                ExpectedMethod = HttpMethod.Get,
                ExpectedQueryParams = expectedQueryParams,
                ExpectedRequestHeaders = expectedRequestHeaders,
                PresentRequestHeaders = presentRequestHeaders,
                ResponseMessage = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(""),
                }
            };

            return handler;
        }

        /// <summary>
        /// Creates a failed IMDS probe handler.
        /// </summary>
        /// <param name="imdsVersion">The IMDS version being probed.</param>
        /// <param name="userAssignedIdentityId">The user-assigned identity identifier type, if any.</param>
        /// <param name="userAssignedId">The user-assigned identity value, if any.</param>
        /// <param name="retry">Whether to simulate a retryable failure.</param>
        /// <returns>A configured <see cref="MockHttpMessageHandler"/>.</returns>
        internal static MockHttpMessageHandler MockImdsProbeFailure(
            ImdsVersion imdsVersion,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null,
            bool retry = false)
        {
            return MockImdsProbe(imdsVersion, userAssignedIdentityId, userAssignedId, success: false, retry: retry);
        }

        /// <summary>
        /// Creates a mock CSR metadata response handler for IMDS v2 flows.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <param name="responseServerHeader">The optional server header value to add.</param>
        /// <param name="userAssignedIdentityId">The user-assigned identity identifier type, if any.</param>
        /// <param name="userAssignedId">The user-assigned identity value, if any.</param>
        /// <param name="clientIdOverride">Optional client ID override to include in the payload.</param>
        /// <param name="tenantIdOverride">Optional tenant ID override to include in the payload.</param>
        /// <param name="attestationEndpointOverride">Optional attestation endpoint override to include in the payload.</param>
        /// <returns>A configured <see cref="MockHttpMessageHandler"/>.</returns>
        public static MockHttpMessageHandler MockCsrResponse(
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string responseServerHeader = "IMDS/150.870.65.1854",
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null,
            string clientIdOverride = null,
            string tenantIdOverride = null,
            string attestationEndpointOverride = null)
        {
            IDictionary<string, string> expectedQueryParams = new Dictionary<string, string>();
            IDictionary<string, string> expectedRequestHeaders = new Dictionary<string, string>();
            IList<string> presentRequestHeaders = new List<string>
                {
                    OAuth2Header.XMsCorrelationId
                };

            if (userAssignedIdentityId != UserAssignedIdentityId.None && userAssignedId != null)
            {
                var userAssignedIdQueryParam = ImdsManagedIdentitySource.GetUserAssignedIdQueryParam(
                    (ManagedIdentityIdType)userAssignedIdentityId, userAssignedId, null);
                expectedQueryParams.Add(userAssignedIdQueryParam.Value.Key, userAssignedIdQueryParam.Value.Value);
            }
            expectedQueryParams.Add("cred-api-version", "2.0");
            expectedRequestHeaders.Add("Metadata", "true");

            string content =
                "{" +
                "\"cuId\": { \"vmId\": \"fake_vmId\" }," +
                "\"clientId\": \"" + (clientIdOverride ?? TestConstants.ClientId) + "\"," +
                "\"tenantId\": \"" + (tenantIdOverride ?? TestConstants.TenantId) + "\"," +
                "\"attestationEndpoint\": \"" + (attestationEndpointOverride ?? "https://fake_attestation_endpoint") + "\"" +
                "}";

            var handler = new MockHttpMessageHandler()
            {
                ExpectedUrl = $"{ImdsManagedIdentitySource.DefaultImdsBaseEndpoint}{ImdsV2ManagedIdentitySource.CsrMetadataPath}",
                ExpectedMethod = HttpMethod.Get,
                ExpectedQueryParams = expectedQueryParams,
                ExpectedRequestHeaders = expectedRequestHeaders,
                PresentRequestHeaders = presentRequestHeaders,
                ResponseMessage = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content),
                }
            };

            if (responseServerHeader != null)
                handler.ResponseMessage.Headers.TryAddWithoutValidation("server", responseServerHeader);

            return handler;
        }

        /// <summary>
        /// Creates a failed CSR metadata response handler using HTTP 400.
        /// </summary>
        /// <returns>A configured <see cref="MockHttpMessageHandler"/>.</returns>
        public static MockHttpMessageHandler MockCsrResponseFailure()
        {
            // 400 doesn't trigger the retry policy
            return MockCsrResponse(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Creates a mock certificate issuance response handler for IMDS v2 flows.
        /// </summary>
        /// <param name="userAssignedIdentityId">The user-assigned identity identifier type, if any.</param>
        /// <param name="userAssignedId">The user-assigned identity value, if any.</param>
        /// <param name="certificate">The raw certificate payload to return.</param>
        /// <param name="clientIdOverride">Optional client ID override to include in the payload.</param>
        /// <param name="tenantIdOverride">Optional tenant ID override to include in the payload.</param>
        /// <param name="mtlsEndpointOverride">Optional mTLS endpoint override to include in the payload.</param>
        /// <returns>A configured <see cref="MockHttpMessageHandler"/>.</returns>
        public static MockHttpMessageHandler MockCertificateRequestResponse(
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null,
            string certificate = TestConstants.ValidRawCertificate,
            string clientIdOverride = null,
            string tenantIdOverride = null,
            string mtlsEndpointOverride = null)
        {
            IDictionary<string, string> expectedQueryParams = new Dictionary<string, string>();
            IDictionary<string, string> expectedRequestHeaders = new Dictionary<string, string>();
            IList<string> presentRequestHeaders = new List<string>
                {
                    OAuth2Header.XMsCorrelationId
                };

            if (userAssignedIdentityId != UserAssignedIdentityId.None && userAssignedId != null)
            {
                var userAssignedIdQueryParam = ImdsManagedIdentitySource.GetUserAssignedIdQueryParam(
                    (ManagedIdentityIdType)userAssignedIdentityId, userAssignedId, null);
                expectedQueryParams.Add(userAssignedIdQueryParam.Value.Key, userAssignedIdQueryParam.Value.Value);
            }
            expectedQueryParams.Add("cred-api-version", ImdsV2ManagedIdentitySource.ImdsV2ApiVersion);
            expectedRequestHeaders.Add("Metadata", "true");

            string content =
                "{" +
                "\"client_id\": \"" + (clientIdOverride ?? TestConstants.ClientId) + "\"," +
                "\"tenant_id\": \"" + (tenantIdOverride ?? TestConstants.TenantId) + "\"," +
                "\"certificate\": \"" + certificate + "\"," +
                "\"identity_type\": \"fake_identity_type\"," + // "SystemAssigned" or "UserAssigned" - not relevant in tests
                "\"mtls_authentication_endpoint\": \"" + (mtlsEndpointOverride ?? TestConstants.MtlsAuthenticationEndpoint) + "\"" +
                "}";

            var handler = new MockHttpMessageHandler()
            {
                ExpectedUrl = $"{ImdsManagedIdentitySource.DefaultImdsBaseEndpoint}{ImdsV2ManagedIdentitySource.CertificateRequestPath}",
                ExpectedMethod = HttpMethod.Post,
                ExpectedQueryParams = expectedQueryParams,
                ExpectedRequestHeaders = expectedRequestHeaders,
                PresentRequestHeaders = presentRequestHeaders,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content),
                }
            };

            return handler;
        }

        /// <summary>
        /// Creates a mock Entra token response handler for IMDS v2 token acquisition.
        /// </summary>
        /// <param name="identityLoggerAdapter">The logger adapter used to populate expected MSAL ID headers.</param>
        /// <returns>A configured <see cref="MockHttpMessageHandler"/>.</returns>
        internal static MockHttpMessageHandler MockImdsV2EntraTokenRequestResponse(
            IdentityLoggerAdapter identityLoggerAdapter)
        {
            IDictionary<string, string> expectedPostData = new Dictionary<string, string>();
            IDictionary<string, string> expectedRequestHeaders = new Dictionary<string, string>
                {
                    { ThrottleCommon.ThrottleRetryAfterHeaderName, ThrottleCommon.ThrottleRetryAfterHeaderValue }
                };
            IList<string> presentRequestHeaders = new List<string>
                {
                    OAuth2Header.XMsCorrelationId
                };

            var idParams = MsalIdHelper.GetMsalIdParameters(identityLoggerAdapter);
            foreach (var idParam in idParams)
            {
                expectedRequestHeaders[idParam.Key] = idParam.Value;
            }

            expectedPostData.Add("token_type", "mtls_pop");

            var handler = new MockHttpMessageHandler()
            {
                ExpectedUrl = $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = expectedPostData,
                ExpectedRequestHeaders = expectedRequestHeaders,
                PresentRequestHeaders = presentRequestHeaders,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GetMsiSuccessfulResponse(imdsV2: true)),
                }
            };

            return handler;
        }

        /// <summary>
        /// Creates a mock certificate issuance response handler that returns HTTP 400 because attestation was required but no attestation token was provided.
        /// </summary>
        /// <param name="userAssignedIdentityId">The user-assigned identity identifier type, if any.</param>
        /// <param name="userAssignedId">The user-assigned identity value, if any.</param>
        /// <returns>A configured <see cref="MockHttpMessageHandler"/>.</returns>
        internal static MockHttpMessageHandler MockCertificateRequestResponse_AttestationRequired_ButMissingToken_Returns400(
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null)
        {
            IDictionary<string, string> expectedQueryParams = new Dictionary<string, string>();
            IDictionary<string, string> expectedRequestHeaders = new Dictionary<string, string>();
            IList<string> presentRequestHeaders = new List<string>
            {
                OAuth2Header.XMsCorrelationId
            };

            if (userAssignedIdentityId != UserAssignedIdentityId.None && userAssignedId != null)
            {
                var userAssignedIdQueryParam = ImdsManagedIdentitySource.GetUserAssignedIdQueryParam(
                    (ManagedIdentityIdType)userAssignedIdentityId, userAssignedId, null);

                expectedQueryParams.Add(userAssignedIdQueryParam.Value.Key, userAssignedIdQueryParam.Value.Value);
            }

            expectedQueryParams.Add("cred-api-version", ImdsV2ManagedIdentitySource.ImdsV2ApiVersion);
            expectedRequestHeaders.Add("Metadata", "true");

            string content =
                "{\"error\":\"invalid_request\",\"error_description\":\"Attestation Token is missing / empty in the issue credential request\"}";

            return new MockHttpMessageHandler()
            {
                ExpectedUrl = $"{ImdsManagedIdentitySource.DefaultImdsBaseEndpoint}{ImdsV2ManagedIdentitySource.CertificateRequestPath}",
                ExpectedMethod = HttpMethod.Post,
                ExpectedQueryParams = expectedQueryParams,
                ExpectedRequestHeaders = expectedRequestHeaders,
                PresentRequestHeaders = presentRequestHeaders,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(content),
                }
            };
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

        internal static MockHttpMessageHandler MockImdsV2EntraTokenRequestResponseExpectClientId(
            IdentityLoggerAdapter identityLoggerAdapter,
            bool mTLSPop = false,
            string expectedClientId = TestConstants.ClientId)
        {
            IDictionary<string, string> expectedPostData = new Dictionary<string, string>();
            IDictionary<string, string> expectedRequestHeaders = new Dictionary<string, string>
        {
            { ThrottleCommon.ThrottleRetryAfterHeaderName, ThrottleCommon.ThrottleRetryAfterHeaderValue }
        };
            IList<string> presentRequestHeaders = new List<string>
        {
            OAuth2Header.XMsCorrelationId
        };

            var idParams = MsalIdHelper.GetMsalIdParameters(identityLoggerAdapter);
            foreach (var idParam in idParams)
            {
                expectedRequestHeaders[idParam.Key] = idParam.Value;
            }

            var tokenType = mTLSPop ? "mtls_pop" : "bearer";
            expectedPostData.Add("token_type", tokenType);
            expectedPostData.Add("client_id", expectedClientId); // <— assert canonical GUID

            return new MockHttpMessageHandler()
            {
                ExpectedUrl = $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = expectedPostData,
                ExpectedRequestHeaders = expectedRequestHeaders,
                PresentRequestHeaders = presentRequestHeaders,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GetMsiSuccessfulResponse(imdsV2: true)),
                }
            };
        }

        internal static void AddMocksToGetEntraTokenUsingCachedCert(
            MockHttpManager httpManager,
            IdentityLoggerAdapter identityLoggerAdapter,
            bool mTLSPop = false,
            bool assertClientId = false,
            string expectedClientId = TestConstants.ClientId,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null)
        {
            // cached-cert refresh still calls /getplatformmetadata (SAMI or UAMI flavor)
            if (userAssignedIdentityId != UserAssignedIdentityId.None && userAssignedId != null)
            {
                httpManager.AddMockHandler(
                    MockHelpers.MockCsrResponse(userAssignedIdentityId: userAssignedIdentityId, userAssignedId: userAssignedId));
            }
            else
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());
            }

            // Token request (no /issuecredential added here)
            if (assertClientId)
            {
                httpManager.AddMockHandler(
                    MockHelpers.MockImdsV2EntraTokenRequestResponseExpectClientId(identityLoggerAdapter, mTLSPop, expectedClientId));
            }
            else
            {
                httpManager.AddMockHandler(
                    MockHelpers.MockImdsV2EntraTokenRequestResponse(identityLoggerAdapter));
            }
        }

        internal static void AddMocks_AttestedCertMustNotBeReused_ExpectIssueCredential400(
            MockHttpManager httpManager,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null)
        {
            // Even on refresh, MSAL calls /getplatformmetadata (CSR metadata)
            if (userAssignedIdentityId != UserAssignedIdentityId.None && userAssignedId != null)
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(userAssignedIdentityId: userAssignedIdentityId, userAssignedId: userAssignedId));
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse_AttestationRequired_ButMissingToken_Returns400(userAssignedIdentityId, userAssignedId));
            }
            else
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse_AttestationRequired_ButMissingToken_Returns400());
            }

            // IMPORTANT: DO NOT add MockImdsV2EntraTokenRequestResponse here.
            // If MSAL incorrectly reuses the cert and calls /token, the test should fail,
            // and will pass after the product fix.
        }
    }
}

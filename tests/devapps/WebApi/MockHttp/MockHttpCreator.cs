// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Net;
using System.Net.Http;

namespace WebApi.MockHttp
{
    public static class MockHttpCreator
    {
        public static HttpResponseMessage CreateS2SBearerResponse(
            string secret = "header.payload.signature", int expiry = 86399)
        {

            int refreshIn = expiry / 2;
            return CreateSuccessResponseMessage(
                "{\"token_type\":\"Bearer\",\"expires_in\":\"" + expiry + "\",\"refresh_in\":\"" + refreshIn + "\",\"access_token\":\"" + secret + "\"}");
        }

        private static HttpResponseMessage CreateSuccessResponseMessage(string successResponse)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent(successResponse);
            responseMessage.Content = content;
            return responseMessage;
        }

        private static string CreateClientInfo(string uid = "my-uid", string utid = "my-utid")
        {
            return Base64UrlHelpers.Encode("{\"uid\":\"" + uid + "\",\"utid\":\"" + utid + "\"}");
        }

        public static HttpResponseMessage CreateInstanceDiscoveryMockHandler(
            string content = MockHttpValues.DiscoveryJsonResponse)
        {
            return CreateSuccessResponseMessage(content);
        }

        public static HttpResponseMessage CreateUserTokenResponse(
            string tenantId, 
            string scopes = "scope",
            string atSecret = "secret_at", 
            string uid = "uid", 
            string utid = "utid",
            int expiresIn = 3600)
        {

            string message = 
              "{\"token_type\":\"Bearer\",\"expires_in\":\"" + expiresIn + "\",\"scope\":" + "\"" + scopes + "\"" +
              ",\"access_token\":\"" + atSecret + "\"" +
              ",\"refresh_token\":\"secret_rt\",\"client_info\"" +
              ":\"" + CreateClientInfo(uid, utid) + "\",\"id_token\"" +
              ":\"" + CreateIdToken(tenantId, uid, $"user_{uid}") +
              "\",\"id_token_expires_in\":\"" + expiresIn + "\"}";

            return CreateSuccessResponseMessage(message);
        }

        private static string CreateIdToken(string tenantId, string uniqueId = "uid", string displayableId = "joe@contoso.com")
        {
            string id = "{\"aud\": \"e854a4a7-6c34-449c-b237-fc7a28093d84\"," +
                        "\"iss\": \"https://login.microsoftonline.com/6c3d51dd-f0e5-4959-b4ea-a80c4e36fe5e/v2.0/\"," +
                        "\"iat\": 1455833828," +
                        "\"nbf\": 1455833828," +
                        "\"exp\": 1455837728," +
                        "\"ipaddr\": \"131.107.159.117\"," +
                        "\"name\": \"Joe Don\"," +
                        "\"oid\": \"" + uniqueId + "\"," +
                        "\"preferred_username\": \"" + displayableId + "\"," +
                        "\"sub\": \"K4_SGGxKqW1SxUAmhg6C1F6VPiFzcx-Qd80ehIEdFus\"," +
                        "\"tid\": \"" + tenantId + "\"," +
                        "\"ver\": \"2.0\"}";
            return string.Format(CultureInfo.InvariantCulture, "someheader.{0}.somesignature", Base64UrlHelpers.Encode(id));
        }

    }

    public class MockHttpValues
    {
        public const string DiscoveryJsonResponse = @"{
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
                }";
    }

}


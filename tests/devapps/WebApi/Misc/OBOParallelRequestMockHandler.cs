using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Utils;

namespace WebApi.Controllers
{
    internal class OBOParallelRequestMockHandler : IHttpManager
    {
        public long LastRequestDurationInMs => Settings.NetworkAccessPenaltyMs;

        public async Task<HttpResponse> SendGetAsync(Uri endpoint, IDictionary<string, string> headers, ICoreLogger logger, bool retry = true, CancellationToken cancellationToken = default)
        {
            // simulate delay and also add complexity due to thread context switch
            await Task.Delay(Settings.NetworkAccessPenaltyMs).ConfigureAwait(false);

            if (endpoint.AbsoluteUri.StartsWith("https://login.microsoftonline.com/common/discovery/instance?api-version=1.1"))
            {
                return new HttpResponse()
                {
                    Body = DiscoveryJsonResponse,
                    StatusCode = System.Net.HttpStatusCode.OK
                };
            }

            throw new InvalidOperationException("Only instance discovery is supported");
        }

        private static Random s_random = new Random();
        public static string GetDefaultTokenResponse(string tenantId)
        {
            // add anywhere between 0 and 30 s to the expiration, just to emulate some of these tokens expiring
            int expirationBaseline = 5 * 60; // 5 min
            int seconds = s_random.Next(30);

            int totalExpiration = expirationBaseline + seconds;

            return
          "{\"token_type\":\"Bearer\",\"expires_in\":\"" + totalExpiration + "\",\"scope\":" +
          "\"scope\",\"access_token\":\"" + "secret_at" + "\"" +
          ",\"refresh_token\":\"secret_rt\",\"client_info\"" +
          ":\"" + CreateClientInfo() + "\",\"id_token\"" +
          ":\"" + CreateIdToken(tenantId) +
          "\",\"id_token_expires_in\":\"" + totalExpiration + "\"}";
        }

        public static string CreateIdToken(string tenantId, string uniqueId = "uid", string displayableId = "joe@contoso.com")
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

        private static string CreateClientInfo(string uid = "uid", string utid = "utid")
        {
            return Base64UrlHelpers.Encode("{\"uid\":\"" + uid + "\",\"utid\":\"" + utid + "\"}");
        }

        public async Task<HttpResponse> SendPostAsync(Uri endpoint, IDictionary<string, string> headers, IDictionary<string, string> bodyParameters, ICoreLogger logger, CancellationToken cancellationToken = default)
        {
            using (logger.LogBlockDuration("Test HttpManager - SendPostAsync"))
            {
                await Task.Delay(Settings.NetworkAccessPenaltyMs).ConfigureAwait(false);

                // example endpoint https://login.microsoftonline.com/tid2/oauth2/v2.0/token

                var regexp = @"https://login.microsoftonline.com/(?<tid>.*)/oauth2/v2.0/token"; // captures the tenantID
                var m = Regex.Match(endpoint.AbsoluteUri, regexp);
                var tid = m.Groups["tid"];

                if (tid != null)
                {
                    return new HttpResponse()
                    {
                        Body = GetDefaultTokenResponse(tid.Value),
                        StatusCode = System.Net.HttpStatusCode.OK
                    };
                }
                else
                {
                    throw new InvalidOperationException("Not expecting this /token request " + endpoint.AbsoluteUri);
                }
            }

        }
        public Task<HttpResponse> SendPostAsync(Uri endpoint, IDictionary<string, string> headers, HttpContent body, ICoreLogger logger, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponse> SendPostForceResponseAsync(Uri uri, Dictionary<string, string> headers, StringContent body, ICoreLogger logger, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private const string DiscoveryJsonResponse = @"{
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

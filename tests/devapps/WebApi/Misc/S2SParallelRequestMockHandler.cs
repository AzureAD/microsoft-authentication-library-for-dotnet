//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Net.Http;
//using System.Text.RegularExpressions;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Identity.Client.Core;
//using Microsoft.Identity.Client.Http;

//namespace WebApi.Controllers
//{
//    /// <summary>
//    /// This custom HttpManager does the following: 
//    /// - provides a standard reponse for discovery calls
//    /// - responds with valid tokens based on a naming convention (uid = "uid" + rtSecret, upn = "user_" + rtSecret)
//    /// </summary>
//    internal class S2SParallelRequestMockHandler : IHttpManager
//    {
//        public long LastRequestDurationInMs => Settings.NetworkAccessPenaltyMs;

//        public async Task<HttpResponse> SendGetAsync(Uri endpoint, IDictionary<string, string> headers, ICoreLogger logger, bool retry = true, CancellationToken cancellationToken = default)
//        {
//            // simulate delay and also add complexity due to thread context switch
//            await Task.Delay(Settings.NetworkAccessPenaltyMs).ConfigureAwait(false);

//            if (endpoint.AbsoluteUri.StartsWith("https://login.microsoftonline.com/common/discovery/instance?api-version=1.1"))
//            {
//                return new HttpResponse()
//                {
//                    Body = DiscoveryJsonResponse,
//                    StatusCode = System.Net.HttpStatusCode.OK
//                };
//            }

//            throw new InvalidOperationException("Only instance discovery is supported");
//        }

//        public async Task<HttpResponse> SendPostAsync(Uri endpoint, IDictionary<string, string> headers, IDictionary<string, string> bodyParameters, ICoreLogger logger, CancellationToken cancellationToken = default)
//        {
//            await Task.Delay(Settings.NetworkAccessPenaltyMs).ConfigureAwait(false);

//            // example endpoint https://login.microsoftonline.com/tid2/oauth2/v2.0/token

//            var regexp = @"https://login.microsoftonline.com/(?<tid>.*)/oauth2/v2.0/token"; // captures the tenantID
//            var m = Regex.Match(endpoint.AbsoluteUri, regexp);
//            var tid = m.Groups["tid"];

//            if (tid != null)
//            {
//                return new HttpResponse()
//                {
//                    Body = "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"" + tid + "\"}",
//                    StatusCode = System.Net.HttpStatusCode.OK
//                };
//            }
//            else
//            {
//                throw new InvalidOperationException("Not expecting this /token request " + endpoint.AbsoluteUri);
//            }

//        }
//        public Task<HttpResponse> SendPostAsync(Uri endpoint, IDictionary<string, string> headers, HttpContent body, ICoreLogger logger, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<HttpResponse> SendPostForceResponseAsync(Uri uri, Dictionary<string, string> headers, StringContent body, ICoreLogger logger, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        private const string DiscoveryJsonResponse = @"{
//                        ""tenant_discovery_endpoint"":""https://login.microsoftonline.com/tenant/.well-known/openid-configuration"",
//                        ""api-version"":""1.1"",
//                        ""metadata"":[
//                            {
//                            ""preferred_network"":""login.microsoftonline.com"",
//                            ""preferred_cache"":""login.windows.net"",
//                            ""aliases"":[
//                                ""login.microsoftonline.com"", 
//                                ""login.windows.net"",
//                                ""login.microsoft.com"",
//                                ""sts.windows.net""]},
//                            {
//                            ""preferred_network"":""login.partner.microsoftonline.cn"",
//                            ""preferred_cache"":""login.partner.microsoftonline.cn"",
//                            ""aliases"":[
//                                ""login.partner.microsoftonline.cn"",
//                                ""login.chinacloudapi.cn""]},
//                            {
//                            ""preferred_network"":""login.microsoftonline.de"",
//                            ""preferred_cache"":""login.microsoftonline.de"",
//                            ""aliases"":[
//                                    ""login.microsoftonline.de""]},
//                            {
//                            ""preferred_network"":""login.microsoftonline.us"",
//                            ""preferred_cache"":""login.microsoftonline.us"",
//                            ""aliases"":[
//                                ""login.microsoftonline.us"",
//                                ""login.usgovcloudapi.net""]},
//                            {
//                            ""preferred_network"":""login-us.microsoftonline.com"",
//                            ""preferred_cache"":""login-us.microsoftonline.com"",
//                            ""aliases"":[
//                                ""login-us.microsoftonline.com""]}
//                        ]
//                }";

//    }
//}

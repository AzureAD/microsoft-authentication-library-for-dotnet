//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class AuthenticationMetadata
    {
        private const string CustomTrustedHostEnvironmentVariableName = "customTrustedHost";
        private const string AuthorizeEndpointTemplate = "https://{host}/{tenant}/oauth2/authorize";
        private const string MetadataTemplate = "{\"Host\":\"{host}\", \"Authority\":\"https://{host}/{tenant}/\", \"InstanceDiscoveryEndpoint\":\"https://{host}/common/discovery/instance\", \"AuthorizeEndpoint\":\"" + AuthorizeEndpointTemplate + "\", \"TokenEndpoint\":\"https://{host}/{tenant}/oauth2/token\", \"UserRealmEndpoint\":\"https://{host}/common/UserRealm\"}";

        static AuthenticationMetadata()
        {
            string[] trustedHostList = { "login.windows.net", "login.chinacloudapi.cn", "login.cloudgovapi.us" };

            AuthorityList = new List<ActiveDirectoryAuthenticationAuthority>();

            string customAuthorityHost = PlatformSpecificHelper.GetEnvironmentVariable(CustomTrustedHostEnvironmentVariableName);
            if (string.IsNullOrWhiteSpace(customAuthorityHost))
            {
                foreach (string host in trustedHostList)
                {
                    AuthorityList.Add(CreateActiveDirectoryAuthenticationAuthority(host));
                }
            }
            else
            {
                AuthorityList.Add(CreateActiveDirectoryAuthenticationAuthority(customAuthorityHost));
            }
        }

        public static List<ActiveDirectoryAuthenticationAuthority> AuthorityList { get; private set; }

        public static ActiveDirectoryAuthenticationAuthority CreateActiveDirectoryAuthenticationAuthority(string host)
        {
            string metadata = MetadataTemplate.Replace("{host}", host);
            var serializer = new DataContractJsonSerializer(typeof(ActiveDirectoryAuthenticationAuthority));
            byte[] serializedObjectBytes = Encoding.UTF8.GetBytes(metadata);
            ActiveDirectoryAuthenticationAuthority authority;
            using (var stream = new MemoryStream(serializedObjectBytes))
            {
                authority = (ActiveDirectoryAuthenticationAuthority)serializer.ReadObject(stream);
                authority.Issuer = authority.TokenEndpoint;
            }

            return authority;
        }

        public static async Task<ActiveDirectoryAuthenticationAuthority> FindMatchingAuthorityAsync(string authority, string tenant, CallState callState)
        {
            ActiveDirectoryAuthenticationAuthority matchingAuthority = AuthorityList.FirstOrDefault(a => string.Compare(authority, a.Host, StringComparison.OrdinalIgnoreCase) == 0);
            if (matchingAuthority == null)
            {
                // We only check with the first trusted authority (login.windows.net) for instance discovery
                if (await FetchAuthenticatorAsync(AuthorityList.First().InstanceDiscoveryEndpoint, authority, tenant, callState) != null)
                {
                    matchingAuthority = CreateActiveDirectoryAuthenticationAuthority(authority);
                }
            }

            return matchingAuthority;
        }

        private static async Task<string> FetchAuthenticatorAsync(string instanceDiscoveryEndpoint, string host, string tenant, CallState callState)
        {
            instanceDiscoveryEndpoint += ("?api-version=1.0&authorization_endpoint=" + AuthorizeEndpointTemplate);
            instanceDiscoveryEndpoint = instanceDiscoveryEndpoint.Replace("{host}", host);
            instanceDiscoveryEndpoint = instanceDiscoveryEndpoint.Replace("{tenant}", tenant);

            instanceDiscoveryEndpoint = HttpHelper.CheckForExtraQueryParameter(instanceDiscoveryEndpoint);

            ClientMetrics clientMetrics = new ClientMetrics();

            try
            {
                IHttpWebRequest request = NetworkPlugin.HttpWebRequestFactory.Create(instanceDiscoveryEndpoint);
                request.Method = "GET";
                HttpHelper.AddCorrelationIdHeadersToRequest(request, callState);
                AdalIdHelper.AddAsHeaders(request);

                clientMetrics.BeginClientMetricsRecord(request, callState);

                using (var response = await request.GetResponseSyncOrAsync(callState))
                {
                    HttpHelper.VerifyCorrelationIdHeaderInReponse(response, callState);
                    InstanceDiscoveryResponse discoveryResponse = HttpHelper.DeserializeResponse<InstanceDiscoveryResponse>(response);
                    clientMetrics.SetLastError(null); 
                    return discoveryResponse.TenantDiscoveryEndpoint;
                }
            }
            catch (WebException ex)
            {
                TokenResponse tokenResponse = OAuth2Response.ReadErrorResponse(ex.Response);
                clientMetrics.SetLastError(tokenResponse.ErrorCodes);
                throw new AdalServiceException(
                    AdalError.AuthorityNotInValidList,
                    string.Format(CultureInfo.InvariantCulture, "{0}. {1} ({2}): {3}", 
                        AdalErrorMessage.AuthorityNotInValidList, tokenResponse.Error, host, tokenResponse.ErrorDescription), 
                    ex);
            }
            finally
            {
                clientMetrics.EndClientMetricsRecord(ClientMetricsEndpointType.InstanceDiscovery, callState);
            }
        }

        [DataContract]
        internal sealed class InstanceDiscoveryResponse
        {
            [DataMember(Name = "tenant_discovery_endpoint")]
            public string TenantDiscoveryEndpoint { get; set; }
        }
    }
}

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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal enum AuthorityType
    {
        Unknown = 0,
        AAD = 3,
        ADFS = 4
    }

    internal class Authenticator
    {
        public AuthorityType AuthorityType { get; set; }
        
        public bool IsTenantless { get; set; }
        
        public string AuthorizationUri { get; set; }
        
        public string TokenUri { get; set; }

        public string UserRealmUri { get; set; }

        public string SelfSignedJwtAudience { get; set; }
    }

    internal static class AuthenticationMetadata
    {
        private const string CustomTrustedHostEnvironmentVariableName = "customTrustedHost";
        private const string AuthorizeEndpointTemplate = "https://{host}/{tenant}/oauth2/authorize";
        private const string MetadataTemplate = "{\"Host\":\"{host}\", \"Authority\":\"https://{host}/{tenant}/\", \"InstanceDiscoveryEndpoint\":\"https://{host}/common/discovery/instance\", \"AuthorizeEndpoint\":\"" + AuthorizeEndpointTemplate + "\", \"TokenEndpoint\":\"https://{host}/{tenant}/oauth2/token\", \"UserRealmEndpoint\":\"https://{host}/common/UserRealm\"}";
        private const string TenantlessTenantName = "Common";

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

        public static async Task<Authenticator> CreateAuthenticatorAsync(bool validateAuthority, string authority, CallState callState, AuthorityType authorityType = AuthorityType.Unknown)
        {
            if (authorityType == AuthorityType.Unknown)
            {
                authorityType = DetectAuthorityType(authority);
            }

            if (authorityType != AuthorityType.AAD && validateAuthority)
            {
                throw new ArgumentException(ActiveDirectoryAuthenticationErrorMessage.UnsupportedAuthorityValidation, "validateAuthority");
            }

            Authenticator authenticator = await GetAuthenticatorAsync(authority, authorityType, validateAuthority, callState);

            if (authenticator == null)
            {
                throw new ArgumentException(ActiveDirectoryAuthenticationErrorMessage.AuthorityNotInValidList, "authority");
            }

            return authenticator;
        }

        public static string CanonicalizeUri(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri) && !uri.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri + "/";
            }

            return uri;
        }

        public static AuthorityType DetectAuthorityType(string authority)
        {
            if (string.IsNullOrWhiteSpace(authority))
            {
                throw new ArgumentNullException("authority");
            }

            if (!Uri.IsWellFormedUriString(authority, UriKind.Absolute))
            {
                throw new ArgumentException(
                    ActiveDirectoryAuthenticationErrorMessage.AuthorityInvalidUriFormat, "authority");
            }

            var authorityUri = new Uri(authority);
            if (authorityUri.Scheme != "https")
            {
                throw new ArgumentException(ActiveDirectoryAuthenticationErrorMessage.AuthorityUriInsecure, "authority");
            }

            string path = authorityUri.AbsolutePath.Substring(1);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(ActiveDirectoryAuthenticationErrorMessage.AuthorityUriInvalidPath, "authority");
            }

            string firstPath = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));
            AuthorityType authorityType = IsAdfsAuthority(firstPath) ? AuthorityType.ADFS : AuthorityType.AAD;

            return authorityType;
        }

        public static string ReplaceTenantlessTenant(string authority, string tenantId)
        {
            var regex = new Regex(Regex.Escape(TenantlessTenantName), RegexOptions.IgnoreCase);
            return regex.Replace(authority, tenantId, 1);
        }

        private static ActiveDirectoryAuthenticationAuthority CreateActiveDirectoryAuthenticationAuthority(string host)
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

        private static bool IsAdfsAuthority(string firstPath)
        {
            return string.Compare(firstPath, "adfs", StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static async Task<Authenticator> GetAuthenticatorAsync(string authority, AuthorityType authorityType, bool validateAuthority, CallState callState)
        {
            var authorityUri = new Uri(authority);
            string host = authorityUri.Authority;
            string path = authorityUri.AbsolutePath.Substring(1);
            string tenant = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));

            ActiveDirectoryAuthenticationAuthority matchingAuthority = (validateAuthority) ? await FindMatchingAuthorityAsync(host, tenant, callState) : CreateActiveDirectoryAuthenticationAuthority(host);

            return new Authenticator
                {
                    AuthorityType = authorityType,
                    AuthorizationUri = matchingAuthority.AuthorizeEndpoint.Replace("{tenant}", tenant),
                    TokenUri = matchingAuthority.TokenEndpoint.Replace("{tenant}", tenant),
                    UserRealmUri = CanonicalizeUri(matchingAuthority.UserRealmEndpoint),
                    IsTenantless = (string.Compare(tenant, TenantlessTenantName, StringComparison.OrdinalIgnoreCase) == 0),
                    SelfSignedJwtAudience = matchingAuthority.Issuer.Replace("{tenant}", tenant)
                };
        }

        private static async Task<ActiveDirectoryAuthenticationAuthority> FindMatchingAuthorityAsync(string authority, string tenant, CallState callState)
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

            try
            {
                IHttpWebRequest request = HttpWebRequestFactory.Create(instanceDiscoveryEndpoint);
                request.Method = "GET";
                HttpHelper.AddCorrelationIdHeadersToRequest(request, callState);
                AdalIdHelper.AddAsHeaders(request);

                using (var response = await request.GetResponseSyncOrAsync(callState))
                {
                    HttpHelper.VerifyCorrelationIdHeaderInReponse(response, callState);
                    InstanceDiscoveryResponse discoveryResponse = HttpHelper.DeserializeResponse<InstanceDiscoveryResponse>(response);
                    return discoveryResponse.TenantDiscoveryEndpoint;
                }
            }
            catch (WebException ex)
            {
                TokenResponse tokenResponse = OAuth2Response.ReadErrorResponse(ex.Response);
                throw new ActiveDirectoryAuthenticationException(
                    ActiveDirectoryAuthenticationError.AuthorityNotInValidList,
                    string.Format(CultureInfo.InvariantCulture, "{0}. {1} ({2}): {3}", 
                        ActiveDirectoryAuthenticationErrorMessage.AuthorityNotInValidList, tokenResponse.Error, host, tokenResponse.ErrorDescription), 
                    ex);
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

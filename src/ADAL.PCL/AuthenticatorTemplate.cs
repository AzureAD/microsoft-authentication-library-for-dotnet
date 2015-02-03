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

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [DataContract]
    internal class AuthenticatorTemplate
    {
        private const string AuthorizeEndpointTemplate = "https://{host}/{tenant}/oauth2/authorize";
        private const string MetadataTemplate = "{\"Host\":\"{host}\", \"Authority\":\"https://{host}/{tenant}/\", \"InstanceDiscoveryEndpoint\":\"https://{host}/common/discovery/instance\", \"AuthorizeEndpoint\":\"" + AuthorizeEndpointTemplate + "\", \"TokenEndpoint\":\"https://{host}/{tenant}/oauth2/token\", \"UserRealmEndpoint\":\"https://{host}/common/UserRealm\"}";

        public static AuthenticatorTemplate CreateFromHost(string host)
        {
            string metadata = MetadataTemplate.Replace("{host}", host);
            var serializer = new DataContractJsonSerializer(typeof(AuthenticatorTemplate));
            byte[] serializedObjectBytes = Encoding.UTF8.GetBytes(metadata);
            AuthenticatorTemplate authority;
            using (var stream = new MemoryStream(serializedObjectBytes))
            {
                authority = (AuthenticatorTemplate)serializer.ReadObject(stream);
                authority.Issuer = authority.TokenEndpoint;
            }

            return authority;
        }

        [DataMember]
        public string Host { get; internal set; }

        [DataMember]
        public string Issuer { get; internal set; }

        [DataMember]
        public string Authority { get; internal set; }

        [DataMember]
        public string InstanceDiscoveryEndpoint { get; internal set; }

        [DataMember]
        public string AuthorizeEndpoint { get; internal set; }

        [DataMember]
        public string TokenEndpoint { get; internal set; }

        [DataMember]
        public string UserRealmEndpoint { get; internal set; }

        public async Task VerifyAnotherHostByInstanceDiscoveryAsync(string host, string tenant, CallState callState)
        {
            string instanceDiscoveryEndpoint = this.InstanceDiscoveryEndpoint;
            instanceDiscoveryEndpoint += ("?api-version=1.0&authorization_endpoint=" + AuthorizeEndpointTemplate);
            instanceDiscoveryEndpoint = instanceDiscoveryEndpoint.Replace("{host}", host);
            instanceDiscoveryEndpoint = instanceDiscoveryEndpoint.Replace("{tenant}", tenant);

            try
            {
                var client = new AdalHttpClient(instanceDiscoveryEndpoint, callState);
                InstanceDiscoveryResponse discoveryResponse = await client.GetResponseAsync<InstanceDiscoveryResponse>(ClientMetricsEndpointType.InstanceDiscovery);

                if (discoveryResponse.TenantDiscoveryEndpoint == null)
                {
                    throw new AdalException(AdalError.AuthorityNotInValidList);
                }
            }
            catch (AdalServiceException ex)
            {
                throw new AdalException((ex.ErrorCode == "invalid_instance") ? AdalError.AuthorityNotInValidList : AdalError.AuthorityValidationFailed, ex);
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
//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

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
        private const string DeviceCodeEndpointTemplate = "https://{host}/{tenant}/oauth2/devicecode";
        private const string MetadataTemplate = "{\"Host\":\"{host}\", \"Authority\":\"https://{host}/{tenant}/\", \"InstanceDiscoveryEndpoint\":\"https://{host}/common/discovery/instance\", \"DeviceCodeEndpoint\":\"" + DeviceCodeEndpointTemplate + "\", \"AuthorizeEndpoint\":\"" + AuthorizeEndpointTemplate + "\", \"TokenEndpoint\":\"https://{host}/{tenant}/oauth2/token\", \"UserRealmEndpoint\":\"https://{host}/common/UserRealm\"}";

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
        public string DeviceCodeEndpoint { get; internal set; }

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
                InstanceDiscoveryResponse discoveryResponse = await client.GetResponseAsync<InstanceDiscoveryResponse>().ConfigureAwait(false);

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

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

using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [DataContract]
    internal sealed class UserRealmDiscoveryResponse
    {
        [DataMember(Name = "ver")]
        public string Version { get; set; }

        [DataMember(Name = "account_type")]
        public string AccountType { get; set; }

        [DataMember(Name = "federation_protocol")]
        public string FederationProtocol { get; set; }

        [DataMember(Name = "federation_metadata_url")]
        public string FederationMetadataUrl { get; set; }

        [DataMember(Name = "federation_active_auth_url")]
        public string FederationActiveAuthUrl { get; set; }

        internal static async Task<UserRealmDiscoveryResponse> CreateByDiscoveryAsync(string userRealmUri, string userName, CallState callState)
        {
            string userRealmEndpoint = userRealmUri;
            userRealmEndpoint += (userName + "?api-version=1.0");

            userRealmEndpoint = HttpHelper.CheckForExtraQueryParameter(userRealmEndpoint);
            Logger.Information(callState, "Sending user realm discovery request to '{0}'", userRealmEndpoint);

            UserRealmDiscoveryResponse userRealmResponse;
            ClientMetrics clientMetrics = new ClientMetrics();

            try
            {
                IHttpWebRequest request = NetworkPlugin.HttpWebRequestFactory.Create(userRealmEndpoint);
                request.Method = "GET";
                request.Accept = "application/json";
                HttpHelper.AddCorrelationIdHeadersToRequest(request, callState);
                AdalIdHelper.AddAsHeaders(request);

                clientMetrics.BeginClientMetricsRecord(request, callState);

                using (var response = await request.GetResponseSyncOrAsync(callState))
                {
                    HttpHelper.VerifyCorrelationIdHeaderInReponse(response, callState);
                    userRealmResponse = HttpHelper.DeserializeResponse<UserRealmDiscoveryResponse>(response);
                    clientMetrics.SetLastError(null);
                }
            }
            catch (WebException ex)
            {
                var serviceException = new AdalServiceException(AdalError.UserRealmDiscoveryFailed, ex);
                clientMetrics.SetLastError(new[] { serviceException.StatusCode.ToString() });
                throw serviceException;
            }
            finally
            {
                clientMetrics.EndClientMetricsRecord(ClientMetricsEndpointType.UserRealmDiscovery, callState);
            }

            return userRealmResponse;
        }
    }
}
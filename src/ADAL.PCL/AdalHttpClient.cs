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

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class AdalHttpClient
    {
        public AdalHttpClient(string uri, CallState callState)
        {
            this.Client = PlatformPlugin.HttpClientFactory.Create(CheckForExtraQueryParameter(uri), callState);
            this.CallState = callState;
        }

        public IHttpClient Client { get; private set; }

        public CallState CallState { get; private set; }

        public async Task<T> GetResponseAsync<T>(string endpointType)
        {
            T typedResponse;
            ClientMetrics clientMetrics = new ClientMetrics();

            try
            {
                clientMetrics.BeginClientMetricsRecord(this.CallState);

                if (PlatformPlugin.HttpClientFactory.AddAdditionalHeaders)
                {
                    Dictionary<string, string> clientMetricsHeaders = clientMetrics.GetPreviousRequestRecord(this.CallState);
                    foreach (KeyValuePair<string, string> kvp in clientMetricsHeaders)
                    {
                        this.Client.Headers[kvp.Key] = kvp.Value;
                    }

                    IDictionary<string, string> adalIdHeaders = AdalIdHelper.GetAdalIdParameters();
                    foreach (KeyValuePair<string, string> kvp in adalIdHeaders)
                    {
                        this.Client.Headers[kvp.Key] = kvp.Value;
                    }
                }

                using (IHttpWebResponse response = await this.Client.GetResponseAsync())
                {
                    typedResponse = DeserializeResponse<T>(response.ResponseStream);
                    clientMetrics.SetLastError(null);
                }
            }
            catch (HttpRequestWrapperException ex)
            {
                AdalServiceException serviceEx;
                if (ex.WebResponse != null)
                {
                    TokenResponse tokenResponse = TokenResponse.CreateFromErrorResponse(ex.WebResponse);
                    string[] errorCodes = tokenResponse.ErrorCodes ?? new[] { ex.WebResponse.StatusCode.ToString() };
                    serviceEx = new AdalServiceException(tokenResponse.Error, tokenResponse.ErrorDescription, errorCodes, ex);
                }
                else
                {
                    serviceEx = new AdalServiceException(AdalError.Unknown, ex);                                        
                }

                clientMetrics.SetLastError(serviceEx.ServiceErrorCodes);
                PlatformPlugin.Logger.Error(CallState, serviceEx);
                throw serviceEx;
            }
            finally
            {
                clientMetrics.EndClientMetricsRecord(endpointType, this.CallState);
            }

            return typedResponse;
        }

        private static T DeserializeResponse<T>(Stream responseStream)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));

            if (responseStream == null)
            {
                return default(T);
            }

            using (Stream stream = responseStream)
            {
                return ((T)serializer.ReadObject(stream));
            }
        }

        private static string CheckForExtraQueryParameter(string url)
        {
            string extraQueryParameter = PlatformPlugin.PlatformInformation.GetEnvironmentVariable("ExtraQueryParameter");
            string delimiter = (url.IndexOf('?') > 0) ? "&" : "?";
            if (!string.IsNullOrWhiteSpace(extraQueryParameter))
            {
                url += string.Concat(delimiter, extraQueryParameter);
            }

            return url;
        }
    }
}

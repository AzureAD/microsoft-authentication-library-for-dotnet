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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class HttpHelper
    {

        public static async Task<T> SendPostRequestAndDeserializeJsonResponseAsync<T>(string uri, RequestParameters requestParameters, CallState callState)
        {
            ClientMetrics clientMetrics = new ClientMetrics();

            try
            {
                IHttpWebRequest request = NetworkPlugin.HttpWebRequestFactory.Create(uri);
                request.ContentType = "application/x-www-form-urlencoded";
                AddCorrelationIdHeadersToRequest(request, callState);
                AdalIdHelper.AddAsHeaders(request);

                clientMetrics.BeginClientMetricsRecord(request, callState);

                SetPostRequest(request, requestParameters, callState);
                using (IHttpWebResponse response = await request.GetResponseSyncOrAsync(callState))
                {
                    VerifyCorrelationIdHeaderInReponse(response, callState);
                    clientMetrics.SetLastError(null);
                    return DeserializeResponse<T>(response);
                }
            }
            catch (WebException ex)
            {
                TokenResponse tokenResponse = OAuth2Response.ReadErrorResponse(ex.Response);
                clientMetrics.SetLastError(tokenResponse != null ? tokenResponse.ErrorCodes : null);
                throw new AdalServiceException(tokenResponse.Error, tokenResponse.ErrorDescription, tokenResponse.ErrorCodes, ex);
            }
            finally
            {
                clientMetrics.EndClientMetricsRecord(ClientMetricsEndpointType.Token, callState);
            }
        }

        public static void SetPostRequest(IHttpWebRequest request, RequestParameters requestParameters, CallState callState, Dictionary<string, string> headers = null)
        {
            request.Method = "POST";

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    request.Headers[kvp.Key] = kvp.Value;
                }
            }

            request.BodyParameters = requestParameters;
        }

        public static T DeserializeResponse<T>(IHttpWebResponse response)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));

            Stream responseStream = response.GetResponseStream();

            if (responseStream == null)
            {
                return default(T);
            }

            using (Stream stream = responseStream)
            {
                return ((T)serializer.ReadObject(stream));
            }
        }

        public static string ReadStreamContent(Stream stream)
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }

        public static string CheckForExtraQueryParameter(string url)
        {
            string extraQueryParameter = PlatformSpecificHelper.GetEnvironmentVariable("ExtraQueryParameter");
            string delimiter = (url.IndexOf('?') > 0) ? "&" : "?";
            if (!string.IsNullOrWhiteSpace(extraQueryParameter))
            {
                url += string.Concat(delimiter, extraQueryParameter);
            }

            return url;
        }

        public static void AddCorrelationIdHeadersToRequest(IHttpWebRequest request, CallState callState)
        {
            if (callState == null || callState.CorrelationId == Guid.Empty)
            {
                return;
            }

            Dictionary<string, string> headers = new Dictionary<string, string>
                                                 {
                                                     { OAuthHeader.CorrelationId, callState.CorrelationId.ToString() },
                                                     { OAuthHeader.RequestCorrelationIdInResponse, "true" }
                                                 };

            AddHeadersToRequest(request, headers);
        }

        public static void VerifyCorrelationIdHeaderInReponse(IHttpWebResponse response, CallState callState)
        {
            if (callState == null || callState.CorrelationId == Guid.Empty)
            {
                return;
            }

            WebHeaderCollection headers = response.Headers;
            foreach (string reponseHeaderKey in headers.AllKeys)
            {
                string trimmedKey = reponseHeaderKey.Trim();
                if (string.Compare(trimmedKey, OAuthHeader.CorrelationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string correlationIdHeader = headers[trimmedKey].Trim();
                    Guid correlationIdInResponse;
                    if (!Guid.TryParse(correlationIdHeader, out correlationIdInResponse))
                    {
                        Logger.Warning(callState, "Returned correlation id '{0}' is not in GUID format.", correlationIdHeader);
                    }
                    else if (correlationIdInResponse != callState.CorrelationId)
                    {
                        Logger.Warning(callState, "Returned correlation id '{0}' does not match the sent correlation id '{1}'", correlationIdHeader, callState.CorrelationId);
                    }

                    break;
                }
            }
        }

        public static void AddHeadersToRequest(IHttpWebRequest request, Dictionary<string, string> headers)
        {
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    request.Headers[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}

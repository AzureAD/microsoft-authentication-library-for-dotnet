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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Http;

namespace Microsoft.Identity.Client.Internal.OAuth2
{
    internal class OAuth2Client
    {
        private readonly Dictionary<string, string> _bodyParameters = new Dictionary<string, string>();

        private readonly Dictionary<string, string> _headers =
            new Dictionary<string, string>(MsalIdHelper.GetMsalIdParameters());

        private readonly Dictionary<string, string> _queryParameters = new Dictionary<string, string>();

        public void AddQueryParameter(string key, string value)
        {
            _queryParameters[key] = value;
        }

        public void AddHeader(string key, string value)
        {
            _headers[key] = value;
        }

        public void AddBodyParameter(string key, string value)
        {
            _bodyParameters[EncodingHelper.UrlEncode(key)] = EncodingHelper.UrlEncode(value);
        }

        public async Task<InstanceDiscoveryResponse> DoAuthorityValidation(Uri endPoint, CallState callState)
        {
            bool addCorrelationId = (callState != null && callState.CorrelationId != Guid.Empty);
            if (addCorrelationId)
            {
                _headers.Add(OAuth2Header.CorrelationId, callState.CorrelationId.ToString());
                _headers.Add(OAuth2Header.RequestCorrelationIdInResponse, "true");
            }

            MsalHttpResponse response =
                await
                    MsalHttpRequest.SendGet(CreateFullEndpointUri(endPoint), this._headers, callState);
            return CreateResponse<InstanceDiscoveryResponse>(response, callState, addCorrelationId);
        }

        public async Task<TokenResponse> GetToken(Uri endPoint, CallState callState)
        {
            bool addCorrelationId = (callState != null && callState.CorrelationId != Guid.Empty);
            if (addCorrelationId)
            {
                _headers.Add(OAuth2Header.CorrelationId, callState.CorrelationId.ToString());
                _headers.Add(OAuth2Header.RequestCorrelationIdInResponse, "true");
            }

            MsalHttpResponse response =
                await
                    MsalHttpRequest.SendPost(CreateFullEndpointUri(endPoint), this._headers, this._bodyParameters,
                        callState);
            return CreateResponse<TokenResponse>(response, callState, addCorrelationId);
        }


        private T CreateResponse<T>(MsalHttpResponse response, CallState callState, bool addCorrelationId)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                CreateErrorResponse(response, callState);
            }

            if (addCorrelationId)
            {
                VerifyCorrelationIdHeaderInReponse(response.Headers, callState);
            }

            return DeserializeResponse<T>(response.Body);
        }

        private void CreateErrorResponse(MsalHttpResponse response, CallState callState)
        {
            MsalServiceException serviceEx;
            try
            {
                TokenResponse tokenResponse = DeserializeResponse<TokenResponse>(response.Body);
                serviceEx = new MsalServiceException(tokenResponse.Error, tokenResponse.ErrorDescription);
            }
            catch (SerializationException)
            {
                serviceEx = new MsalServiceException(MsalError.Unknown, response.Body);
            }

            PlatformPlugin.Logger.Error(callState, serviceEx);
            throw serviceEx;
        }

        internal Uri CreateFullEndpointUri(Uri endPoint)
        {
            UriBuilder endpointUri = new UriBuilder(endPoint);
            string extraQp = _queryParameters.ToQueryParameter();
            if (endpointUri.Query.Length > 0)
            {
                extraQp = "&" + extraQp;
            }

            endpointUri.Query += extraQp;

            return endpointUri.Uri;
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

        private T DeserializeResponse<T>(string response)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof (T));

            if (response == null)
            {
                return default(T);
            }

            using (Stream stream = GenerateStreamFromString(response))
            {
                return ((T) serializer.ReadObject(stream));
            }
        }

        public Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private void VerifyCorrelationIdHeaderInReponse(Dictionary<string, string> headers, CallState callState)
        {
            foreach (string reponseHeaderKey in headers.Keys)
            {
                string trimmedKey = reponseHeaderKey.Trim();
                if (string.Compare(trimmedKey, OAuth2Header.CorrelationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string correlationIdHeader = headers[trimmedKey].Trim();
                    Guid correlationIdInResponse;
                    if (!Guid.TryParse(correlationIdHeader, out correlationIdInResponse))
                    {
                        PlatformPlugin.Logger.Warning(callState,
                            string.Format(CultureInfo.InvariantCulture,
                                "Returned correlation id '{0}' is not in GUID format.", correlationIdHeader));
                    }
                    else if (correlationIdInResponse != callState.CorrelationId)
                    {
                        PlatformPlugin.Logger.Warning(
                            callState,
                            string.Format(CultureInfo.InvariantCulture,
                                "Returned correlation id '{0}' does not match the sent correlation id '{1}'",
                                correlationIdHeader, callState.CorrelationId));
                    }

                    break;
                }
            }
        }


    }
}
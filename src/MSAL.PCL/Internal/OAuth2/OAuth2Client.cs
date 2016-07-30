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
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using Microsoft.Identity.Client.Internal.Http;

namespace Microsoft.Identity.Client.Internal.OAuth2
{
    internal class OAuth2Client
    {
        private readonly Dictionary<string, string> BodyParameters = new Dictionary<string, string>();

        public readonly Dictionary<string, string> Headers =
            new Dictionary<string, string>(MsalIdHelper.GetMsalIdParameters());

        public readonly Dictionary<string, string> QueryParameters = new Dictionary<string, string>();

        public void AddQueryParameter(string key, string value)
        {
            QueryParameters[key] = value;
        }

        public void AddHeader(string key, string value)
        {
            Headers[key] = value;
        }

        public void AddBodyParameter(string key, string value)
        {
            BodyParameters[EncodingHelper.UrlEncode(key)] = EncodingHelper.UrlEncode(value);
        }

        public InstanceDiscoveryResponse DoAuthorityValidation(Uri endPoint, CallState callstate)
        {
            return null;
        }

        public TokenResponse GetToken(Uri endPoint, CallState callState)
        {
            UriBuilder endpointUri = new UriBuilder(endPoint);
            foreach (var VARIABLE in QueryParameters.ToQueryParameter())
            {
                
            }

            try
            {
                bool addCorrelationId = (callState != null && callState.CorrelationId != Guid.Empty);
                if (addCorrelationId)
                {
                    Headers.Add(OAuth2Header.CorrelationId, callState.CorrelationId.ToString());
                    Headers.Add(OAuth2Header.RequestCorrelationIdInResponse, "true");
                }

                MsalHttpResponse response = MsalHttpRequest.SendPost(endpointUri., this.Headers, this.BodyParameters, callState);

                if (addCorrelationId)
                {
                    VerifyCorrelationIdHeaderInReponse(response.Headers);
                }
            }
            catch (HttpRequestWrapperException ex)
            {
                PlatformPlugin.Logger.Error(callState, ex);
                MsalServiceException serviceEx;
                if (ex.WebResponse != null)
                {
                    TokenResponse tokenResponse = TokenResponse.CreateFromErrorResponse(ex.WebResponse);
                    string[] errorCodes = tokenResponse.ErrorCodes ?? new[] {ex.WebResponse.StatusCode.ToString()};
                    serviceEx = new MsalServiceException(tokenResponse.Error, tokenResponse.ErrorDescription,
                        errorCodes, ex);
                }
                else
                {
                    serviceEx = new MsalServiceException(MsalError.Unknown, ex);
                }

                PlatformPlugin.Logger.Error(CallState, serviceEx);
                throw serviceEx;
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

        private static T DeserializeResponse<T>(Stream responseStream)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof (T));

            if (responseStream == null)
            {
                return default(T);
            }

            using (Stream stream = responseStream)
            {
                return ((T) serializer.ReadObject(stream));
            }
        }

        private void VerifyCorrelationIdHeaderInReponse(Dictionary<string, string> headers)
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
                        PlatformPlugin.Logger.Warning(CallState,
                            string.Format(CultureInfo.InvariantCulture,
                                "Returned correlation id '{0}' is not in GUID format.", correlationIdHeader));
                    }
                    else if (correlationIdInResponse != this.CallState.CorrelationId)
                    {
                        PlatformPlugin.Logger.Warning(
                            this.CallState,
                            string.Format(CultureInfo.InvariantCulture,
                                "Returned correlation id '{0}' does not match the sent correlation id '{1}'",
                                correlationIdHeader, CallState.CorrelationId));
                    }

                    break;
                }
            }
        }
    }
}
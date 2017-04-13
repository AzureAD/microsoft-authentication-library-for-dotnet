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
using Microsoft.Identity.Client.Internal.Instance;

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
            _bodyParameters[key] = value;
        }

        public async Task<TenantDiscoveryResponse> GetOpenIdConfiguration(Uri endPoint, RequestContext requestContext)
        {
            return await ExecuteRequest<TenantDiscoveryResponse>(endPoint, HttpMethod.Get, requestContext);
        }

        public async Task<InstanceDiscoveryResponse> DiscoverAadInstance(Uri endPoint, RequestContext requestContext)
        {
            return await ExecuteRequest<InstanceDiscoveryResponse>(endPoint, HttpMethod.Get, requestContext);
        }

        public async Task<TokenResponse> GetToken(Uri endPoint, RequestContext requestContext)
        {
            return await ExecuteRequest<TokenResponse>(endPoint, HttpMethod.Post, requestContext);
        }

        internal async Task<T> ExecuteRequest<T>(Uri endPoint, HttpMethod method, RequestContext requestContext)
        {
            bool addCorrelationId = (requestContext != null && !string.IsNullOrEmpty(requestContext.CorrelationId));
            if (addCorrelationId)
            {
                _headers.Add(OAuth2Header.CorrelationId, requestContext.CorrelationId);
                _headers.Add(OAuth2Header.RequestCorrelationIdInResponse, "true");
            }

            HttpResponse response = null;
            Uri endpointUri = CreateFullEndpointUri(endPoint);
            var httpEvent = new HttpEvent()
            {
                HttpPath = endpointUri.AbsolutePath,
                QueryParams = String.Join("&", MsalHelpers.ParseKeyValueList(endpointUri.Query, '&', false, true, requestContext).Keys)
            };
            Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, httpEvent);
            try
            {
                if (method == HttpMethod.Post)
                {
                    response = await HttpRequest.SendPost(endpointUri, _headers, _bodyParameters, requestContext);
                }
                else
                {
                    response = await HttpRequest.SendGet(endpointUri, _headers, requestContext);
                }

                httpEvent.HttpResponseStatus = (int) response.StatusCode;
                httpEvent.UserAgent = response.UserAgent;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    httpEvent.OauthErrorCode = JsonHelper.DeserializeFromJson<TokenResponse>(response.Body).Error;
                }
            }
            finally
            {
                Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, httpEvent);
            }

            return CreateResponse<T>(response, requestContext, addCorrelationId);
        }

        public static T CreateResponse<T>(HttpResponse response, RequestContext requestContext, bool addCorrelationId)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                CreateErrorResponse(response, requestContext);
            }

            if (addCorrelationId)
            {
                VerifyCorrelationIdHeaderInReponse(response.Headers, requestContext);
            }

            return JsonHelper.DeserializeFromJson<T>(response.Body);
        }

        public static void CreateErrorResponse(HttpResponse response, RequestContext requestContext)
        {
            MsalServiceException serviceEx;
            try
            {
                TokenResponse tokenResponse = JsonHelper.DeserializeFromJson<TokenResponse>(response.Body);

                if (MsalUiRequiredException.InvalidGrantError.Equals(tokenResponse.Error,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new MsalUiRequiredException(MsalUiRequiredException.InvalidGrantError,
                        tokenResponse.ErrorDescription);
                }

                serviceEx = new MsalServiceException(tokenResponse.Error, tokenResponse.ErrorDescription, (int)response.StatusCode, tokenResponse.Claims, null)
                {
                    ResponseBody =  response.Body
                };
            }
            catch (SerializationException)
            {
                serviceEx = new MsalServiceException(MsalException.UnknownError, response.Body, (int)response.StatusCode);
            }

            requestContext.Logger.Error(serviceEx);
            throw serviceEx;
        }

        internal Uri CreateFullEndpointUri(Uri endPoint)
        {
            UriBuilder endpointUri = new UriBuilder(endPoint);
            string extraQp = _queryParameters.ToQueryParameter();
            endpointUri.AppendQueryParameters(extraQp);
            
            return new Uri(MsalHelpers.CheckForExtraQueryParameter(endpointUri.Uri.AbsoluteUri));
        }

        private static void VerifyCorrelationIdHeaderInReponse(Dictionary<string, string> headers, RequestContext requestContext)
        {
            foreach (string reponseHeaderKey in headers.Keys)
            {
                string trimmedKey = reponseHeaderKey.Trim();
                if (string.Compare(trimmedKey, OAuth2Header.CorrelationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string correlationIdHeader = headers[trimmedKey].Trim();
                    if (!string.Equals(correlationIdHeader, requestContext.CorrelationId))
                    {
                        requestContext.Logger.Warning(string.Format(CultureInfo.InvariantCulture,
                                "Returned correlation id '{0}' does not match the sent correlation id '{1}'",
                                correlationIdHeader, requestContext.CorrelationId));
                    }

                    break;
                }
            }
        }
    }
}
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
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.Telemetry;
using Telemetry = Microsoft.Identity.Client.Telemetry;

namespace Microsoft.Identity.Core.OAuth2
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
            return await ExecuteRequest<TenantDiscoveryResponse>(endPoint, HttpMethod.Get, requestContext).ConfigureAwait(false);
        }

        public async Task<InstanceDiscoveryResponse> DiscoverAadInstance(Uri endPoint, RequestContext requestContext)
        {
            return await ExecuteRequest<InstanceDiscoveryResponse>(endPoint, HttpMethod.Get, requestContext).ConfigureAwait(false);
        }

        public async Task<MsalTokenResponse> GetToken(Uri endPoint, RequestContext requestContext)
        {
            return await ExecuteRequest<MsalTokenResponse>(endPoint, HttpMethod.Post, requestContext).ConfigureAwait(false);
        }

        internal async Task<T> ExecuteRequest<T>(Uri endPoint, HttpMethod method, RequestContext requestContext)
        {
            bool addCorrelationId = (requestContext != null && !string.IsNullOrEmpty(requestContext.Logger.CorrelationId.ToString()));
            if (addCorrelationId)
            {
                _headers.Add(OAuth2Header.CorrelationId, requestContext.Logger.CorrelationId.ToString());
                _headers.Add(OAuth2Header.RequestCorrelationIdInResponse, "true");
            }

            HttpResponse response = null;
            Uri endpointUri = CreateFullEndpointUri(endPoint);
            var httpEvent = new HttpEvent(){HttpPath = endpointUri, QueryParams = endpointUri.Query};
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, httpEvent);
            try
            {
                if (method == HttpMethod.Post)
                {
                    response = await HttpRequest.SendPost(endpointUri, _headers, _bodyParameters, requestContext).ConfigureAwait(false);
                }
                else
                {
                    response = await HttpRequest.SendGet(endpointUri, _headers, requestContext).ConfigureAwait(false);
                }

                httpEvent.HttpResponseStatus = (int) response.StatusCode;
                httpEvent.UserAgent = response.UserAgent;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    httpEvent.OauthErrorCode = JsonHelper.DeserializeFromJson<MsalTokenResponse>(response.Body).Error;
                }
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, httpEvent);
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
                MsalTokenResponse msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(response.Body);

                if (MsalUiRequiredException.InvalidGrantError.Equals(msalTokenResponse.Error,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new MsalUiRequiredException(MsalUiRequiredException.InvalidGrantError,
                        msalTokenResponse.ErrorDescription)
                    {
                        Claims = msalTokenResponse.Claims
                    };
                }

                serviceEx = new MsalServiceException(msalTokenResponse.Error, msalTokenResponse.ErrorDescription, (int)response.StatusCode, msalTokenResponse.Claims, null)
                {
                    ResponseBody =  response.Body
                };
            }
            catch (SerializationException)
            {
                serviceEx = new MsalServiceException(MsalException.UnknownError, response.Body, (int)response.StatusCode);
            }

            requestContext.Logger.Error(serviceEx);
            requestContext.Logger.ErrorPii(serviceEx);
            throw serviceEx;
        }

        internal Uri CreateFullEndpointUri(Uri endPoint)
        {
            UriBuilder endpointUri = new UriBuilder(endPoint);
            string extraQp = _queryParameters.ToQueryParameter();
            endpointUri.AppendQueryParameters(extraQp);

            return endpointUri.Uri;
        }

        private static void VerifyCorrelationIdHeaderInReponse(Dictionary<string, string> headers, RequestContext requestContext)
        {
            foreach (string reponseHeaderKey in headers.Keys)
            {
                string trimmedKey = reponseHeaderKey.Trim();
                if (string.Compare(trimmedKey, OAuth2Header.CorrelationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string correlationIdHeader = headers[trimmedKey].Trim();
                    if (!string.Equals(correlationIdHeader, requestContext.Logger.CorrelationId))
                    {
                        requestContext.Logger.Warning("Returned correlation id does not match the sent correlation id");
                        requestContext.Logger.WarningPii(string.Format(CultureInfo.InvariantCulture,
                            "Returned correlation id '{0}' does not match the sent correlation id '{1}'",
                            correlationIdHeader, requestContext.Logger.CorrelationId));
                    }

                    break;
                }
            }
        }
    }
}
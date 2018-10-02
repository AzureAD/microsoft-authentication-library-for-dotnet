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
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.Telemetry;

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

        public void AddBodyParameter(string key, string value)
        {
            _bodyParameters[key] = value;
        }

        public async Task<InstanceDiscoveryResponse> DiscoverAadInstanceAsync(Uri endPoint, RequestContext requestContext)
        {
            return await ExecuteRequestAsync<InstanceDiscoveryResponse>(endPoint, HttpMethod.Get, requestContext).ConfigureAwait(false);
        }

        public async Task<MsalTokenResponse> GetTokenAsync(Uri endPoint, RequestContext requestContext)
        {
            return await ExecuteRequestAsync<MsalTokenResponse>(endPoint, HttpMethod.Post, requestContext).ConfigureAwait(false);
        }

        internal async Task<T> ExecuteRequestAsync<T>(Uri endPoint, HttpMethod method, RequestContext requestContext)
        {
            bool addCorrelationId = (requestContext != null && !string.IsNullOrEmpty(requestContext.Logger.CorrelationId.ToString()));
            if (addCorrelationId)
            {
                _headers.Add(OAuth2Header.CorrelationId, requestContext.Logger.CorrelationId.ToString());
                _headers.Add(OAuth2Header.RequestCorrelationIdInResponse, "true");
            }

            HttpResponse response = null;
            Uri endpointUri = CreateFullEndpointUri(endPoint);
            var httpEvent = new HttpEvent() { HttpPath = endpointUri, QueryParams = endpointUri.Query };
            var telemetry = CoreTelemetryService.GetInstance();
            telemetry.StartEvent(requestContext.TelemetryRequestId, httpEvent);
            try
            {
                if (method == HttpMethod.Post)
                {
                    response = await HttpRequest.SendPostAsync(endpointUri, _headers, _bodyParameters, requestContext).ConfigureAwait(false);
                }
                else
                {
                    response = await HttpRequest.SendGetAsync(endpointUri, _headers, requestContext).ConfigureAwait(false);
                }

                httpEvent.HttpResponseStatus = (int)response.StatusCode;
                httpEvent.UserAgent = response.UserAgent;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    httpEvent.OauthErrorCode = JsonHelper.DeserializeFromJson<MsalTokenResponse>(response.Body).Error;
                }
            }
            finally
            {
                telemetry.StopEvent(requestContext.TelemetryRequestId, httpEvent);
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
                VerifyCorrelationIdHeaderInResponse(response.HeadersAsDictionary, requestContext);
            }

            return JsonHelper.DeserializeFromJson<T>(response.Body);
        }

        public static void CreateErrorResponse(HttpResponse response, RequestContext requestContext)
        {
            Exception serviceEx;
            try
            {
                MsalTokenResponse msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(response.Body);

                if (CoreErrorCodes.InvalidGrantError.Equals(msalTokenResponse.Error,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw CoreExceptionFactory.Instance.GetUiRequiredException(
                        CoreErrorCodes.InvalidGrantError,
                        msalTokenResponse.ErrorDescription,
                        null,
                         new ExceptionDetail()
                         {
                             Claims = msalTokenResponse.Claims,
                         });
                }

                serviceEx = CoreExceptionFactory.Instance.GetServiceException(
                    msalTokenResponse.Error,
                    msalTokenResponse.ErrorDescription,
                    null,
                    new ExceptionDetail()
                    {
                        ResponseBody = response.Body,
                        StatusCode = (int)response.StatusCode,
                        Claims = msalTokenResponse.Claims,
                    });
            }
            catch (SerializationException)
            {
                serviceEx = CoreExceptionFactory.Instance.GetServiceException(
                    CoreErrorCodes.UnknownError,
                    response.Body,
                    new ExceptionDetail() { StatusCode = (int)response.StatusCode });
            }

            requestContext.Logger.ErrorPii(serviceEx);
            throw serviceEx;
        }

        private Uri CreateFullEndpointUri(Uri endPoint)
        {
            UriBuilder endpointUri = new UriBuilder(endPoint);
            string extraQp = _queryParameters.ToQueryParameter();
            endpointUri.AppendQueryParameters(extraQp);

            return endpointUri.Uri;
        }

        private static void VerifyCorrelationIdHeaderInResponse(IDictionary<string, string> headers, RequestContext requestContext)
        {
            foreach (string reponseHeaderKey in headers.Keys)
            {
                string trimmedKey = reponseHeaderKey.Trim();
                if (string.Compare(trimmedKey, OAuth2Header.CorrelationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string correlationIdHeader = headers[trimmedKey].Trim();
                    if (!string.Equals(correlationIdHeader, requestContext.Logger.CorrelationId))
                    {
                        requestContext.Logger.WarningPii(
                            string.Format(CultureInfo.InvariantCulture,
                               "Returned correlation id '{0}' does not match the sent correlation id '{1}'",
                                correlationIdHeader, requestContext.Logger.CorrelationId),
                            "Returned correlation id does not match the sent correlation id");
                    }

                    break;
                }
            }
        }
    }
}
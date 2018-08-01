//----------------------------------------------------------------------
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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http
{
    class AdalHttpClient
    {
        private const string DeviceAuthHeaderName = "x-ms-PKeyAuth";
        private const string DeviceAuthHeaderValue = "1.0";
        private const string WwwAuthenticateHeader = "WWW-Authenticate";
        private const string PKeyAuthName = "PKeyAuth";
        private const int DelayTimePeriodMilliSeconds = 1000;
        private readonly RequestContext _requestContext;
        internal bool Resiliency = false;
        internal bool RetryOnce = true;

        public AdalHttpClient(string uri, RequestContext requestContext)
        {
            this.RequestUri = CheckForExtraQueryParameter(uri);
            this.Client = new HttpClientWrapper(RequestUri, requestContext);
            _requestContext = requestContext;
        }

        internal string RequestUri { get; set; }

        public IHttpClient Client { get; private set; }

        public RequestContext RequestContext { get; private set; }

        public async Task<T> GetResponseAsync<T>()
        {
            return await this.GetResponseAsync<T>(true).ConfigureAwait(false);
        }

        private async Task<T> GetResponseAsync<T>(bool respondToDeviceAuthChallenge)
        {
            T typedResponse = default(T);
            IHttpWebResponse response;

            try
            {
                IDictionary<string, string> adalIdHeaders = AdalIdHelper.GetAdalIdParameters();
                foreach (KeyValuePair<string, string> kvp in adalIdHeaders)
                {
                    this.Client.Headers[kvp.Key] = kvp.Value;
                }
                //add pkeyauth header
                this.Client.Headers[DeviceAuthHeaderName] = DeviceAuthHeaderValue;
                using (response = await this.Client.GetResponseAsync().ConfigureAwait(false))
                {
                    typedResponse = EncodingHelper.DeserializeResponse<T>(response.ResponseString);
                }
            }
            catch (HttpRequestWrapperException ex)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    Resiliency = true;

                    _requestContext.Logger.Info("Network timeout, Exception type: " + ex.InnerException.GetType());
                    _requestContext.Logger.InfoPii("Network timeout, Exception message: " + ex.InnerException.Message);
                }

                if (!Resiliency && ex.WebResponse == null)
                {
                    _requestContext.Logger.Error(ex);
                    _requestContext.Logger.ErrorPii(ex);
                    throw new AdalServiceException(AdalError.Unknown, ex);
                }

                //check for resiliency
                if (!Resiliency && (int)ex.WebResponse.StatusCode >= 500 && (int)ex.WebResponse.StatusCode < 600)
                {
                    _requestContext.Logger.Info("HttpStatus code: " + ex.WebResponse.StatusCode + ", Exception type: " + ex.InnerException?.GetType());

                    _requestContext.Logger.InfoPii("HttpStatus code: " + ex.WebResponse.StatusCode + ", Exception message: " + ex.InnerException?.Message);
                    
                    Resiliency = true;
                }

                if (Resiliency)
                {
                    if (RetryOnce)
                    {
                        await Task.Delay(DelayTimePeriodMilliSeconds).ConfigureAwait(false);
                        RetryOnce = false;

                        var msg = "Retrying one more time..";
                        _requestContext.Logger.Info(msg);
                        _requestContext.Logger.InfoPii(msg);

                        return await this.GetResponseAsync<T>(respondToDeviceAuthChallenge).ConfigureAwait(false);
                    }

                    _requestContext.Logger.Info("Retry Failed, Exception type: " + ex.InnerException?.GetType());
                    _requestContext.Logger.InfoPii("Retry Failed, Exception message: " + ex.InnerException?.Message);
                }
                
                if (!this.IsDeviceAuthChallenge(ex.WebResponse, respondToDeviceAuthChallenge))
                {
                    TokenResponse tokenResponse = TokenResponse.CreateFromErrorResponse(ex.WebResponse);
                    string[] errorCodes = tokenResponse.ErrorCodes ?? new[] {ex.WebResponse.StatusCode.ToString()};
                    AdalServiceException serviceEx = new AdalServiceException(tokenResponse.Error,
                        tokenResponse.ErrorDescription,
                        errorCodes, ex);

                    if (ex.WebResponse.StatusCode == HttpStatusCode.BadRequest &&
                        tokenResponse.Error == AdalError.InteractionRequired)
                    {
                        throw new AdalClaimChallengeException(tokenResponse.Error, tokenResponse.ErrorDescription, ex, tokenResponse.Claims);
                    }

                    throw serviceEx;
                }

                //attempt device auth
                return await HandleDeviceAuthChallengeAsync<T>(ex.WebResponse).ConfigureAwait(false);
            }
            
            return typedResponse;
        }

        private bool IsDeviceAuthChallenge(IHttpWebResponse response, bool respondToDeviceAuthChallenge)
        {
            return DeviceAuthHelper.CanHandleDeviceAuthChallenge
                   && response != null
                   && respondToDeviceAuthChallenge
                   && response?.Headers != null
                   && response.StatusCode == HttpStatusCode.Unauthorized
                   && response.Headers.Contains(WwwAuthenticateHeader)
                   && response.Headers.GetValues(WwwAuthenticateHeader).FirstOrDefault()
                       .StartsWith(PKeyAuthName, StringComparison.OrdinalIgnoreCase);
        }

        private IDictionary<string, string> ParseChallengeData(IHttpWebResponse response)
        {
            IDictionary<string, string> data = new Dictionary<string, string>();
            string wwwAuthenticate = response.Headers.GetValues(WwwAuthenticateHeader).FirstOrDefault();
            wwwAuthenticate = wwwAuthenticate.Substring(PKeyAuthName.Length + 1);
            List<string> headerPairs = EncodingHelper.SplitWithQuotes(wwwAuthenticate, ',');
            foreach (string pair in headerPairs)
            {
                List<string> keyValue = EncodingHelper.SplitWithQuotes(pair, '=');
                data.Add(keyValue[0].Trim(), keyValue[1].Trim().Replace("\"", ""));
            }

            return data;
        }

        private async Task<T> HandleDeviceAuthChallengeAsync<T>(IHttpWebResponse response)
        {
            IDictionary<string, string> responseDictionary = this.ParseChallengeData(response);

            if (!responseDictionary.ContainsKey("SubmitUrl"))
            {
                responseDictionary["SubmitUrl"] = RequestUri;
            }

            string responseHeader = await DeviceAuthHelper.CreateDeviceAuthChallengeResponseAsync(responseDictionary)
                .ConfigureAwait(false);
            IRequestParameters rp = this.Client.BodyParameters;
            this.Client = new HttpClientWrapper(CheckForExtraQueryParameter(responseDictionary["SubmitUrl"]), RequestContext);
            this.Client.BodyParameters = rp;
            this.Client.Headers["Authorization"] = responseHeader;
            return await this.GetResponseAsync<T>(false).ConfigureAwait(false);
        }

        private static string CheckForExtraQueryParameter(string url)
        {
            string extraQueryParameter = new PlatformInformation().GetEnvironmentVariable("ExtraQueryParameter");
            string delimiter = (url.IndexOf('?') > 0) ? "&" : "?";
            if (!string.IsNullOrWhiteSpace(extraQueryParameter))
            {
                url += string.Concat(delimiter, extraQueryParameter);
            }

            return url;
        }
    }
}
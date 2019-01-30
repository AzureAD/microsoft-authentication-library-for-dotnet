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
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.WsTrust
{
    internal class WsTrustWebRequestManager : IWsTrustWebRequestManager
    {
        private readonly IHttpManager _httpManager;

        public WsTrustWebRequestManager(IHttpManager httpManager)
        {
            _httpManager = httpManager;
        }

        /// <inheritdoc/>
        public async Task<MexDocument> GetMexDocumentAsync(string federationMetadataUrl, RequestContext requestContext)
        {
            var uri = new UriBuilder(federationMetadataUrl);
            HttpResponse httpResponse = await _httpManager.SendGetAsync(uri.Uri, null, requestContext).ConfigureAwait(false);
            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw MsalExceptionFactory.GetServiceException(
                    CoreErrorCodes.AccessingWsMetadataExchangeFailed,
                    string.Format(CultureInfo.CurrentCulture,
                        CoreErrorMessages.HttpRequestUnsuccessful,
                        (int)httpResponse.StatusCode, httpResponse.StatusCode),
                    new ExceptionDetail()
                    {
                        StatusCode = (int)httpResponse.StatusCode,
                        ServiceErrorCodes = new[] { httpResponse.StatusCode.ToString() }
                    });
            }

            var mexDoc = new MexDocument(httpResponse.Body);

            requestContext.Logger.InfoPii(
                $"MEX document fetched and parsed from '{federationMetadataUrl}'",
                "Fetched and parsed MEX");

            return mexDoc;
        }

        /// <inheritdoc/>
        public async Task<WsTrustResponse> GetWsTrustResponseAsync(
            WsTrustEndpoint wsTrustEndpoint,
            string wsTrustRequest,
            RequestContext requestContext)
        {
            var headers = new Dictionary<string, string>
            {
                { "ContentType", "application/soap+xml" },
                { "SOAPAction", (wsTrustEndpoint.Version == WsTrustVersion.WsTrust2005) ? XmlNamespace.Issue2005.ToString() : XmlNamespace.Issue.ToString() }
            };

            var body = new StringContent(
                wsTrustRequest,
                Encoding.UTF8, headers["ContentType"]);

            IHttpWebResponse resp = await _httpManager.SendPostForceResponseAsync(wsTrustEndpoint.Uri, headers, body, requestContext).ConfigureAwait(false);

            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
            {
                string errorMessage = null;
                try
                {
                    errorMessage = WsTrustResponse.ReadErrorResponse(XDocument.Parse(resp.Body, LoadOptions.None), requestContext);
                }
                catch (System.Xml.XmlException)
                {
                    errorMessage = resp.Body;
                }

                throw MsalExceptionFactory.GetServiceException(
                    CoreErrorCodes.FederatedServiceReturnedError,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        CoreErrorMessages.FederatedServiceReturnedErrorTemplate,
                        wsTrustEndpoint.Uri,
                        errorMessage),
                    new ExceptionDetail()
                    {
                        StatusCode = (int)resp.StatusCode,
                        ResponseBody = resp.Body,
                        HttpResponseHeaders = resp.Headers
                    });
            }

            try
            {
                return WsTrustResponse.CreateFromResponse(resp.Body, wsTrustEndpoint.Version);
            }
            catch (System.Xml.XmlException ex)
            {
                throw MsalExceptionFactory.GetClientException(
                    CoreErrorCodes.ParsingWsTrustResponseFailed, CoreErrorCodes.ParsingWsTrustResponseFailed, ex);
            }
        }

        public async Task<UserRealmDiscoveryResponse> GetUserRealmAsync(
            string userRealmUriPrefix, 
            string userName, 
            RequestContext requestContext)
        {
            requestContext.Logger.Info("Sending request to userrealm endpoint.");

             var uri = new UriBuilder(userRealmUriPrefix + userName + "?api-version=1.0").Uri;

            var httpResponse = await _httpManager.SendGetAsync(uri, MsalIdHelper.GetMsalIdParameters(requestContext.Logger), requestContext).ConfigureAwait(false);
            return httpResponse.StatusCode == System.Net.HttpStatusCode.OK 
                ? JsonHelper.DeserializeFromJson<UserRealmDiscoveryResponse>(httpResponse.Body) 
                : null;
        }
    } 
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Identity.Client.Core;
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
            HttpResponse httpResponse = await _httpManager.SendGetAsync(uri.Uri, null, requestContext.Logger).ConfigureAwait(false);
            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                string message = string.Format(CultureInfo.CurrentCulture,
                        MsalErrorMessage.HttpRequestUnsuccessful,
                        (int)httpResponse.StatusCode, httpResponse.StatusCode);

                throw MsalServiceExceptionFactory.FromHttpResponse(
                    MsalError.AccessingWsMetadataExchangeFailed,
                    message,
                    httpResponse);
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

            HttpResponse resp = await _httpManager.SendPostForceResponseAsync(wsTrustEndpoint.Uri, headers, body, requestContext.Logger).ConfigureAwait(false);

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

                string message = string.Format(
                        CultureInfo.CurrentCulture,
                        MsalErrorMessage.FederatedServiceReturnedErrorTemplate,
                        wsTrustEndpoint.Uri,
                        errorMessage);

                throw MsalServiceExceptionFactory.FromHttpResponse(
                    MsalError.FederatedServiceReturnedError,
                    message,
                    resp);
            }

            try
            {
                return WsTrustResponse.CreateFromResponse(resp.Body, wsTrustEndpoint.Version);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new MsalClientException(
                    MsalError.ParsingWsTrustResponseFailed, MsalError.ParsingWsTrustResponseFailed, ex);
            }
        }

        public async Task<UserRealmDiscoveryResponse> GetUserRealmAsync(
            string userRealmUriPrefix,
            string userName,
            RequestContext requestContext)
        {
            requestContext.Logger.Info("Sending request to userrealm endpoint.");

             var uri = new UriBuilder(userRealmUriPrefix + userName + "?api-version=1.0").Uri;

            var httpResponse = await _httpManager.SendGetAsync(
                uri,
                MsalIdHelper.GetMsalIdParameters(requestContext.Logger),
                requestContext.Logger).ConfigureAwait(false);

            return httpResponse.StatusCode == System.Net.HttpStatusCode.OK
                ? JsonHelper.DeserializeFromJson<UserRealmDiscoveryResponse>(httpResponse.Body)
                : null;
        }
    }
}

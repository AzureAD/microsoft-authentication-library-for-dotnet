// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore;
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
        public async Task<MexDocument> GetMexDocumentAsync(string federationMetadataUrl, RequestContext requestContext, string federationMetadata = null)
        {
            MexDocument mexDoc;

            if (!string.IsNullOrEmpty(federationMetadata))
            {
                mexDoc = new MexDocument(federationMetadata);
                requestContext.Logger.Info(() => $"MEX document fetched and parsed from provided federation metadata");
                return mexDoc;
            }

            Dictionary<string, string> msalIdParams = MsalIdHelper.GetMsalIdParameters(requestContext.Logger);

            var uri = new UriBuilder(federationMetadataUrl);
            

            HttpResponse httpResponse = await _httpManager.SendRequestAsync(
                    uri.Uri,
                    msalIdParams,
                    body: null,
                    HttpMethod.Get,
                    logger: requestContext.Logger,
                    doNotThrow: false,
                    retry: true,
                    mtlsCertificate: null,
                    requestContext.UserCancellationToken)
                .ConfigureAwait(false);

            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                string message = string.Format(CultureInfo.CurrentCulture,
                        MsalErrorMessage.HttpRequestUnsuccessful + "See https://aka.ms/msal-net-ropc for more information. ",
                        (int)httpResponse.StatusCode, httpResponse.StatusCode);

                requestContext.Logger.ErrorPii(
                    string.Format(MsalErrorMessage.RequestFailureErrorMessagePii,
                        requestContext.ApiEvent?.ApiIdString,
                        requestContext.ServiceBundle.Config.Authority.AuthorityInfo.CanonicalAuthority,
                        requestContext.ServiceBundle.Config.ClientId),
                    string.Format(MsalErrorMessage.RequestFailureErrorMessage,
                        requestContext.ApiEvent?.ApiIdString, 
                        requestContext.ServiceBundle.Config.Authority.AuthorityInfo.Host));
                throw MsalServiceExceptionFactory.FromHttpResponse(
                    MsalError.AccessingWsMetadataExchangeFailed,
                    message,
                    httpResponse);
            }

            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
            {
                string errorMessage = null;
                try
                {
                    errorMessage = WsTrustResponse.ReadErrorResponse(XDocument.Parse(resp.Body, LoadOptions.None));
                }
                catch (System.Xml.XmlException)
                {
                    errorMessage = resp.Body;
                }

                requestContext.Logger.ErrorPii(LogMessages.WsTrustRequestFailed + $"Status code: {resp.StatusCode} \nError message: {errorMessage}", 
                    LogMessages.WsTrustRequestFailed + $"Status code: {resp.StatusCode}");

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
                var wsTrustResponse = WsTrustResponse.CreateFromResponse(resp.Body, wsTrustEndpoint.Version);

                if  (wsTrustResponse == null)
                {
                    requestContext.Logger.ErrorPii("Token not found in the ws trust response. See response for more details: \n" + resp.Body, "Token not found in WS-Trust response.");
                    throw new MsalClientException(MsalError.ParsingWsTrustResponseFailed, MsalErrorMessage.ParsingWsTrustResponseFailedDueToConfiguration);
                }

                return wsTrustResponse;
            }
            catch (System.Xml.XmlException ex)
            {
                string message = string.Format(
                        CultureInfo.CurrentCulture,
                        MsalErrorMessage.ParsingWsTrustResponseFailedErrorTemplate,
                        wsTrustEndpoint.Uri,
                        resp.Body);

                throw new MsalClientException(
                    MsalError.ParsingWsTrustResponseFailed, message, ex);
            }
        }

        public async Task<UserRealmDiscoveryResponse> GetUserRealmAsync(
            string userRealmUriPrefix,
            string userName,
            RequestContext requestContext)
        {
            requestContext.Logger.Info("Sending request to userrealm endpoint. ");

            Dictionary<string, string> msalIdParams = MsalIdHelper.GetMsalIdParameters(requestContext.Logger);

            var uri = new UriBuilder(userRealmUriPrefix + userName + "?api-version=1.0").Uri;
            
            var httpResponse = await _httpManager.SendRequestAsync(
               uri,
               msalIdParams,
               body: null,
               HttpMethod.Get,
               logger: requestContext.Logger,
               doNotThrow: false,
               retry: true,
               mtlsCertificate: null,
               requestContext.UserCancellationToken)
                  .ConfigureAwait(false);

            if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return JsonHelper.DeserializeFromJson<UserRealmDiscoveryResponse>(httpResponse.Body);
            }

            string message = string.Format(CultureInfo.CurrentCulture,
                    MsalErrorMessage.HttpRequestUnsuccessful,
                    (int)httpResponse.StatusCode, httpResponse.StatusCode);

            requestContext.Logger.ErrorPii(
                    string.Format(MsalErrorMessage.RequestFailureErrorMessagePii,
                        requestContext.ApiEvent?.ApiIdString,
                        requestContext.ServiceBundle.Config.Authority.AuthorityInfo.CanonicalAuthority,
                        requestContext.ServiceBundle.Config.ClientId),
                    string.Format(MsalErrorMessage.RequestFailureErrorMessage,
                        requestContext.ApiEvent?.ApiIdString, 
                        requestContext.ServiceBundle.Config.Authority.AuthorityInfo.Host));
            throw MsalServiceExceptionFactory.FromHttpResponse(
                MsalError.UserRealmDiscoveryFailed,
                message,
                httpResponse);
        }
    }
}

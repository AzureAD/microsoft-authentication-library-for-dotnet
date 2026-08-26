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
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.WsTrust
{
    internal class WsTrustWebRequestManager : IWsTrustWebRequestManager
    {
        private const int MaxRedirects = 50;
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

            if (string.IsNullOrWhiteSpace(federationMetadataUrl))
            {
                throw new MsalClientException(
                    MsalError.MissingFederationMetadataUrl,
                    MsalErrorMessage.MissingFederationMetadataUrl);
            }

            if (!Uri.IsWellFormedUriString(federationMetadataUrl, UriKind.Absolute))
            {
                throw new MsalClientException(
                    MsalError.ParsingWsMetadataExchangeFailed,
                    MsalErrorMessage.WsTrustMetadataEndpointInvalidUri);
            }

            var uri = new Uri(federationMetadataUrl);
            if (!IsHttpsUri(uri))
            {
                throw new MsalClientException(
                    MsalError.AccessingWsMetadataExchangeFailed,
                    MsalErrorMessage.WsTrustMetadataEndpointRequiresHttps);
            }

            IRetryPolicyFactory retryPolicyFactory = requestContext.ServiceBundle.Config.RetryPolicyFactory;
            IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.STS);

            Uri requestUri = uri;
            HttpResponse httpResponse;
            int redirectCount = 0;

            while (true)
            {
                httpResponse = await _httpManager.SendRequestAsync(
                    requestUri,
                    msalIdParams,
                    body: null,
                    method: HttpMethod.Get,
                    logger: requestContext.Logger,
                    doNotThrow: false,
                    mtlsCertificate: null,
                    validateServerCertificate: null,
                    cancellationToken: requestContext.UserCancellationToken,
                    retryPolicy: retryPolicy,
                    allowAutoRedirect: false)
                .ConfigureAwait(false);

                ThrowIfNonHttpsResponse(httpResponse, requestUri);

                if (!TryGetSecureRedirectUri(httpResponse, requestUri, out Uri redirectUri) ||
                    redirectCount >= MaxRedirects)
                {
                    break;
                }

                requestUri = redirectUri;
                redirectCount++;
            }

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

            mexDoc = new MexDocument(httpResponse.Body);

            requestContext.Logger.InfoPii(
                () => $"MEX document fetched and parsed from '{federationMetadataUrl}'",
                () => "Fetched and parsed MEX");

            return mexDoc;
        }

        /// <inheritdoc/>
        public async Task<WsTrustResponse> GetWsTrustResponseAsync(
            WsTrustEndpoint wsTrustEndpoint,
            string wsTrustRequest,
            RequestContext requestContext)
        {
            if (wsTrustEndpoint is null)
            {
                throw new ArgumentNullException(nameof(wsTrustEndpoint));
            }

            if (!IsHttpsUri(wsTrustEndpoint.Uri))
            {
                throw new MsalClientException(
                    MsalError.WsTrustEndpointNotFoundInMetadataDocument,
                    MsalErrorMessage.WsTrustEndpointNotFoundInMetadataDocument);
            }

            var headers = new Dictionary<string, string>
            {
                { "SOAPAction", (wsTrustEndpoint.Version == WsTrustVersion.WsTrust2005) ? XmlNamespace.Issue2005.ToString() : XmlNamespace.Issue.ToString() }
            };
            
            // CodeQL [SM00417] False Positive: wsTrustRequest is a body parameter for HttpRequest that follows WsTrust protocol
            var body = new StringContent(wsTrustRequest, Encoding.UTF8, "application/soap+xml");

            IRetryPolicyFactory retryPolicyFactory = requestContext.ServiceBundle.Config.RetryPolicyFactory;
            IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.STS);

            Uri requestUri = wsTrustEndpoint.Uri;
            HttpMethod requestMethod = HttpMethod.Post;
            HttpContent requestBody = body;
            HttpResponse resp;
            int redirectCount = 0;

            while (true)
            {
                resp = await _httpManager.SendRequestAsync(
                    requestUri,
                    headers,
                    body: requestBody,
                    method: requestMethod,
                    logger: requestContext.Logger,
                    doNotThrow: true,
                    mtlsCertificate: null,
                    validateServerCertificate: null,
                    cancellationToken: requestContext.UserCancellationToken,
                    retryPolicy: retryPolicy,
                    allowAutoRedirect: false)
                .ConfigureAwait(false);

                ThrowIfNonHttpsResponse(resp, requestUri);

                if (!TryGetSecureRedirectUri(resp, requestUri, out Uri redirectUri) ||
                    redirectCount >= MaxRedirects)
                {
                    break;
                }

                if (RedirectChangesMethodToGet(resp.StatusCode, requestMethod))
                {
                    requestMethod = HttpMethod.Get;
                    requestBody = null;
                }

                requestUri = redirectUri;
                redirectCount++;
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
                requestContext.Logger.ErrorPii("Error parsing WS-Trust response: \n" + resp.Body, "Error parsing WS-Trust response. ");

                string message = string.Format(
                        CultureInfo.CurrentCulture,
                        MsalErrorMessage.ParsingWsTrustResponseFailedErrorTemplate,
                        wsTrustEndpoint.Uri);

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

            IRetryPolicyFactory retryPolicyFactory = requestContext.ServiceBundle.Config.RetryPolicyFactory;
            IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.STS);

            var httpResponse = await _httpManager.SendRequestAsync(
                uri,
                msalIdParams,
                body: null,
                method: HttpMethod.Get,
                logger: requestContext.Logger,
                doNotThrow: false,
                mtlsCertificate: null,
                validateServerCertificate: null,
                cancellationToken: requestContext.UserCancellationToken,
                retryPolicy: retryPolicy)
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

        private static bool IsHttpsUri(Uri uri)
        {
            return uri is not null &&
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        private static void ThrowIfNonHttpsResponse(HttpResponse response, Uri requestUri)
        {
            if (!IsHttpsUri(response.RequestUri ?? requestUri))
            {
                throw new MsalClientException(
                    MsalError.NonHttpsRedirectNotSupported,
                    MsalErrorMessage.WsTrustNonHttpsRedirectNotSupported);
            }
        }

        private static bool TryGetSecureRedirectUri(
            HttpResponse response,
            Uri requestUri,
            out Uri redirectUri)
        {
            redirectUri = null;

            if (!IsRedirectStatusCode(response.StatusCode) ||
                response.Headers?.Location is not Uri location)
            {
                return false;
            }

            Uri responseUri = response.RequestUri ?? requestUri;
            redirectUri = location.IsAbsoluteUri
                ? location
                : new Uri(responseUri, location);

            if (!IsHttpsUri(redirectUri))
            {
                throw new MsalClientException(
                    MsalError.NonHttpsRedirectNotSupported,
                    MsalErrorMessage.WsTrustNonHttpsRedirectNotSupported);
            }

            return true;
        }

        private static bool IsRedirectStatusCode(System.Net.HttpStatusCode statusCode)
        {
            switch ((int)statusCode)
            {
                case 300:
                case 301:
                case 302:
                case 303:
                case 307:
                case 308:
                    return true;
                default:
                    return false;
            }
        }

        private static bool RedirectChangesMethodToGet(
            System.Net.HttpStatusCode statusCode,
            HttpMethod requestMethod)
        {
            int statusCodeValue = (int)statusCode;

            return ((statusCodeValue == 300 ||
                     statusCodeValue == 301 ||
                     statusCodeValue == 302) &&
                    requestMethod == HttpMethod.Post) ||
                (statusCodeValue == 303 &&
                 requestMethod != HttpMethod.Get &&
                 requestMethod != HttpMethod.Head);
        }
    }
}

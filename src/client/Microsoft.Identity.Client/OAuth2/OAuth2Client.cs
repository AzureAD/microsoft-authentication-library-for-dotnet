// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Instance.Oidc;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text.Json;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.OAuth2
{
    /// <summary>
    /// Responsible for talking to all the Identity provider endpoints:
    /// - instance discovery
    /// - endpoint metadata
    /// - mex
    /// - /token endpoint via TokenClient
    /// - device code endpoint
    /// </summary>    
    internal class OAuth2Client
    {
        private readonly Dictionary<string, string> _headers;
        private readonly Dictionary<string, string> _queryParameters = new Dictionary<string, string>();
        private readonly IDictionary<string, string> _bodyParameters = new Dictionary<string, string>();
        private readonly IHttpManager _httpManager;

        public OAuth2Client(ILoggerAdapter logger, IHttpManager httpManager)
        {
            _headers = new Dictionary<string, string>(MsalIdHelper.GetMsalIdParameters(logger));
            _httpManager = httpManager ?? throw new ArgumentNullException(nameof(httpManager));
        }

        public void AddQueryParameter(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                _queryParameters[key] = value;
            }
        }

        public void AddBodyParameter(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                _bodyParameters[key] = value;
            }
        }

        internal void AddHeader(string key, string value)
        {
            _headers[key] = value;
        }

        internal IReadOnlyDictionary<string, string> GetBodyParameters()
        {
            return new ReadOnlyDictionary<string, string>(_bodyParameters);
        }

        public async Task<InstanceDiscoveryResponse> DiscoverAadInstanceAsync(Uri endPoint, RequestContext requestContext)
        {
            return await ExecuteRequestAsync<InstanceDiscoveryResponse>(endPoint, HttpMethod.Get, requestContext)
                       .ConfigureAwait(false);
        }

        public async Task<OidcMetadata> DiscoverOidcMetadataAsync(Uri endPoint, RequestContext requestContext)
        {
            return await ExecuteRequestAsync<OidcMetadata>(endPoint, HttpMethod.Get, requestContext).ConfigureAwait(false);
        }

        internal async Task<MsalTokenResponse> GetTokenAsync(
            Uri endPoint,
            RequestContext requestContext,
            bool addCommonHeaders,
            Func<OnBeforeTokenRequestData, Task> onBeforePostRequestHandler)
        {
            return await ExecuteRequestAsync<MsalTokenResponse>(
                endPoint,
                HttpMethod.Post,
                requestContext,
                false,
                addCommonHeaders,
                onBeforePostRequestHandler).ConfigureAwait(false);
        }

        internal async Task<T> ExecuteRequestAsync<T>(
            Uri endPoint,
            HttpMethod method,
            RequestContext requestContext,
            bool expectErrorsOn200OK = false,
            bool addCommonHeaders = true,
            Func<OnBeforeTokenRequestData, Task> onBeforePostRequestData = null)
        {
            //Requests that are replayed by PKeyAuth do not need to have headers added because they already exist
            if (addCommonHeaders)
            {
                AddCommonHeaders(requestContext);
            }

            HttpResponse response = null;
            Uri endpointUri = AddExtraQueryParams(endPoint);

            using (requestContext.Logger.LogBlockDuration($"[Oauth2Client] Sending {method} request "))
            {
                try
                {
                    if (method == HttpMethod.Post)
                    {
                        if (onBeforePostRequestData != null)
                        {
                            var requestData = new OnBeforeTokenRequestData(_bodyParameters, _headers, endpointUri, requestContext.UserCancellationToken);
                            await onBeforePostRequestData(requestData).ConfigureAwait(false);
                        }

                        response = await _httpManager.SendPostAsync(
                            endpointUri,
                            _headers,
                            _bodyParameters,
                            requestContext.Logger,
                            cancellationToken: requestContext.UserCancellationToken)
                                 .ConfigureAwait(false);
                    }
                    else
                    {
                        response = await _httpManager.SendGetAsync(
                            endpointUri,
                            _headers,
                            requestContext.Logger,
                            cancellationToken: requestContext.UserCancellationToken)
                                .ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is TaskCanceledException && requestContext.UserCancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    requestContext.Logger.ErrorPii(
                    string.Format(MsalErrorMessage.RequestFailureErrorMessagePii,
                        requestContext.ApiEvent?.ApiIdString,
                        $"{endpointUri.Scheme}://{endpointUri.Host}{endpointUri.AbsolutePath}",
                        requestContext.ServiceBundle.Config.ClientId),
                    string.Format(MsalErrorMessage.RequestFailureErrorMessage,
                        requestContext.ApiEvent?.ApiIdString,
                        $"{endpointUri.Scheme}://{endpointUri.Host}"));
                    requestContext.Logger.ErrorPii(ex);

                    throw;
                }
            }

            if (requestContext.ApiEvent != null)
            {
                requestContext.ApiEvent.DurationInHttpInMs += _httpManager.LastRequestDurationInMs;
            }

            if (response.StatusCode != HttpStatusCode.OK || expectErrorsOn200OK)
            {
                requestContext.Logger.Verbose(() => "[Oauth2Client] Processing error response ");

                try
                {
                    // In cases where the end-point is not found (404) response.body will be empty.
                    // CreateResponse handles throwing errors - in the case of HttpStatusCode <> and ErrorResponse will be created.
                    if (!string.IsNullOrWhiteSpace(response.Body))
                    {
                        var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(response.Body);

                        if (response.StatusCode == HttpStatusCode.OK &&
                            expectErrorsOn200OK &&
                            !string.IsNullOrEmpty(msalTokenResponse?.Error))
                        {
                            ThrowServerException(response, requestContext);
                        }
                    }
                }
                catch (JsonException) // in the rare case we get an error response we cannot deserialize
                {
                    // CreateErrorResponse does the same validation. Will be logging the error there.
                }
            }

            return CreateResponse<T>(response, requestContext);
        }

        internal void AddBodyParameter(KeyValuePair<string, string> kvp)
        {
            _bodyParameters.Add(kvp);
        }

        private void AddCommonHeaders(RequestContext requestContext)
        {
            _headers.Add(OAuth2Header.CorrelationId, requestContext.CorrelationId.ToString());
            _headers.Add(OAuth2Header.RequestCorrelationIdInResponse, "true");

            if (!string.IsNullOrWhiteSpace(requestContext.Logger.ClientName))
            {
                _headers.Add(OAuth2Header.AppName, requestContext.Logger.ClientName);
            }

            if (!string.IsNullOrWhiteSpace(requestContext.Logger.ClientVersion))
            {
                _headers.Add(OAuth2Header.AppVer, requestContext.Logger.ClientVersion);
            }
        }

        public static T CreateResponse<T>(HttpResponse response, RequestContext requestContext)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                ThrowServerException(response, requestContext);
            }

            VerifyCorrelationIdHeaderInResponse(response.HeadersAsDictionary, requestContext);

            using (requestContext.Logger.LogBlockDuration("[OAuth2Client] Deserializing response"))
            {
                return JsonHelper.DeserializeFromJson<T>(response.Body);
            }
        }

        private static void ThrowServerException(HttpResponse response, RequestContext requestContext)
        {
            bool shouldLogAsError = true;

            var httpErrorCodeMessage = string.Format(CultureInfo.InvariantCulture, "HttpStatusCode: {0}: {1}", (int)response.StatusCode, response.StatusCode.ToString());
            requestContext.Logger.Info(httpErrorCodeMessage);

            MsalServiceException exceptionToThrow;
            try
            {
                exceptionToThrow = ExtractErrorsFromTheResponse(response, ref shouldLogAsError);
            }
            catch (JsonException) // in the rare case we get an error response we cannot deserialize
            {
                exceptionToThrow = MsalServiceExceptionFactory.FromHttpResponse(
                    MsalError.NonParsableOAuthError,
                    MsalErrorMessage.NonParsableOAuthError,
                    response);
            }
            catch (Exception ex)
            {

                exceptionToThrow = MsalServiceExceptionFactory.FromHttpResponse(
                    MsalError.UnknownError,
                    response.Body,
                    response,
                    ex);
            }

            exceptionToThrow ??= MsalServiceExceptionFactory.FromHttpResponse(
                    response.StatusCode == HttpStatusCode.NotFound
                        ? MsalError.HttpStatusNotFound
                        : MsalError.HttpStatusCodeNotOk,
                    httpErrorCodeMessage,
                    response);

            if (shouldLogAsError)
            {
                requestContext.Logger.ErrorPii(
                    string.Format(MsalErrorMessage.RequestFailureErrorMessagePii,
                        requestContext.ApiEvent?.ApiIdString,
                        requestContext.ServiceBundle.Config.Authority.AuthorityInfo.CanonicalAuthority,
                        requestContext.ServiceBundle.Config.ClientId),
                    string.Format(MsalErrorMessage.RequestFailureErrorMessage,
                        requestContext.ApiEvent?.ApiIdString,
                        requestContext.ServiceBundle.Config.Authority.AuthorityInfo.Host));
                requestContext.Logger.ErrorPii(exceptionToThrow);
            }
            else
            {
                requestContext.Logger.InfoPii(exceptionToThrow);
            }

            throw exceptionToThrow;
        }

        private static MsalServiceException ExtractErrorsFromTheResponse(HttpResponse response, ref bool shouldLogAsError)
        {
            // In cases where the end-point is not found (404) response.body will be empty.
            if (string.IsNullOrWhiteSpace(response.Body))
            {
                return null;
            }

            MsalTokenResponse msalTokenResponse = null;

            try
            {
                msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(response.Body);
            }
            catch (JsonException)
            {
                //Throttled responses for client credential flows do not have a parsable response.
                if ((int)response.StatusCode == 429)
                {
                    return MsalServiceExceptionFactory.FromThrottledAuthenticationResponse(response);
                }

                throw;
            }

            if (msalTokenResponse?.Error == null)
            {
                return null;
            }

            // For device code flow, AuthorizationPending can occur a lot while waiting
            // for the user to auth via browser and this causes a lot of error noise in the logs.
            // So suppress this particular case to an Info so we still see the data but don't
            // log it as an error since it's expected behavior while waiting for the user.
            if (string.Compare(msalTokenResponse.Error, OAuth2Error.AuthorizationPending,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                shouldLogAsError = false;
            }

            return MsalServiceExceptionFactory.FromHttpResponse(
                msalTokenResponse.Error,
                msalTokenResponse.ErrorDescription,
                response);
        }

        private Uri AddExtraQueryParams(Uri endPoint)
        {
            var endpointUri = new UriBuilder(endPoint);
            string extraQp = _queryParameters.ToQueryParameter();
            endpointUri.AppendQueryParameters(extraQp);

            return endpointUri.Uri;
        }

        private static void VerifyCorrelationIdHeaderInResponse(
            IDictionary<string, string> headers,
            RequestContext requestContext)
        {
            foreach (string responseHeaderKey in headers.Keys)
            {
                string trimmedKey = responseHeaderKey.Trim();
                if (string.Compare(trimmedKey, OAuth2Header.CorrelationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string correlationIdHeader = headers[trimmedKey].Trim();
                    if (string.Compare(
                            correlationIdHeader,
                            requestContext.CorrelationId.ToString(),
                            StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        requestContext.Logger.WarningPii(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Returned correlation id '{0}' does not match the sent correlation id '{1}'",
                                correlationIdHeader,
                                requestContext.CorrelationId),
                            "Returned correlation id does not match the sent correlation id");
                    }

                    break;
                }
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.TelemetryCore;
using System.Net;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon;

namespace Microsoft.Identity.Client.OAuth2
{
    /// <summary>
    /// Responsible for talking to the /token endpoint
    /// </summary>
    internal class TokenClient
    {
        private readonly AuthenticationRequestParameters _requestParams;
        private readonly IServiceBundle _serviceBundle;
        private readonly OAuth2Client _oAuth2Client;

        /// <summary>
        /// Used to avoid sending duplicate "last request" telemetry
        /// from a multi-threaded environment
        /// </summary>
        private volatile bool _requestInProgress = false;

        public TokenClient(AuthenticationRequestParameters requestParams)
        {
            _requestParams = requestParams ?? throw new ArgumentNullException(nameof(requestParams));
            _serviceBundle = _requestParams.RequestContext.ServiceBundle;

            _oAuth2Client = new OAuth2Client(
               _serviceBundle.DefaultLogger,
               _serviceBundle.HttpManager,
               _serviceBundle.MatsTelemetryManager);
        }

        public async Task<MsalTokenResponse> SendTokenRequestAsync(
            IDictionary<string, string> additionalBodyParameters,
            string scopeOverride = null,
            string tokenEndpointOverride = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string tokenEndpoint = tokenEndpointOverride ?? _requestParams.Endpoints.TokenEndpoint;
            string scopes = !string.IsNullOrEmpty(scopeOverride) ? scopeOverride : GetDefaultScopes(_requestParams.Scope);
            AddBodyParamsAndHeaders(additionalBodyParameters, scopes);
            AddThrottlingHeader();

            _serviceBundle.ThrottlingManager.TryThrottle(_requestParams, _oAuth2Client.GetBodyParameters());

            MsalTokenResponse response;
            try
            {
                response = await SendHttpAndClearTelemetryAsync(tokenEndpoint)
                    .ConfigureAwait(false);
            }
            catch (MsalServiceException e)
            {
                _serviceBundle.ThrottlingManager.RecordException(_requestParams, _oAuth2Client.GetBodyParameters(), e);
                throw;
            }

            if (string.IsNullOrEmpty(response.Scope))
            {
                response.Scope = _requestParams.Scope.AsSingleString();
                _requestParams.RequestContext.Logger.Info(
                    "ScopeSet was missing from the token response, so using developer provided scopes in the result. ");
            }

            if (!string.IsNullOrEmpty(response.TokenType) &&
                !string.Equals(
                    response.TokenType,
                    _requestParams.AuthenticationScheme.AccessTokenType,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new MsalClientException(
                    MsalError.TokenTypeMismatch,
                    MsalErrorMessage.TokenTypeMismatch(
                        _requestParams.AuthenticationScheme.AccessTokenType,
                        response.TokenType));
            }

            return response;
        }

        /// <summary>
        /// A client side library needs to communicate to the server side that 
        /// it has implemented enforcement of HTTP 429 and Retry-After header.
        /// Because if the server-side detects loops, then it can break the loop by sending 
        /// either HTTP 429 or Retry-After header with a different HTTP status.
        /// Right now, the server side breaks the loops by invalid_grant response, 
        /// which breaks protocol under some condition and also causes unexplained prompt.
        /// </summary>
        private void AddThrottlingHeader()
        {
            _oAuth2Client.AddHeader(
                ThrottleCommon.ThrottleRetryAfterHeaderName,
                ThrottleCommon.ThrottleRetryAfterHeaderValue);
        }

        private void AddBodyParamsAndHeaders(IDictionary<string, string> additionalBodyParameters, string scopes)
        {
            _oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientId, _requestParams.ClientId);
            _oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientInfo, "1");


            if (_requestParams.ClientCredential != null)
            {
                Dictionary<string, string> ccBodyParameters = ClientCredentialHelper.CreateClientCredentialBodyParameters(
                    _requestParams.RequestContext.Logger,
                    _serviceBundle.PlatformProxy.CryptographyManager,
                    _requestParams.ClientCredential,
                    _requestParams.ClientId,
                    _requestParams.Endpoints,
                    _requestParams.SendX5C);

                foreach (var entry in ccBodyParameters)
                {
                    _oAuth2Client.AddBodyParameter(entry.Key, entry.Value);
                }
            }

            _oAuth2Client.AddBodyParameter(OAuth2Parameter.Scope, scopes);
            _oAuth2Client.AddBodyParameter(OAuth2Parameter.Claims, _requestParams.ClaimsAndClientCapabilities);

            foreach (var kvp in additionalBodyParameters)
            {
                _oAuth2Client.AddBodyParameter(kvp.Key, kvp.Value);
            }

            foreach (var kvp in _requestParams.AuthenticationScheme.GetTokenRequestParams())
            {
                _oAuth2Client.AddBodyParameter(kvp.Key, kvp.Value);
            }

            _oAuth2Client.AddHeader(
                TelemetryConstants.XClientCurrentTelemetry,
                _serviceBundle.HttpTelemetryManager.GetCurrentRequestHeader(
                    _requestParams.RequestContext.ApiEvent));

            if (!_requestInProgress)
            {
                _requestInProgress = true;

                _oAuth2Client.AddHeader(
                    TelemetryConstants.XClientLastTelemetry,
                    _serviceBundle.HttpTelemetryManager.GetLastRequestHeader());
            }

            //Signaling that the client can perform PKey Auth on supported platforms
            if (DeviceAuthHelper.CanOSPerformPKeyAuth())
            {
                _oAuth2Client.AddHeader(PKeyAuthConstants.DeviceAuthHeaderName, PKeyAuthConstants.DeviceAuthHeaderValue);
            }
        }

        private async Task<MsalTokenResponse> SendHttpAndClearTelemetryAsync(string tokenEndpoint)
        {
            UriBuilder builder = new UriBuilder(tokenEndpoint);

            try
            {
                builder.AppendQueryParameters(_requestParams.ExtraQueryParameters);

                MsalTokenResponse msalTokenResponse =
                    await _oAuth2Client
                        .GetTokenAsync(builder.Uri,
                            _requestParams.RequestContext)
                        .ConfigureAwait(false);

                // Clear failed telemetry data as we've just sent it
                _serviceBundle.HttpTelemetryManager.ResetPreviousUnsentData();

                return msalTokenResponse;
            }
            catch (MsalServiceException ex)
            {
                if (!ex.IsAadUnavailable())
                {
                    // Clear failed telemetry data as we've just sent it ... 
                    // even if we received an error from the server, 
                    // telemetry would have been recorded
                    _serviceBundle.HttpTelemetryManager.ResetPreviousUnsentData();
                }

                if (ex.StatusCode == (int)HttpStatusCode.Unauthorized)
                {
                    string responseHeader = string.Empty;
                    var isChallenge = _serviceBundle.DeviceAuthManager.TryCreateDeviceAuthChallengeResponseAsync(ex.Headers, builder.Uri, out responseHeader);
                    if (isChallenge)
                    {
                        //Injecting PKeyAuth response here and replaying request to attempt device auth
                        _oAuth2Client.AddHeader("Authorization", responseHeader);

                        return await _oAuth2Client.GetTokenAsync(builder.Uri, _requestParams.RequestContext, false).ConfigureAwait(false);
                    }
                }

                throw;
            }
            finally
            {
                _requestInProgress = false;
            }
        }

        private static string GetDefaultScopes(ISet<string> inputScope)
        {
            // OAuth spec states that scopes are case sensitive, but 
            // merge the reserved scopes in a case insensitive way, to 
            // avoid sending things like "openid OpenId" (note that EVO is tolerant of this)
            SortedSet<string> set = new SortedSet<string>(
                inputScope.ToArray(),
                StringComparer.OrdinalIgnoreCase);

            set.UnionWith(OAuth2Value.ReservedScopes);
            return set.AsSingleString();
        }
    }
}

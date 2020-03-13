// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.TelemetryCore;

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

        public TokenClient(AuthenticationRequestParameters requestParams)
        {
            _requestParams = requestParams ?? throw new ArgumentNullException(nameof(requestParams));
            _serviceBundle = _requestParams.RequestContext.ServiceBundle;

            _oAuth2Client = new OAuth2Client(
               _serviceBundle.DefaultLogger,
               _serviceBundle.HttpManager,
               _serviceBundle.TelemetryManager);
        }

        public async Task<MsalTokenResponse> SendTokenRequestAsync(
            IDictionary<string, string> additionalBodyParameters,
            string scopeOverride = null,
            string tokenEndpointOverride = null,
            CancellationToken cancellationToken = default)
        {
            string tokenEndpoint = tokenEndpointOverride ?? _requestParams.Endpoints.TokenEndpoint;
            string scopes = !string.IsNullOrEmpty(scopeOverride) ? scopeOverride: GetDefaultScopes(_requestParams.Scope);
            _oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientId, _requestParams.ClientId);
            _oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientInfo, "1");


#if DESKTOP || NETSTANDARD1_3 || NET_CORE
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
#endif

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

            
            _oAuth2Client.AddHttpTelemetryToHeaders(TelemetryConstants.XClientLastTelemetry, _serviceBundle.TelemetryManager.FetchAndResetPreviousHttpTelemetryContent());
            _oAuth2Client.AddHttpTelemetryToHeaders(TelemetryConstants.XClientCurrentTelemetry, _serviceBundle.TelemetryManager.FetchCurrentHttpTelemetryContent(_requestParams.RequestContext.ApiEvent));

            MsalTokenResponse response = await SendHttpMessageAsync(tokenEndpoint)
                .ConfigureAwait(false);

            if (!string.Equals(
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

        private async Task<MsalTokenResponse> SendHttpMessageAsync(string tokenEndpoint)
        {
            UriBuilder builder = new UriBuilder(tokenEndpoint);
            builder.AppendQueryParameters(_requestParams.ExtraQueryParameters);
            MsalTokenResponse msalTokenResponse =
                await _oAuth2Client
                    .GetTokenAsync(builder.Uri,
                        _requestParams.RequestContext)
                    .ConfigureAwait(false);

            if (string.IsNullOrEmpty(msalTokenResponse.Scope))
            {
                msalTokenResponse.Scope = _requestParams.Scope.AsSingleString();
                _requestParams.RequestContext.Logger.Info("ScopeSet was missing from the token response, so using developer provided scopes in the result. ");
            }

            //Request was successful. Clear telemetry data
            _serviceBundle.TelemetryManager.ClearHttpTelemetryData();

            return msalTokenResponse;
        }

        private static string GetDefaultScopes(ISet<string> inputScope)
        {
            // OAuth spec states that scopes are case sensitive, but 
            // merge the reserved scopes in a case insensitive way, to 
            // avoid sending things like "openid OpenId" (note that EVO is tollerant of this)
            SortedSet<string> set = new SortedSet<string>(
                inputScope.ToArray(),
                StringComparer.OrdinalIgnoreCase);

            set.UnionWith(OAuth2Value.ReservedScopes);
            return set.AsSingleString();
        }
    }
}

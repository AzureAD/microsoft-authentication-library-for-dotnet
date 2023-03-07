// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class DeviceCodeRequest : RequestBase
    {
        private readonly AcquireTokenWithDeviceCodeParameters _deviceCodeParameters;

        public DeviceCodeRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenWithDeviceCodeParameters deviceCodeParameters)
            : base(serviceBundle, authenticationRequestParameters, deviceCodeParameters)
        {
            _deviceCodeParameters = deviceCodeParameters;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);

            var client = new OAuth2Client(ServiceBundle.ApplicationLogger, ServiceBundle.HttpManager);

            var deviceCodeScopes = new HashSet<string>();
            deviceCodeScopes.UnionWith(AuthenticationRequestParameters.Scope);
            deviceCodeScopes.Add(OAuth2Value.ScopeOfflineAccess);
            deviceCodeScopes.Add(OAuth2Value.ScopeProfile);
            deviceCodeScopes.Add(OAuth2Value.ScopeOpenId);

            client.AddBodyParameter(OAuth2Parameter.ClientId, AuthenticationRequestParameters.AppConfig.ClientId);
            client.AddBodyParameter(OAuth2Parameter.Scope, deviceCodeScopes.AsSingleString());
            client.AddBodyParameter(OAuth2Parameter.Claims, AuthenticationRequestParameters.ClaimsAndClientCapabilities);

            var deviceCodeEndpoint = await AuthenticationRequestParameters.Authority.GetDeviceCodeEndpointAsync(
                AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);

            var builder = new UriBuilder(deviceCodeEndpoint);
            builder.AppendQueryParameters(AuthenticationRequestParameters.ExtraQueryParameters);

            var response = await client.ExecuteRequestAsync<DeviceCodeResponse>(
                               builder.Uri,
                               HttpMethod.Post,
                               AuthenticationRequestParameters.RequestContext, 
                               // Normally AAD responds with an error HTTP code, but /devicecode endpoint sends errors on 200OK
                               expectErrorsOn200OK: true).ConfigureAwait(false);

            var deviceCodeResult = response.GetResult(AuthenticationRequestParameters.AppConfig.ClientId, deviceCodeScopes);
            await _deviceCodeParameters.DeviceCodeResultCallback(deviceCodeResult).ConfigureAwait(false);

            var msalTokenResponse = await WaitForTokenResponseAsync(deviceCodeResult, cancellationToken).ConfigureAwait(false);
            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }

        private async Task<MsalTokenResponse> WaitForTokenResponseAsync(
            DeviceCodeResult deviceCodeResult,
            CancellationToken cancellationToken)
        {
            var timeRemaining = deviceCodeResult.ExpiresOn - DateTimeOffset.UtcNow;

            while (timeRemaining.TotalSeconds > 0.0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                try
                {
                    var tokenEndpoint = await AuthenticationRequestParameters.GetTokenEndpointAsync(AuthenticationRequestParameters.RequestContext)
                        .ConfigureAwait(false);

                    var tokenResponse = await SendTokenRequestAsync(
                        tokenEndpoint,
                        GetBodyParameters(deviceCodeResult),
                        cancellationToken).ConfigureAwait(false);

                    Metrics.IncrementTotalAccessTokensFromIdP();
                    return tokenResponse;
                }
                catch (MsalServiceException ex)
                {
                    // TODO: handle 429 concerns here and back off farther if needed.
                    if (ex.ErrorCode.Equals(OAuth2Error.AuthorizationPending, StringComparison.OrdinalIgnoreCase))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(deviceCodeResult.Interval), cancellationToken)
                                  .ConfigureAwait(false);
                        timeRemaining = deviceCodeResult.ExpiresOn - DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            throw new MsalClientException(MsalError.CodeExpired, "Verification code expired before contacting the server");
        }

        private Dictionary<string, string> GetBodyParameters(DeviceCodeResult deviceCodeResult)
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.ClientInfo] = "1",
                [OAuth2Parameter.GrantType] = OAuth2GrantType.DeviceCode,
                [OAuth2Parameter.DeviceCode] = deviceCodeResult.DeviceCode,
            };
            return dict;
        }

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            return null;
        }
    }
}

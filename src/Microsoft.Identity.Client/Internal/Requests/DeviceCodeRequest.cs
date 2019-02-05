// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
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

        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);

            var client = new OAuth2Client(ServiceBundle.DefaultLogger, ServiceBundle.HttpManager, ServiceBundle.TelemetryManager);

            var deviceCodeScopes = new HashSet<string>();
            deviceCodeScopes.UnionWith(AuthenticationRequestParameters.Scope);
            deviceCodeScopes.Add(OAuth2Value.ScopeOfflineAccess);
            deviceCodeScopes.Add(OAuth2Value.ScopeProfile);
            deviceCodeScopes.Add(OAuth2Value.ScopeOpenId);

            client.AddBodyParameter(OAuth2Parameter.ClientId, AuthenticationRequestParameters.ClientId);
            client.AddBodyParameter(OAuth2Parameter.Scope, deviceCodeScopes.AsSingleString());
            client.AddQueryParameter(OAuth2Parameter.Claims, AuthenticationRequestParameters.Claims);


            // Talked with Shiung, devicecode will be added to the discovery endpoint "soon".
            // Fow now, the string replace is correct.
            // TODO: We should NOT be talking to common, need to work with henrik/bogdan on why /common is being set
            // as default for msal.
            string deviceCodeEndpoint = AuthenticationRequestParameters.Endpoints.TokenEndpoint
                                                                       .Replace("token", "devicecode").Replace(
                                                                           "common",
                                                                           "organizations");

            var builder = new UriBuilder(deviceCodeEndpoint);
            builder.AppendQueryParameters(AuthenticationRequestParameters.ExtraQueryParameters);

            var response = await client.ExecuteRequestAsync<DeviceCodeResponse>(
                               builder.Uri,
                               HttpMethod.Post,
                               AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);

            var deviceCodeResult = response.GetResult(AuthenticationRequestParameters.ClientId, deviceCodeScopes);
            await _deviceCodeParameters.DeviceCodeResultCallback(deviceCodeResult).ConfigureAwait(false);

            var msalTokenResponse = await WaitForTokenResponseAsync(deviceCodeResult, cancellationToken).ConfigureAwait(false);
            return CacheTokenResponseAndCreateAuthenticationResult(msalTokenResponse);
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
                    // TODO: once we have a devicecode discovery endpoint, we should remove this modification...
                    return await SendTokenRequestAsync(
                                   AuthenticationRequestParameters.Endpoints.TokenEndpoint.Replace("common", "organizations"),
                                   GetBodyParameters(deviceCodeResult), cancellationToken)
                               .ConfigureAwait(false);
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
                [OAuth2Parameter.GrantType] = OAuth2GrantType.DeviceCode,
                [OAuth2Parameter.DeviceCode] = deviceCodeResult.DeviceCode,
            };
            return dict;
        }
    }
}
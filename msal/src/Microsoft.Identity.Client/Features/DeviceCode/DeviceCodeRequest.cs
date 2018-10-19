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
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.Telemetry;

namespace Microsoft.Identity.Client.Features.DeviceCode
{
    internal class DeviceCodeRequest : RequestBase
    {
        private readonly Func<DeviceCodeResult, Task> _deviceCodeResultCallback;

        public DeviceCodeRequest(
            IHttpManager httpManager,
            ICryptographyManager cryptographyManager,
            AuthenticationRequestParameters authenticationRequestParameters,
            ApiEvent.ApiIds apiId,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
            : base(httpManager, cryptographyManager, authenticationRequestParameters, apiId)
        {
            _deviceCodeResultCallback = deviceCodeResultCallback;
        }

        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);

            var client = new OAuth2Client(HttpManager);

            var deviceCodeScopes = new HashSet<string>();
            deviceCodeScopes.UnionWith(AuthenticationRequestParameters.Scope);
            deviceCodeScopes.Add(OAuth2Value.ScopeOfflineAccess);
            deviceCodeScopes.Add(OAuth2Value.ScopeProfile);
            deviceCodeScopes.Add(OAuth2Value.ScopeOpenId);

            client.AddBodyParameter(OAuth2Parameter.ClientId, AuthenticationRequestParameters.ClientId);
            client.AddBodyParameter(OAuth2Parameter.Scope, deviceCodeScopes.AsSingleString());

            // Talked with Shiung, devicecode will be added to the discovery endpoint "soon".
            // Fow now, the string replace is correct.
            // TODO: We should NOT be talking to common, need to work with henrik/bogdan on why /common is being set
            // as default for msal.
            string deviceCodeEndpoint = AuthenticationRequestParameters.Authority.TokenEndpoint
                                                                       .Replace("token", "devicecode").Replace(
                                                                           "common",
                                                                           "organizations");

            AuthenticationRequestParameters.Authority.TokenEndpoint =
                AuthenticationRequestParameters.Authority.TokenEndpoint.Replace("common", "organizations");

            var response = await client.ExecuteRequestAsync<DeviceCodeResponse>(
                               new Uri(deviceCodeEndpoint),
                               HttpMethod.Post,
                               AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);

            var deviceCodeResult = response.GetResult(AuthenticationRequestParameters.ClientId, deviceCodeScopes);
            await _deviceCodeResultCallback(deviceCodeResult).ConfigureAwait(false);

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
                    return await SendTokenRequestAsync(GetBodyParameters(deviceCodeResult), cancellationToken)
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
                [OAuth2Parameter.Code] = deviceCodeResult.DeviceCode
            };
            return dict;
        }
    }
}
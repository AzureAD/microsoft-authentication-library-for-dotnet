//------------------------------------------------------------------------------
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
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.OAuth2;

namespace Microsoft.Identity.Client.Features.DeviceCode
{
    internal class DeviceCodeRequest : RequestBase
    {
        private readonly Func<DeviceCodeResult, Task> _deviceCodeResultCallback;
        private DeviceCodeResult _deviceCodeResult;

        public DeviceCodeRequest(
            IHttpManager httpManager,
            ICryptographyManager cryptographyManager,
            AuthenticationRequestParameters authenticationRequestParameters,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
            : base(httpManager, cryptographyManager, authenticationRequestParameters)
        {
            _deviceCodeResultCallback = deviceCodeResultCallback;

            LoadFromCache = false;  // no cache lookup for token
            SupportADFS = false;
            StoreToCache = true;
        }

        internal override async Task PreTokenRequestAsync(CancellationToken cancellationToken)
        {
            await base.PreTokenRequestAsync(cancellationToken).ConfigureAwait(false);

            OAuth2Client client = new OAuth2Client(HttpManager);

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
                .Replace("token", "devicecode")
                .Replace("common", "organizations");

            AuthenticationRequestParameters.Authority.TokenEndpoint = AuthenticationRequestParameters.Authority.TokenEndpoint.Replace("common", "organizations");

            DeviceCodeResponse response = await client.ExecuteRequestAsync<DeviceCodeResponse>(
                new Uri(deviceCodeEndpoint),
                HttpMethod.Post,
                AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);

            _deviceCodeResult = response.GetResult(AuthenticationRequestParameters.ClientId, deviceCodeScopes);
            await _deviceCodeResultCallback(_deviceCodeResult).ConfigureAwait(false);
        }

        protected override async Task SendTokenRequestAsync(CancellationToken cancellationToken)
        {
            TimeSpan timeRemaining = _deviceCodeResult.ExpiresOn - DateTimeOffset.UtcNow;

            while (timeRemaining.TotalSeconds > 0.0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                try
                {
                    await base.SendTokenRequestAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (MsalServiceException ex)
                {
                    // TODO: handle 429 concerns here and back off farther if needed.
                    if (ex.ErrorCode.Equals(OAuth2Error.AuthorizationPending, StringComparison.OrdinalIgnoreCase))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_deviceCodeResult.Interval), cancellationToken).ConfigureAwait(false);
                        timeRemaining = _deviceCodeResult.ExpiresOn - DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            throw new MsalClientException(OAuth2Error.CodeExpired, "Verification code expired before contacting the server");
        }

        protected override void SetAdditionalRequestParameters(OAuth2Client client)
        {
            client.AddBodyParameter(OAuth2Parameter.GrantType, OAuth2GrantType.DeviceCode);
            client.AddBodyParameter(OAuth2Parameter.Code, _deviceCodeResult.DeviceCode);
        }
    }
}

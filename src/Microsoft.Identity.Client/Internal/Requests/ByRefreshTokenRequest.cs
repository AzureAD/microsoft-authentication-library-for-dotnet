﻿// ------------------------------------------------------------------------------
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class ByRefreshTokenRequest : RequestBase
    {
        private string _userProvidedRefreshToken;

        private const string _nullTokenCacheErrorMassage = "Token cache is set to null. Acquire by refresh token requests cannot be executed.";
        private const string _noRefreshTokenInResponse = "Acquire by refresh token request completed, but no refresh token was found";

        public ByRefreshTokenRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            ApiEvent.ApiIds apiId,
            string userProvidedRefreshToken)
        : base(serviceBundle, authenticationRequestParameters, apiId, false)
        {
            _userProvidedRefreshToken = userProvidedRefreshToken;
        }

        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (TokenCache == null)
            {
                throw new MsalUiRequiredException(
                    MsalUiRequiredException.TokenCacheNullError,
                    _nullTokenCacheErrorMassage);
            }

            AuthenticationRequestParameters.RequestContext.Logger.Verbose("Begin acquire token by refresh token...");
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);
            var msalTokenResponse = await SendTokenRequestAsync(GetBodyParameters(_userProvidedRefreshToken), cancellationToken)
                                        .ConfigureAwait(false);

            if (msalTokenResponse.RefreshToken == null)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info(
                    _noRefreshTokenInResponse);
                throw new MsalServiceException(msalTokenResponse.Error, msalTokenResponse.ErrorDescription, null);
            }

            return CacheTokenResponseAndCreateAuthenticationResult(msalTokenResponse);
        }

        private Dictionary<string, string> GetBodyParameters(string refreshTokenSecret)
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = OAuth2GrantType.RefreshToken,
                [OAuth2Parameter.RefreshToken] = refreshTokenSecret
            };

            return dict;
        }
    }
}

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
using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    /// <summary>
    ///     TODO: this should be merged conceptually with MsalTokenResponse...
    /// </summary>
    internal class TokenResponse
    {
        public TokenResponse(IdToken idToken, Credential accessToken, Credential refreshToken)
        {
            IdToken = idToken ?? new IdToken(string.Empty);
            if (accessToken != null)
            {
                AccessToken = accessToken.Secret;
                ExpiresOn = DateTime.UtcNow; // TODO: ToTimePoint(accessToken.ExpiresOn)
                ExtendedExpiresOn = DateTime.UtcNow; // TODO: ToTimePoint(accessToken.ExtendedExpiresOn)
                GrantedScopes = ScopeUtils.SplitScopes(accessToken.Target);
            }

            if (refreshToken != null)
            {
                RefreshToken = refreshToken.Secret;
            }
        }

        public MsalTokenResponse ToMsalTokenResponse()
        {
            return new MsalTokenResponse
            {
                AccessToken = AccessToken,
                ExpiresIn = Convert.ToInt64(DateTime.UtcNow.Subtract(ExpiresOn).TotalSeconds),
                ExtendedExpiresIn = Convert.ToInt64(DateTime.UtcNow.Subtract(ExtendedExpiresOn).TotalSeconds),
                Claims = string.Empty,
                ClientInfo = RawClientInfo,
                IdToken = IdToken.ToString(),
                RefreshToken = RefreshToken,
                Scope = ScopeUtils.JoinScopes(GrantedScopes),
                TokenType = "whatgoeshere",  // TODO: figure out MsalTokenResponse TokenType value(s)
            };
        }

        public TokenResponse(MsalTokenResponse msalTokenResponse)
        {
            AccessToken = msalTokenResponse.AccessToken;
            RefreshToken = msalTokenResponse.RefreshToken;
            // todo: implement me
            throw new NotImplementedException();
        }

        public string AccessToken { get; }
        public DateTime ExpiresOn { get; }
        public DateTime ExtendedExpiresOn { get; }
        public ISet<string> GrantedScopes { get; }
        public ISet<string> DeclinedScopes { get; }
        public IdToken IdToken { get; set; } // TODO: set; here only exists for test...
        public string RefreshToken { get; }
        public string RawClientInfo => ClientInfo?.ToString();
        public string Uid => JsonUtils.GetExistingOrEmptyString(ClientInfo, "uid");
        public string Utid => JsonUtils.GetExistingOrEmptyString(ClientInfo, "utid");
        public bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);
        public bool HasRefreshToken => !string.IsNullOrWhiteSpace(RefreshToken);

        // todo: this is only for testing.  c++ unit tests are reaching in and mucking with this...
        public JObject ClientInfo { get; set; }
    }
}
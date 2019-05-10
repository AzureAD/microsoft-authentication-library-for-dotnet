// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Internal
{
    internal class MobileBrokerTokenResponseClaim : TokenResponseClaim
    {
        public const string TenantIdAndroidBrokerOnly = "tenant_id";
        public const string UpnAndroidBrokerOnly = "username";
        public const string LocalAccountIdAndroidBrokerOnly = "local_account_id";
    }

    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class MobileBrokerTokenResponse : MsalTokenResponse
    {
        [JsonProperty(PropertyName = TokenResponseClaim.Authority)]
        public string AuthorityUrl { get; set; }

        [JsonProperty(PropertyName = MobileBrokerTokenResponseClaim.TenantIdAndroidBrokerOnly)]
        public string TenantId { get; set; }

        [JsonProperty(PropertyName = MobileBrokerTokenResponseClaim.UpnAndroidBrokerOnly)]
        public string Upn { get; set; }

        [JsonProperty(PropertyName = MobileBrokerTokenResponseClaim.LocalAccountIdAndroidBrokerOnly)]
        public string AccountUserId { get; set; }

        internal static MobileBrokerTokenResponse CreateFromAndroidBrokerResponse(string jsonResponse, string correlationId)
        {
            JObject authResult = JObject.Parse(jsonResponse);
            var errorCode = authResult[BrokerResponseConst.BrokerErrorCode]?.ToString();

            if (!string.IsNullOrEmpty(errorCode))
            {
                return new MobileBrokerTokenResponse
                {
                    Error = errorCode,
                    ErrorDescription = authResult[BrokerResponseConst.BrokerErrorMessage]?.ToString(),
                    AuthorityUrl = authResult[BrokerResponseConst.Authority].ToString(),
                    TenantId = authResult[BrokerResponseConst.TenantId].ToString(),
                    Upn = authResult[BrokerResponseConst.UserName].ToString(),
                    AccountUserId = authResult[BrokerResponseConst.LocalAccountId].ToString(),
                };
            }

            MobileBrokerTokenResponse mobileTokenResponse = new MobileBrokerTokenResponse()
            {
                AccessToken = authResult[BrokerResponseConst.AccessToken].ToString(),
                IdToken = authResult[BrokerResponseConst.IdToken].ToString(),
                CorrelationId = correlationId, // Android response does not expose Correlation ID
                Scope = authResult[BrokerResponseConst.AndroidScopes].ToString(), // sadly for iOS this is "scope" and for Android "scopes"
                ExpiresIn = DateTimeHelpers.GetDurationFromNowInSeconds(authResult[BrokerResponseConst.ExpiresOn].ToString()),
                ExtendedExpiresIn = DateTimeHelpers.GetDurationFromNowInSeconds(authResult[BrokerResponseConst.ExtendedExpiresOn].ToString()),
                ClientInfo = authResult[BrokerResponseConst.ClientInfo].ToString(),
                TokenType = authResult[BrokerResponseConst.TokenType]?.ToString() ?? "Bearer",
                TokenSource = TokenSource.Broker,
                AuthorityUrl = authResult[BrokerResponseConst.Authority].ToString(),
                TenantId = authResult[BrokerResponseConst.TenantId].ToString(),
                Upn = authResult[BrokerResponseConst.UserName].ToString(),
                AccountUserId = authResult[BrokerResponseConst.LocalAccountId].ToString(),
            };

            return mobileTokenResponse;
        }
    }
}

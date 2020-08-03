// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.OAuth2
{
    internal class TokenResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string Code = "code";
        public const string TokenType = "token_type";
        public const string AccessToken = "access_token";
        public const string RefreshToken = "refresh_token";
        public const string IdToken = "id_token";
        public const string Scope = "scope";
        public const string ClientInfo = "client_info";
        public const string ExpiresIn = "expires_in";
        public const string CloudInstanceHost = "cloud_instance_host_name";
        public const string CreatedOn = "created_on";
        public const string ExtendedExpiresIn = "ext_expires_in";
        public const string Authority = "authority";
        public const string FamilyId = "foci";
        public const string RefreshIn = "refresh_in";
    }

    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class MsalTokenResponse : OAuth2ResponseBase
    {
        private long _expiresIn;
        private long _extendedExpiresIn;
        private long _refreshIn;

        [JsonProperty(PropertyName = TokenResponseClaim.TokenType)]
        public string TokenType { get; set; }

        [JsonProperty(PropertyName = TokenResponseClaim.AccessToken)]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = TokenResponseClaim.RefreshToken)]
        public string RefreshToken { get; set; }

        [JsonProperty(PropertyName = TokenResponseClaim.Scope)]
        public string Scope { get; set; }

        [JsonProperty(PropertyName = TokenResponseClaim.ClientInfo)]
        public string ClientInfo { get; set; }

        [JsonProperty(PropertyName = TokenResponseClaim.IdToken)]
        public string IdToken { get; set; }

        [JsonProperty(PropertyName = TokenResponseClaim.ExpiresIn)]
        public long ExpiresIn
        {
            get => _expiresIn;
            set
            {
                _expiresIn = value;
                AccessTokenExpiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(_expiresIn);
            }
        }

        [JsonProperty(PropertyName = TokenResponseClaim.ExtendedExpiresIn)]
        public long ExtendedExpiresIn
        {
            get => _extendedExpiresIn;
            set
            {
                _extendedExpiresIn = value;
                AccessTokenExtendedExpiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(_extendedExpiresIn);
            }
        }

        [JsonProperty(PropertyName = TokenResponseClaim.RefreshIn)]
        public long RefreshIn
        {
            get => _refreshIn;
            set
            {
                _refreshIn = value;
                AccessTokenRefreshOn = DateTime.UtcNow + TimeSpan.FromSeconds(_refreshIn);
            }
        }

        /// <summary>
        /// Optional field, FOCI support.
        /// </summary>
        [JsonProperty(PropertyName = TokenResponseClaim.FamilyId)]
        public string FamilyId { get; set; }

        public DateTimeOffset AccessTokenExpiresOn { get; private set; }
        public DateTimeOffset AccessTokenExtendedExpiresOn { get; private set; }

        public DateTimeOffset? AccessTokenRefreshOn { get; private set; }

        public string Authority { get; private set; }

        public TokenSource TokenSource { get; set; }

        public HttpResponse HttpResponse { get; set; }

        internal static MsalTokenResponse CreateFromiOSBrokerResponse(Dictionary<string, string> responseDictionary)
        {
            if  (responseDictionary.TryGetValue(BrokerResponseConst.BrokerErrorCode, out string errorCode))
            {
                return new MsalTokenResponse
                {
                    Error = errorCode,
                    ErrorDescription = CoreHelpers.UrlDecode(responseDictionary[BrokerResponseConst.BrokerErrorDescription])
                };
            }

            var response = new MsalTokenResponse
            {
                Authority = responseDictionary.ContainsKey(BrokerResponseConst.Authority)
                    ? AuthorityInfo.CanonicalizeAuthorityUri(CoreHelpers.UrlDecode(responseDictionary[BrokerResponseConst.Authority]))
                    : null,
                AccessToken = responseDictionary[BrokerResponseConst.AccessToken],
                RefreshToken = responseDictionary.ContainsKey(BrokerResponseConst.RefreshToken)
                    ? responseDictionary[BrokerResponseConst.RefreshToken]
                    : null,
                IdToken = responseDictionary[BrokerResponseConst.IdToken],
                TokenType = BrokerResponseConst.Bearer,
                CorrelationId = responseDictionary[BrokerResponseConst.CorrelationId],
                Scope = responseDictionary[BrokerResponseConst.Scope],
                ExpiresIn = responseDictionary.TryGetValue(BrokerResponseConst.ExpiresOn, out string expiresOn) ?
                                GetExpiresIn(expiresOn) :
                                0,
                ClientInfo = responseDictionary.ContainsKey(BrokerResponseConst.ClientInfo)
                    ? responseDictionary[BrokerResponseConst.ClientInfo]
                    : null,
                TokenSource = TokenSource.Broker
            };

            if (responseDictionary.ContainsKey(TokenResponseClaim.RefreshIn))
            {
                response.RefreshIn = long.Parse(
                    responseDictionary[TokenResponseClaim.RefreshIn], 
                    CultureInfo.InvariantCulture);
            }

            return response;
        }

        /// <remarks>
        /// This method does not belong here - it is more tied to the Android code. However, that code is
        /// not unit testable, and this one is. 
        /// The values of the JSON response are based on 
        /// https://github.com/AzureAD/microsoft-authentication-library-common-for-android/blob/dev/common/src/main/java/com/microsoft/identity/common/internal/broker/BrokerResult.java
        /// </remarks>
        internal static MsalTokenResponse CreateFromAndroidBrokerResponse(string jsonResponse, string correlationId)
        {
            JObject authResult = JObject.Parse(jsonResponse);
            var errorCode = authResult[BrokerResponseConst.BrokerErrorCode]?.ToString();

            if (!string.IsNullOrEmpty(errorCode))
            {
                return new MsalTokenResponse
                {
                    Error = errorCode,
                    ErrorDescription = authResult[BrokerResponseConst.BrokerErrorMessage]?.ToString(),
                };
            }

            MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
            {
                Authority = authResult[BrokerResponseConst.Authority].ToString(),
                AccessToken = authResult[BrokerResponseConst.AccessToken].ToString(),
                IdToken = authResult[BrokerResponseConst.IdToken].ToString(),
                CorrelationId = correlationId, // Android response does not expose Correlation ID
                Scope = authResult[BrokerResponseConst.AndroidScopes].ToString(), // sadly for iOS this is "scope" and for Android "scopes"
                ExpiresIn = GetExpiresIn(authResult[BrokerResponseConst.ExpiresOn].ToString()),
                ExtendedExpiresIn = GetExpiresIn(authResult[BrokerResponseConst.ExtendedExpiresOn].ToString()),
                ClientInfo = authResult[BrokerResponseConst.ClientInfo].ToString(),
                TokenType = authResult[BrokerResponseConst.TokenType]?.ToString() ?? "Bearer",
                TokenSource = TokenSource.Broker
            };

            return msalTokenResponse;
        }

        private static long GetExpiresIn(string expiresOn)
        {
            if (string.IsNullOrEmpty(expiresOn))
            {
                return 0;
            }

            long expiresOnUnixTimestamp = long.Parse(expiresOn, CultureInfo.InvariantCulture);
            return expiresOnUnixTimestamp - CoreHelpers.CurrDateTimeInUnixTimestamp();
        }
    }    
}

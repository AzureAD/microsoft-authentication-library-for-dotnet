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
using Microsoft.Identity.Client.Core;
using System.Text;

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
    internal class MsalTokenResponse : OAuth2ResponseBase, IJsonSerializable<MsalTokenResponse>
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

        [JsonIgnore]
        public string WamAccountId { get; set; }

        [JsonIgnore]
        public DateTimeOffset AccessTokenExpiresOn { get; private set; }

        [JsonIgnore]
        public DateTimeOffset AccessTokenExtendedExpiresOn { get; private set; }

        [JsonIgnore]
        public DateTimeOffset? AccessTokenRefreshOn { get; private set; }

        [JsonIgnore]
        public TokenSource TokenSource { get; set; }

        [JsonIgnore]
        public HttpResponse HttpResponse { get; set; }

        public new MsalTokenResponse DeserializeFromJson(string json) => DeserializeFromJObject(JObject.Parse(json));

        public new MsalTokenResponse DeserializeFromJObject(JObject jObject)
        {
            TokenType = jObject[TokenResponseClaim.TokenType]?.ToString();
            AccessToken = jObject[TokenResponseClaim.AccessToken]?.ToString();
            RefreshToken = jObject[TokenResponseClaim.RefreshToken]?.ToString();
            Scope = jObject[TokenResponseClaim.Scope]?.ToString();
            ClientInfo = jObject[TokenResponseClaim.ClientInfo]?.ToString();
            IdToken = jObject[TokenResponseClaim.IdToken]?.ToString();

            if (jObject[TokenResponseClaim.ExpiresIn] != null)
            {
                ExpiresIn = TryParseLong(jObject[TokenResponseClaim.ExpiresIn].ToString());
            }

            if (jObject[TokenResponseClaim.ExtendedExpiresIn] != null)
            {
                ExtendedExpiresIn = TryParseLong(jObject[TokenResponseClaim.ExtendedExpiresIn].ToString());
            }

            if (jObject[TokenResponseClaim.RefreshIn] != null)
            {
                RefreshIn = TryParseLong(jObject[TokenResponseClaim.RefreshIn].ToString());
            }

            FamilyId = jObject[TokenResponseClaim.FamilyId]?.ToString();
            base.DeserializeFromJObject(jObject);

            long TryParseLong(string stringVal)
            {
                long.TryParse(stringVal, out var val);
                return val;
            }

            return this;
        }

        public new string SerializeToJson() => SerializeToJObject().ToString(Formatting.None);

        public new JObject SerializeToJObject()
        {
            return new JObject(
                new JProperty(TokenResponseClaim.TokenType, TokenType),
                new JProperty(TokenResponseClaim.AccessToken, AccessToken),
                new JProperty(TokenResponseClaim.RefreshToken, RefreshToken),
                new JProperty(TokenResponseClaim.Scope, Scope),
                new JProperty(TokenResponseClaim.ClientInfo, ClientInfo),
                new JProperty(TokenResponseClaim.IdToken, IdToken),
                new JProperty(TokenResponseClaim.ExpiresIn, ExpiresIn),
                new JProperty(TokenResponseClaim.ExtendedExpiresIn, ExtendedExpiresIn),
                new JProperty(TokenResponseClaim.RefreshIn, RefreshIn),
                new JProperty(TokenResponseClaim.FamilyId, FamilyId),
                base.SerializeToJObject().Properties());
        }

        internal static MsalTokenResponse CreateFromiOSBrokerResponse(Dictionary<string, string> responseDictionary)
        {
            if (responseDictionary.TryGetValue(BrokerResponseConst.BrokerErrorCode, out string errorCode))
            {
                return new MsalTokenResponse
                {
                    Error = errorCode,
                    ErrorDescription = CoreHelpers.UrlDecode(responseDictionary[BrokerResponseConst.BrokerErrorDescription])
                };
            }

            var response = new MsalTokenResponse
            {
                AccessToken = responseDictionary[BrokerResponseConst.AccessToken],
                RefreshToken = responseDictionary.ContainsKey(BrokerResponseConst.RefreshToken)
                    ? responseDictionary[BrokerResponseConst.RefreshToken]
                    : null,
                IdToken = responseDictionary[BrokerResponseConst.IdToken],
                TokenType = BrokerResponseConst.Bearer,
                CorrelationId = responseDictionary[BrokerResponseConst.CorrelationId],
                Scope = responseDictionary[BrokerResponseConst.Scope],
                ExpiresIn = responseDictionary.TryGetValue(BrokerResponseConst.ExpiresOn, out string expiresOn) ?
                                CoreHelpers.GetDurationFromNowInSeconds(expiresOn) :
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
                AccessToken = authResult[BrokerResponseConst.AccessToken].ToString(),
                IdToken = authResult[BrokerResponseConst.IdToken].ToString(),
                CorrelationId = correlationId, // Android response does not expose Correlation ID
                Scope = authResult[BrokerResponseConst.AndroidScopes].ToString(), // sadly for iOS this is "scope" and for Android "scopes"
                ExpiresIn = CoreHelpers.GetDurationFromNowInSeconds(authResult[BrokerResponseConst.ExpiresOn].ToString()),
                ExtendedExpiresIn = CoreHelpers.GetDurationFromNowInSeconds(authResult[BrokerResponseConst.ExtendedExpiresOn].ToString()),
                ClientInfo = authResult[BrokerResponseConst.ClientInfo].ToString(),
                TokenType = authResult[BrokerResponseConst.TokenType]?.ToString() ?? "Bearer",
                TokenSource = TokenSource.Broker
            };

            return msalTokenResponse;
        }

        public void Log(ICoreLogger logger, LogLevel logLevel)
        {
            if (logger.IsLoggingEnabled(logLevel))
            {
                StringBuilder withPii = new StringBuilder();
                StringBuilder withoutPii = new StringBuilder();

                withPii.AppendLine("==MsalTokenResponse==");
                withoutPii.AppendLine("==MsalTokenResponse==");

                withPii.AppendLine($"Error: {Error} ErrorDescription: {ErrorDescription}");
                withoutPii.AppendLine($"Error: {Error} ErrorDescription: {ErrorDescription}");
                withPii.AppendLine($"Scopes: {Scope} ");
                withoutPii.AppendLine($"Scopes: {Scope} ");
                withPii.AppendLine($"ExpiresIn: {ExpiresIn} RefreshIn {RefreshIn}");
                withoutPii.AppendLine($"ExpiresIn: {ExpiresIn} RefreshIn {RefreshIn}");

                withoutPii.AppendLine(
                    $"AccessToken {!String.IsNullOrEmpty(AccessToken)} " +
                    $"AccessToken Type {TokenType} " +
                    $"RefreshToken {!String.IsNullOrEmpty(RefreshToken)} " +
                    $"IdToken {!String.IsNullOrEmpty(IdToken)} " +
                    $"ClientInfo {!String.IsNullOrEmpty(ClientInfo)} ");

                withPii.AppendLine(
                    $"AccessToken {!String.IsNullOrEmpty(AccessToken)} " +
                    $"AccessToken Type {TokenType} " +
                    $"RefreshToken {!String.IsNullOrEmpty(RefreshToken)} " +
                    $"IdToken {!String.IsNullOrEmpty(IdToken)} " +
                    $"ClientInfo {ClientInfo} ");

                withPii.AppendLine($"FamilyId: {FamilyId} WamAccountId {!string.IsNullOrEmpty(WamAccountId)}");
                withoutPii.AppendLine($"FamilyId: {FamilyId} WamAccountId {!string.IsNullOrEmpty(WamAccountId)}");

                logger.Log(logLevel, withPii.ToString(), withoutPii.ToString());
            }
        }
    }
}

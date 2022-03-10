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
using Microsoft.Identity.Client.Internal;

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
        public const string SpaCode = "spa_code";
        public const string ErrorSubcode = "error_subcode";
        public const string ErrorSubcodeCancel = "cancel";

        public const string TenantId = "tenant_id";
        public const string Upn = "username";
        public const string LocalAccountId = "local_account_id";
    }

    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class MsalTokenResponse : OAuth2ResponseBase
    {
        private const string iOSBrokerErrorMetadata = "error_metadata";
        private const string iOSBrokerHomeAccountId = "home_account_id";
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
        public long ExpiresIn { get; set; }

        [JsonProperty(PropertyName = TokenResponseClaim.ExtendedExpiresIn)]
        public long ExtendedExpiresIn { get; set; }

        [JsonProperty(PropertyName = TokenResponseClaim.RefreshIn)]
        public long? RefreshIn { get; set; }

        /// <summary>
        /// Optional field, FOCI support.
        /// </summary>
        [JsonProperty(PropertyName = TokenResponseClaim.FamilyId)]
        public string FamilyId { get; set; }

        [JsonProperty(PropertyName = TokenResponseClaim.SpaCode)]
        public string SpaAuthCode { get; set; }

        [JsonProperty(PropertyName = TokenResponseClaim.Authority)]
        public string AuthorityUrl { get; set; }

        public string TenantId { get; set; }

        public string Upn { get; set; }

        public string AccountUserId { get; set; }

        public string WamAccountId { get; set; }

        public TokenSource TokenSource { get; set; }

        public HttpResponse HttpResponse { get; set; }

        internal static MsalTokenResponse CreateFromiOSBrokerResponse(Dictionary<string, string> responseDictionary)
        {
            if (responseDictionary.TryGetValue(BrokerResponseConst.BrokerErrorCode, out string errorCode))
            {
                string original = responseDictionary[MsalTokenResponse.iOSBrokerErrorMetadata];
                string dataUnescaped = Uri.UnescapeDataString(original);
                Dictionary<string, string> dictionary = Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dataUnescaped);
                string homeAcctId = null;
                dictionary.TryGetValue(MsalTokenResponse.iOSBrokerHomeAccountId, out homeAcctId);
                return new MsalTokenResponse
                {
                    Error = errorCode,
                    ErrorDescription = CoreHelpers.UrlDecode(responseDictionary[BrokerResponseConst.BrokerErrorDescription]),
                    SubError = responseDictionary[OAuth2ResponseBaseClaim.SubError],
                    AccountUserId = homeAcctId != null ? AccountId.ParseFromString(homeAcctId).ObjectId : null,
                    TenantId = homeAcctId != null ?  AccountId.ParseFromString(homeAcctId).TenantId : null,
                    Upn = dictionary[TokenResponseClaim.Upn],
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
                                DateTimeHelpers.GetDurationFromNowInSeconds(expiresOn) :
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
                    AuthorityUrl = authResult[BrokerResponseConst.Authority]?.ToString(),
                    TenantId = authResult[BrokerResponseConst.TenantId]?.ToString(),
                    Upn = authResult[BrokerResponseConst.UserName]?.ToString(),
                    AccountUserId = authResult[BrokerResponseConst.LocalAccountId]?.ToString(),
                };
            }

            MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
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
                AuthorityUrl = authResult[BrokerResponseConst.Authority]?.ToString(),
                TenantId = authResult[BrokerResponseConst.TenantId]?.ToString(),
                Upn = authResult[BrokerResponseConst.UserName]?.ToString(),
                AccountUserId = authResult[BrokerResponseConst.LocalAccountId]?.ToString(),
            };

            return msalTokenResponse;
        }

        public void Log(ICoreLogger logger, LogLevel logLevel)
        {
            if (logger.IsLoggingEnabled(logLevel))
            {
                StringBuilder withPii = new StringBuilder();
                StringBuilder withoutPii = new StringBuilder();

                withPii.AppendLine($"{Environment.NewLine}[MsalTokenResponse]");
                withPii.AppendLine($"Error: {Error}");
                withPii.AppendLine($"ErrorDescription: {ErrorDescription}");
                withPii.AppendLine($"Scopes: {Scope} ");
                withPii.AppendLine($"ExpiresIn: {ExpiresIn}");
                withPii.AppendLine($"RefreshIn: {RefreshIn}");
                withPii.AppendLine($"AccessToken returned: {!string.IsNullOrEmpty(AccessToken)}");
                withPii.AppendLine($"AccessToken Type: {TokenType}");
                withPii.AppendLine($"RefreshToken returned: {!string.IsNullOrEmpty(RefreshToken)}");
                withPii.AppendLine($"IdToken returned: {!string.IsNullOrEmpty(IdToken)}");
                withPii.AppendLine($"ClientInfo: {ClientInfo}");
                withPii.AppendLine($"FamilyId: {FamilyId}");
                withPii.AppendLine($"WamAccountId exists: {!string.IsNullOrEmpty(WamAccountId)}");

                withoutPii.AppendLine($"{Environment.NewLine}[MsalTokenResponse]");
                withoutPii.AppendLine($"Error: {Error}");
                withoutPii.AppendLine($"ErrorDescription: {ErrorDescription}");
                withoutPii.AppendLine($"Scopes: {Scope} ");
                withoutPii.AppendLine($"ExpiresIn: {ExpiresIn}");
                withoutPii.AppendLine($"RefreshIn: {RefreshIn}");
                withoutPii.AppendLine($"AccessToken returned: {!string.IsNullOrEmpty(AccessToken)}");
                withoutPii.AppendLine($"AccessToken Type: {TokenType}");
                withoutPii.AppendLine($"RefreshToken returned: {!string.IsNullOrEmpty(RefreshToken)}");
                withoutPii.AppendLine($"IdToken returned: {!string.IsNullOrEmpty(IdToken)}");
                withoutPii.AppendLine($"ClientInfo returned: {!string.IsNullOrEmpty(ClientInfo)}");
                withoutPii.AppendLine($"FamilyId: {FamilyId}");
                withoutPii.AppendLine($"WamAccountId exists: {!string.IsNullOrEmpty(WamAccountId)}");

                logger.Log(logLevel, withPii.ToString(), withoutPii.ToString());
            }
        }
    }
}

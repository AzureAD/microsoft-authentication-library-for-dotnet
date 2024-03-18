// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Identity.Client.Platforms.net6;
using JObject = System.Text.Json.Nodes.JsonObject;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;
#endif

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
        public const string ErrorSubcode = "error_subcode";
        public const string ErrorSubcodeCancel = "cancel";

        public const string TenantId = "tenant_id";
        public const string Upn = "username";
        public const string LocalAccountId = "local_account_id";

        // Hybrid SPA - see https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3994
        public const string SpaCode = "spa_code";

    }

    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class MsalTokenResponse : OAuth2ResponseBase
    {
        public MsalTokenResponse()
        {

        }

        private const string iOSBrokerErrorMetadata = "error_metadata";
        private const string iOSBrokerHomeAccountId = "home_account_id";

        // Due to AOT + JSON serializer https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4082
        // disable this functionality (better fix would be to move to System.Text.Json)
#if !__MOBILE__
        // All properties not explicitly defined are added to this dictionary
        // See JSON overflow https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/handle-overflow?pivots=dotnet-7-0
#if SUPPORTS_SYSTEM_TEXT_JSON
        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
#else
        [JsonExtensionData]
        public Dictionary<string, JToken> ExtensionData { get; set; }
#endif
#endif
        // Exposes only scalar properties from ExtensionData
        public IReadOnlyDictionary<string, string> CreateExtensionDataStringMap()
        {
#if __MOBILE__
            return CollectionHelpers.GetEmptyDictionary<string, string>();
#else
            if (ExtensionData == null || ExtensionData.Count == 0)
            {
                return CollectionHelpers.GetEmptyDictionary<string, string>();
            }

            Dictionary<string, string> stringExtensionData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

#if SUPPORTS_SYSTEM_TEXT_JSON
            foreach (KeyValuePair<string, JsonElement> item in ExtensionData)
            {
                if (item.Value.ValueKind == JsonValueKind.String ||
                   item.Value.ValueKind == JsonValueKind.Number ||
                   item.Value.ValueKind == JsonValueKind.True ||
                   item.Value.ValueKind == JsonValueKind.False ||
                   item.Value.ValueKind == JsonValueKind.Null)
                {
                    stringExtensionData.Add(item.Key, item.Value.ToString());
                }
            }
#else
            foreach (KeyValuePair<string, JToken> item in ExtensionData)
            {
                if (item.Value.Type == JTokenType.String ||
                   item.Value.Type == JTokenType.Uri ||
                   item.Value.Type == JTokenType.Boolean ||
                   item.Value.Type == JTokenType.Date ||
                   item.Value.Type == JTokenType.Float ||
                   item.Value.Type == JTokenType.Guid ||
                   item.Value.Type == JTokenType.Integer ||
                   item.Value.Type == JTokenType.TimeSpan ||
                   item.Value.Type == JTokenType.Null)
                {
                    stringExtensionData.Add(item.Key, item.Value.ToString());
                }
            }
#endif
            return stringExtensionData;
#endif
        }

        [JsonProperty(TokenResponseClaim.TokenType)]
        public string TokenType { get; set; }

        [JsonProperty(TokenResponseClaim.AccessToken)]
        public string AccessToken { get; set; }

        [JsonProperty(TokenResponseClaim.RefreshToken)]
        public string RefreshToken { get; set; }

        [JsonProperty(TokenResponseClaim.Scope)]
        public string Scope { get; set; }

        [JsonProperty(TokenResponseClaim.ClientInfo)]
        public string ClientInfo { get; set; }

        [JsonProperty(TokenResponseClaim.IdToken)]
        public string IdToken { get; set; }

        [JsonProperty(TokenResponseClaim.ExpiresIn)]
#if SUPPORTS_SYSTEM_TEXT_JSON
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
#endif
        public long ExpiresIn { get; set; }

#if SUPPORTS_SYSTEM_TEXT_JSON
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
#endif
        [JsonProperty(TokenResponseClaim.ExtendedExpiresIn)]
        public long ExtendedExpiresIn { get; set; }

#if SUPPORTS_SYSTEM_TEXT_JSON
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
#endif
        [JsonProperty(TokenResponseClaim.RefreshIn)]
        public long? RefreshIn { get; set; }

        /// <summary>
        /// Optional field, FOCI support.
        /// </summary>
        [JsonProperty(TokenResponseClaim.FamilyId)]
        public string FamilyId { get; set; }

        [JsonProperty(TokenResponseClaim.SpaCode)]
        public string SpaAuthCode { get; set; }

        [JsonProperty(TokenResponseClaim.Authority)]
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
                string metadataOriginal = responseDictionary.TryGetValue(MsalTokenResponse.iOSBrokerErrorMetadata, out string iOSBrokerErrorMetadata) ? iOSBrokerErrorMetadata : null;
                Dictionary<string, string> metadataDictionary = null;

                if (metadataOriginal != null)
                {
                    string brokerMetadataJson = Uri.UnescapeDataString(metadataOriginal);
#if SUPPORTS_SYSTEM_TEXT_JSON
                    metadataDictionary = new Dictionary<string, string>();
                    foreach (var item in JsonDocument.Parse(brokerMetadataJson).RootElement.EnumerateObject())
                    {
                        metadataDictionary.Add(item.Name, item.Value.GetString());
                    }
#else
                    metadataDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(brokerMetadataJson);
#endif
                }

                string homeAcctId = null;
                metadataDictionary?.TryGetValue(MsalTokenResponse.iOSBrokerHomeAccountId, out homeAcctId);
                return new MsalTokenResponse
                {
                    Error = errorCode,
                    ErrorDescription = responseDictionary.TryGetValue(BrokerResponseConst.BrokerErrorDescription, out string brokerErrorDescription) ? CoreHelpers.UrlDecode(brokerErrorDescription) : string.Empty,
                    SubError = responseDictionary.TryGetValue(OAuth2ResponseBaseClaim.SubError, out string subError) ? subError : string.Empty,
                    AccountUserId = homeAcctId != null ? AccountId.ParseFromString(homeAcctId).ObjectId : null,
                    TenantId = homeAcctId != null ? AccountId.ParseFromString(homeAcctId).TenantId : null,
                    Upn = (metadataDictionary?.ContainsKey(TokenResponseClaim.Upn) ?? false) ? metadataDictionary[TokenResponseClaim.Upn] : null,
                    CorrelationId = responseDictionary.TryGetValue(BrokerResponseConst.CorrelationId, out string correlationId) ? correlationId : null,
                };
            }

            var response = new MsalTokenResponse
            {
                AccessToken = responseDictionary[BrokerResponseConst.AccessToken],
                RefreshToken = responseDictionary.TryGetValue(BrokerResponseConst.RefreshToken, out string refreshToken)
                    ? refreshToken
                    : null,
                IdToken = responseDictionary[BrokerResponseConst.IdToken],
                TokenType = BrokerResponseConst.Bearer,
                CorrelationId = responseDictionary[BrokerResponseConst.CorrelationId],
                Scope = responseDictionary[BrokerResponseConst.Scope],
                ExpiresIn = responseDictionary.TryGetValue(BrokerResponseConst.ExpiresOn, out string expiresOn) ?
                                DateTimeHelpers.GetDurationFromNowInSeconds(expiresOn) :
                                0,
                ClientInfo = responseDictionary.TryGetValue(BrokerResponseConst.ClientInfo, out string clientInfo)
                                ? clientInfo
                                : null,
                TokenSource = TokenSource.Broker
            };

            if (responseDictionary.TryGetValue(TokenResponseClaim.RefreshIn, out string refreshIn))
            {
                response.RefreshIn = long.Parse(
                    refreshIn,
                    CultureInfo.InvariantCulture);
            }

            return response;
        }

        internal static MsalTokenResponse CreateFromManagedIdentityResponse(ManagedIdentityResponse managedIdentityResponse)
        {
            ValidateManagedIdentityResult(managedIdentityResponse);

            long expiresIn = DateTimeHelpers.GetDurationFromNowInSeconds(managedIdentityResponse.ExpiresOn);

            return new MsalTokenResponse
            {
                AccessToken = managedIdentityResponse.AccessToken,
                ExpiresIn = expiresIn,
                TokenType = managedIdentityResponse.TokenType,
                TokenSource = TokenSource.IdentityProvider,
                RefreshIn = InferManagedIdentityRefreshInValue(expiresIn)
            };
        }

        // Compute refresh_in as 1/2 expires_in, but only if expires_in > 2h.
        private static long? InferManagedIdentityRefreshInValue(long expiresIn)

        {
            if (expiresIn > 2 * 3600)
            {
                return expiresIn / 2;
            }

            return null;
        }

        private static void ValidateManagedIdentityResult(ManagedIdentityResponse response)
        {
            if (string.IsNullOrEmpty(response.AccessToken))
            {
                HandleInvalidExternalValueError(nameof(response.AccessToken));
            }

            long expiresIn = DateTimeHelpers.GetDurationFromNowInSeconds(response.ExpiresOn);
            if (expiresIn <= 0)
            {
                HandleInvalidExternalValueError(nameof(response.ExpiresOn));
            }
        }

        internal static MsalTokenResponse CreateFromAppProviderResponse(AppTokenProviderResult tokenProviderResponse)
        {
            ValidateTokenProviderResult(tokenProviderResponse);

            var response = new MsalTokenResponse
            {
                AccessToken = tokenProviderResponse.AccessToken,
                RefreshToken = null,
                IdToken = null,
                TokenType = BrokerResponseConst.Bearer,
                ExpiresIn = tokenProviderResponse.ExpiresInSeconds,
                ClientInfo = null,
                TokenSource = TokenSource.IdentityProvider,
                TenantId = null, // Leave as null so MSAL can use the original request Tid. This is ok for confidential client scenarios
                RefreshIn = tokenProviderResponse.RefreshInSeconds ?? EstimateRefreshIn(tokenProviderResponse.ExpiresInSeconds)
            };

            return response;
        }

        private static long? EstimateRefreshIn(long expiresInSeconds)
        {
            if (expiresInSeconds >= 2 * 3600)
            {
                return expiresInSeconds / 2;
            }

            return null;
        }

        private static void ValidateTokenProviderResult(AppTokenProviderResult TokenProviderResult)
        {
            if (string.IsNullOrEmpty(TokenProviderResult.AccessToken))
            {
                HandleInvalidExternalValueError(nameof(TokenProviderResult.AccessToken));
            }

            if (TokenProviderResult.ExpiresInSeconds == 0 || TokenProviderResult.ExpiresInSeconds < 0)
            {
                HandleInvalidExternalValueError(nameof(TokenProviderResult.ExpiresInSeconds));
            }
        }

        private static void HandleInvalidExternalValueError(string nameOfValue)
        {
            throw new MsalClientException(MsalError.InvalidTokenProviderResponseValue, MsalErrorMessage.InvalidTokenProviderResponseValue(nameOfValue));
        }

        /// <remarks>
        /// This method does not belong here - it is more tied to the Android code. However, that code is
        /// not unit testable, and this one is. 
        /// The values of the JSON response are based on 
        /// https://github.com/AzureAD/microsoft-authentication-library-common-for-android/blob/dev/common/src/main/java/com/microsoft/identity/common/internal/broker/BrokerResult.java
        /// </remarks>
        internal static MsalTokenResponse CreateFromAndroidBrokerResponse(string jsonResponse, string correlationId)
        {
            var authResult = JsonHelper.ParseIntoJsonObject(jsonResponse);
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

        public void Log(ILoggerAdapter logger, LogLevel logLevel)
        {
            if (logger.IsLoggingEnabled(logLevel))
            {
                var withPii =
                    $"""

                     [MsalTokenResponse]
                     Error: {Error}
                     ErrorDescription: {ErrorDescription}
                     Scopes: {Scope}
                     ExpiresIn: {ExpiresIn}
                     RefreshIn: {RefreshIn}
                     AccessToken returned: {!string.IsNullOrEmpty(AccessToken)}
                     AccessToken Type: {TokenType}
                     RefreshToken returned: {!string.IsNullOrEmpty(RefreshToken)}
                     IdToken returned: {!string.IsNullOrEmpty(IdToken)}
                     ClientInfo: {ClientInfo}
                     FamilyId: {FamilyId}
                     WamAccountId exists: {!string.IsNullOrEmpty(WamAccountId)}
                     """;
                var withoutPii =
                    $"""

                     [MsalTokenResponse]
                     Error: {Error}
                     ErrorDescription: {ErrorDescription}
                     Scopes: {Scope}
                     ExpiresIn: {ExpiresIn}
                     RefreshIn: {RefreshIn}
                     AccessToken returned: {!string.IsNullOrEmpty(AccessToken)}
                     AccessToken Type: {TokenType}
                     RefreshToken returned: {!string.IsNullOrEmpty(RefreshToken)}
                     IdToken returned: {!string.IsNullOrEmpty(IdToken)}
                     ClientInfo returned: {!string.IsNullOrEmpty(ClientInfo)}
                     FamilyId: {FamilyId}
                     WamAccountId exists: {!string.IsNullOrEmpty(WamAccountId)}
                     """;

                logger.Log(logLevel, withPii, withoutPii);
            }
        }
    }
}

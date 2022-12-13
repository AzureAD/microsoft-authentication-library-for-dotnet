// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text.Json;
using Microsoft.Identity.Client.Platforms.net6;
using JObject = System.Text.Json.Nodes.JsonObject;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.Utils
{
    internal class MsalTokenResponseHelper
    {
        private const string iOSBrokerErrorMetadata = "error_metadata";
        private const string iOSBrokerHomeAccountId = "home_account_id";

        internal static MsalTokenResponse CreateFromiOSBrokerResponse(Dictionary<string, string> responseDictionary)
        {
            if (responseDictionary.TryGetValue(BrokerResponseConst.BrokerErrorCode, out string errorCode))
            {
                string metadataOriginal = responseDictionary.ContainsKey(iOSBrokerErrorMetadata) ? responseDictionary[iOSBrokerErrorMetadata] : null;
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
                metadataDictionary?.TryGetValue(iOSBrokerHomeAccountId, out homeAcctId);
                return new MsalTokenResponse
                {
                    Error = errorCode,
                    ErrorDescription = responseDictionary.ContainsKey(BrokerResponseConst.BrokerErrorDescription) ? CoreHelpers.UrlDecode(responseDictionary[BrokerResponseConst.BrokerErrorDescription]) : string.Empty,
                    SubError = responseDictionary.ContainsKey(OAuth2ResponseBaseClaim.SubError) ? responseDictionary[OAuth2ResponseBaseClaim.SubError] : string.Empty,
                    AccountUserId = homeAcctId != null ? AccountId.ParseFromString(homeAcctId).ObjectId : null,
                    TenantId = homeAcctId != null ? AccountId.ParseFromString(homeAcctId).TenantId : null,
                    Upn = (metadataDictionary?.ContainsKey(TokenResponseClaim.Upn) ?? false) ? metadataDictionary[TokenResponseClaim.Upn] : null,
                    CorrelationId = responseDictionary.ContainsKey(BrokerResponseConst.CorrelationId) ? responseDictionary[BrokerResponseConst.CorrelationId] : null,
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
    }
}

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
        public const string SpaCode = "spa_code";
        public const string ErrorSubcode = "error_subcode";
        public const string ErrorSubcodeCancel = "cancel";
    }

    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class MsalTokenResponse : OAuth2ResponseBase
    {
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

        public string WamAccountId { get; set; }

        public TokenSource TokenSource { get; set; }

        public HttpResponse HttpResponse { get; set; }

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

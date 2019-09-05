// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.Internal.Broker;

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

    [DataContract]
    internal class MsalTokenResponse : OAuth2ResponseBase
    {
        private long _expiresIn;
        private long _extendedExpiresIn;
        private long _refreshIn;

        [DataMember(Name = TokenResponseClaim.TokenType, IsRequired = false)]
        public string TokenType { get; set; }

        [DataMember(Name = TokenResponseClaim.AccessToken, IsRequired = false)]
        public string AccessToken { get; set; }

        [DataMember(Name = TokenResponseClaim.RefreshToken, IsRequired = false)]
        public string RefreshToken { get; set; }

        [DataMember(Name = TokenResponseClaim.Scope, IsRequired = false)]
        public string Scope { get; set; }

        [DataMember(Name = TokenResponseClaim.ClientInfo, IsRequired = false)]
        public string ClientInfo { get; set; }

        [DataMember(Name = TokenResponseClaim.IdToken, IsRequired = false)]
        public string IdToken { get; set; }

        [DataMember(Name = TokenResponseClaim.ExpiresIn, IsRequired = false)]
        public long ExpiresIn
        {
            get => _expiresIn;
            set
            {
                _expiresIn = value;
                AccessTokenExpiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(_expiresIn);
            }
        }

        [DataMember(Name = TokenResponseClaim.ExtendedExpiresIn, IsRequired = false)]
        public long ExtendedExpiresIn
        {
            get => _extendedExpiresIn;
            set
            {
                _extendedExpiresIn = value;
                AccessTokenExtendedExpiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(_extendedExpiresIn);
            }
        }

        [DataMember(Name = TokenResponseClaim.RefreshIn, IsRequired = false)]
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
        [DataMember(Name = TokenResponseClaim.FamilyId, IsRequired = false)]
        public string FamilyId { get; set; }

        public DateTimeOffset AccessTokenExpiresOn { get; private set; }
        public DateTimeOffset AccessTokenExtendedExpiresOn { get; private set; }

        public DateTimeOffset? AccessTokenRefreshOn { get; private set; }

        public string Authority { get; private set; }

        internal static MsalTokenResponse CreateFromBrokerResponse(Dictionary<string, string> responseDictionary)
        {
            if (responseDictionary.ContainsKey(BrokerResponseConst.BrokerErrorCode) ||
                responseDictionary.ContainsKey(BrokerResponseConst.BrokerErrorDescription))
            {
                return new MsalTokenResponse
                {
                    Error = responseDictionary[BrokerResponseConst.BrokerErrorCode],
                    ErrorDescription = CoreHelpers.UrlDecode(responseDictionary[BrokerResponseConst.BrokerErrorDescription])
                };
            }

            var response =  new MsalTokenResponse
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
                ExpiresIn = responseDictionary.ContainsKey(BrokerResponseConst.ExpiresOn)
                    ? long.Parse(responseDictionary[BrokerResponseConst.ExpiresOn].Split('.')[0], CultureInfo.InvariantCulture)
                    : Convert.ToInt64(DateTime.UtcNow, CultureInfo.InvariantCulture),
                ClientInfo = responseDictionary.ContainsKey(BrokerResponseConst.ClientInfo)
                    ? responseDictionary[BrokerResponseConst.ClientInfo]
                    : null,
            };

            if (responseDictionary.ContainsKey(TokenResponseClaim.RefreshIn))
            {
                response.RefreshIn = long.Parse(
                    responseDictionary[TokenResponseClaim.RefreshIn], 
                    CultureInfo.InvariantCulture);
            }

            return response;
        }
    }
}

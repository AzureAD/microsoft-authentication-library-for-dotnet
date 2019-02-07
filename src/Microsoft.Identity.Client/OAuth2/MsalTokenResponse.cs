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
    }

    [DataContract]
    internal class MsalTokenResponse : OAuth2ResponseBase
    {
        private long _expiresIn;
        private long _extendedExpiresIn;

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

        public DateTimeOffset AccessTokenExpiresOn { get; private set; }
        public DateTimeOffset AccessTokenExtendedExpiresOn { get; private set; }

        public string Authority { get; private set; }

        internal static MsalTokenResponse CreateFromBrokerResponse(Dictionary<string, string> responseDictionary)
        {
            if (responseDictionary.ContainsKey(BrokerResponseConst.ErrorMetadata))
            {
                return new MsalTokenResponse
                {
                    Error = responseDictionary[BrokerResponseConst.ErrorDomain],
                    ErrorDescription = CoreHelpers.UrlDecode(responseDictionary[BrokerResponseConst.ErrorMetadata])
                };
            }

            return new MsalTokenResponse
            {
                Authority = responseDictionary.ContainsKey(BrokerResponseConst.Authority)
                    ? AppConfig.AuthorityInfo.CanonicalizeAuthorityUri(CoreHelpers.UrlDecode(responseDictionary[BrokerResponseConst.Authority]))
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
        }
    }
}
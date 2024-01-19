// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.SSHCertificates
{
    internal class SSHCertAuthenticationScheme : IAuthenticationScheme
    {
        internal const string SSHCertTokenType = "ssh-cert";
        private readonly string _jwk;

        public SSHCertAuthenticationScheme(string keyId, string jwk)
        {
            KeyId = Guard.AgainstNullOrEmpty(keyId);
            _jwk = Guard.AgainstNullOrEmpty(jwk);
        }

        public TokenType TelemetryTokenType => TokenType.SshCert;

        public string AuthorizationHeaderPrefix =>
            throw new MsalClientException(
                MsalError.SSHCertUsedAsHttpHeader,
                MsalErrorMessage.SSHCertUsedAsHttpHeader);
        public string AccessTokenType => SSHCertTokenType;

        public string KeyId { get; }

        public string FormatAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            return msalAccessTokenCacheItem.Secret;
        }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            return new Dictionary<string, string>()
            {
                { OAuth2Parameter.TokenType, SSHCertTokenType },
                { OAuth2Parameter.RequestConfirmation , _jwk }
            };
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.AuthScheme.SSHCertificates
{
    internal class SSHCertAuthenticationOperation : IAuthenticationOperation
    {
        internal const string SSHCertTokenType = "ssh-cert";
        private readonly string _jwk;

        public SSHCertAuthenticationOperation(string keyId, string jwk)
        {
            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentNullException(nameof(keyId));
            }

            if (string.IsNullOrEmpty(jwk))
            {
                throw new ArgumentNullException(nameof(jwk));
            }

            KeyId = keyId;
            _jwk = jwk;
        }

        public int TelemetryTokenType => (int)TokenType.SshCert;

        public string AuthorizationHeaderPrefix =>
            throw new MsalClientException(
                MsalError.SSHCertUsedAsHttpHeader,
                MsalErrorMessage.SSHCertUsedAsHttpHeader);
        public string AccessTokenType => SSHCertTokenType;

        public string KeyId { get; }

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            // no-op
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

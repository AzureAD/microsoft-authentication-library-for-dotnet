// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.AuthScheme.Bearer;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    /// <summary>
    /// Authentication operation for bearer tokens over mTLS.
    /// This uses mTLS for transport security but returns a standard bearer token (not bound to the certificate).
    /// </summary>
    internal class MtlsBearerAuthenticationOperation : IAuthenticationOperation
    {
        private readonly X509Certificate2 _mtlsCert;

        public MtlsBearerAuthenticationOperation(X509Certificate2 mtlsCert)
        {
            _mtlsCert = mtlsCert;
            KeyId = string.Empty; // Bearer tokens don't use KeyId
        }

        public int TelemetryTokenType => TelemetryTokenTypeConstants.Bearer;

        public string AuthorizationHeaderPrefix => Constants.BearerAuthHeaderPrefix;

        public string AccessTokenType => BearerAuthenticationOperation.BearerTokenType;

        public string KeyId { get; }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            // For bearer tokens over mTLS, we don't send token_type parameter
            // The mTLS is at transport level, token remains bearer
            return new Dictionary<string, string>();
        }

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            // Store the mTLS certificate in the result for informational purposes
            authenticationResult.BindingCertificate = _mtlsCert;
        }
    }
}

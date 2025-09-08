// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    /// <summary>
    /// This is just a demo class. Real implementation of MTLS PoP should use <see cref="MtlsPopAuthenticationOperation"/>.
    /// For now we are using this class to demo how to use MTLS PoP with Managed Identity with a bearer flow.
    /// This way caching logic will continue to work as expected.
    /// </summary>
    internal class MsiMtlsPopAuthenticationOperation : IAuthenticationOperation
    {
        private readonly X509Certificate2 _mtlsCert;

        public MsiMtlsPopAuthenticationOperation(X509Certificate2 mtlsCert)
        {
            _mtlsCert = mtlsCert;
            KeyId = CoreHelpers.ComputeX5tS256KeyId(_mtlsCert);
        }

        public int TelemetryTokenType => TelemetryTokenTypeConstants.Bearer;

        public string AuthorizationHeaderPrefix => Constants.BearerAuthHeaderPrefix;

        public string AccessTokenType => Constants.BearerTokenType;

        public string KeyId { get; }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            return new Dictionary<string, string>
            {
                { OAuth2Parameter.TokenType, Constants.BearerTokenType }
            };
        }

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            authenticationResult.BindingCertificate = _mtlsCert;
        }
    }
}

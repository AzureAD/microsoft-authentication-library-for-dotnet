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
    internal class MtlsPopAuthenticationOperation : IAuthenticationOperation
    {
        private readonly X509Certificate2 _mtlsCert;

        public MtlsPopAuthenticationOperation(X509Certificate2 mtlsCert)
        {
            _mtlsCert = mtlsCert;
            KeyId = CoreHelpers.ComputeX5tS256KeyId(_mtlsCert);
        }

        public int TelemetryTokenType => TelemetryTokenTypeConstants.MtlsPop;

        public string AuthorizationHeaderPrefix => Constants.MtlsPoPAuthHeaderPrefix;

        public string AccessTokenType => Constants.MtlsPoPTokenType;

        public string KeyId { get; }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            return new Dictionary<string, string>
            {
                { OAuth2Parameter.TokenType, Constants.MtlsPoPTokenType }
            };
        }

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            authenticationResult.BindingCertificate = _mtlsCert;
        }

        bool ValidateCachedToken(MsalCacheValidationData cachedTokenItem)
        {
            // no-op
            return true;
        }
    }
}

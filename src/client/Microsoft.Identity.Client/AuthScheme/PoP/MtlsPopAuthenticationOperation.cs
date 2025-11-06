// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    internal class MtlsPopAuthenticationOperation : IAuthenticationOperation2
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

        public Task FormatResultAsync(AuthenticationResult authenticationResult, CancellationToken cancellationToken = default)
        {
            FormatResult(authenticationResult);
            return Task.CompletedTask;
        }

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            authenticationResult.BindingCertificate = _mtlsCert;
        }
    }
}

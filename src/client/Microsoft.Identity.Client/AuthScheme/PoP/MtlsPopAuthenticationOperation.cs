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
            KeyId = ComputeX5tS256KeyId(_mtlsCert);
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

        private static string ComputeX5tS256KeyId(X509Certificate2 certificate)
        {
            // Extract the raw bytes of the certificate’s public key.
            var publicKey = certificate.GetPublicKey();

            // Compute the SHA-256 hash of the public key.
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(publicKey);

                // Return the hash encoded in Base64 URL format.
                return Base64UrlHelpers.Encode(hash);
            }
        }

        public Task FormatResultAsync(AuthenticationResult authenticationResult, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            // no-op
        }
    }
}

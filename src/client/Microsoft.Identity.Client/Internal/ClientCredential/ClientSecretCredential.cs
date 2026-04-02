// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal class ClientSecretCredential : IClientCredential
    {
        internal string Secret { get; }

        public AssertionType AssertionType => AssertionType.Secret;

        public ClientSecretCredential(string secret)
        {
            Secret = secret;
        }

        public Task<CredentialMaterial> GetCredentialMaterialAsync(
            CredentialContext context,
            CancellationToken cancellationToken)
        {
            context.Logger.Verbose(() => $"[ClientSecretCredential] Resolving credential material. " +
            $"Mode={context.Mode}");

            if (context.Mode == OAuthMode.MtlsMode)
            {
                context.Logger.Error("[ClientSecretCredential] Client secret cannot be used with mTLS Proof-of-Possession.");

                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    "A client secret cannot be used over mTLS. " +
                    "Use a certificate credential or a ClientSignedAssertion callback " +
                    "that can return a token-binding certificate.");
            }

            var parameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientSecret, Secret }
            };
            
            context.Logger.Verbose(() => "[ClientSecretCredential] Secret-based credential material created successfully.");

            return Task.FromResult(new CredentialMaterial(parameters));
        }
    }
}

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
    internal class SecretStringClientCredential : IClientCredential
    {
        internal string Secret { get; }

        public AssertionType AssertionType => AssertionType.Secret;

        public SecretStringClientCredential(string secret)
        {
            Secret = secret;
        }

        public Task<CredentialMaterial> GetCredentialMaterialAsync(
            CredentialContext context,
            CancellationToken cancellationToken)
        {
            context.Logger.Verbose(() => $"[SecretStringClientCredential] Resolving credential material. " +
            $"Mode={context.Mode}");

            if (context.Mode == ClientAuthMode.MtlsMode)
            {
                context.Logger.Error("[SecretStringClientCredential] Client secret cannot be used with mTLS Proof-of-Possession.");

                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    "A client secret cannot be used with mTLS Proof-of-Possession. " +
                    "Use a certificate-based credential or a delegate that returns a ClientSignedAssertion " +
                    "with a TokenBindingCertificate.");
            }

            var parameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientSecret, Secret }
            };
            
            context.Logger.Verbose(() => "[SecretStringClientCredential] Secret-based credential material created successfully.");

            return Task.FromResult(new CredentialMaterial(parameters, CredentialSource.Static));
        }
    }
}

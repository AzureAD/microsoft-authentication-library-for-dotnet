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
            context.Logger.Verbose(() => $"[ClientSecretCredential] Mode={context.Mode}");

            ClientCredentialGuards.ThrowIfMtlsNotSupported(context, "A client secret");

            var parameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientSecret, Secret }
            };

            return Task.FromResult(new CredentialMaterial(parameters));
        }
    }
}

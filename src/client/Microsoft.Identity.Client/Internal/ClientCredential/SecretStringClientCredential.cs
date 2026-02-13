// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;

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
            // Client secret doesn't support mTLS mode
            if (context.Mode == ClientAuthMode.MtlsMode)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    "Client secret credential cannot be used in mTLS mode.");
            }

            var tokenParameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientSecret, Secret }
            };

            var material = new CredentialMaterial(
                tokenRequestParameters: tokenParameters,
                source: CredentialSource.Static);

            return Task.FromResult(material);
        }
    }
}

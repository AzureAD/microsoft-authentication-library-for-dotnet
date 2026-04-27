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
    internal class SignedAssertionClientCredential(string signedAssertion) : IClientCredential
    {
        private readonly string _signedAssertion = signedAssertion;

        public AssertionType AssertionType => AssertionType.ClientAssertion;

        public Task<CredentialMaterial> GetCredentialMaterialAsync(
            CredentialContext context,
            CancellationToken cancellationToken)
        {
            context.Logger.Verbose(() => $"[SignedAssertionClientCredential] Mode={context.Mode}");

            ClientCredentialGuards.ThrowIfMtlsNotSupported(context, "A precomputed client assertion string");

            var parameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                { OAuth2Parameter.ClientAssertion, _signedAssertion }
            };

            return Task.FromResult(new CredentialMaterial(parameters));
        }
    }
}

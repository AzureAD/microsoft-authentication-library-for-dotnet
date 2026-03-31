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
    internal class SignedAssertionClientCredential : IClientCredential
    {
        private readonly string _signedAssertion;

        public AssertionType AssertionType => AssertionType.ClientAssertion;

        public SignedAssertionClientCredential(string signedAssertion)
        {
            _signedAssertion = signedAssertion;
        }

        public Task<CredentialMaterial> GetCredentialMaterialAsync(
            CredentialContext context,
            CancellationToken cancellationToken)
        {
            context.Logger.Verbose(() => $"[SignedAssertionClientCredential] Resolving credential material. " +
            $"Mode={context.Mode}");

            if (context.Mode == OAuthMode.MtlsMode)
            {
                context.Logger.Error("[SignedAssertionClientCredential] Static signed assertion cannot be used with mTLS Proof-of-Possession.");

                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    MsalErrorMessage.UnsupportedCredentialForMtlsMessage("A precomputed client assertion string"));
            }

            var parameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                { OAuth2Parameter.ClientAssertion, _signedAssertion }
            };

            context.Logger.Verbose(() => "[SignedAssertionClientCredential] Signed assertion credential material created successfully.");

            return Task.FromResult(new CredentialMaterial(parameters));
        }
    }
}

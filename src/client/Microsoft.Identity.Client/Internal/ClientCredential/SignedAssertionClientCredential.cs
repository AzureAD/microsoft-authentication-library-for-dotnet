// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            if (context.Mode == ClientAuthMode.MtlsMode)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    "A static signed assertion cannot be used with mTLS Proof-of-Possession because it " +
                    "cannot supply a certificate for TLS transport binding. " +
                    "Use a delegate credential that returns a ClientSignedAssertion with a TokenBindingCertificate.");
            }

            var parameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                { OAuth2Parameter.ClientAssertion, _signedAssertion }
            };

            return Task.FromResult(new CredentialMaterial(parameters, CredentialSource.Static));
        }
    }
}


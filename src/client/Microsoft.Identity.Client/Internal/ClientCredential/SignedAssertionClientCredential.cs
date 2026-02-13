// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;

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
            // Per canonical matrix: jwt credential doesn't support MtlsMode
            if (context.Mode == ClientAuthMode.MtlsMode)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    "Signed assertion credential cannot be used in mTLS mode. Use WithClientAssertion callback that returns ClientSignedAssertion with TokenBindingCertificate instead.");
            }

            if (string.IsNullOrWhiteSpace(_signedAssertion))
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            var tokenParameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                { OAuth2Parameter.ClientAssertion, _signedAssertion }
            };

            var material = new CredentialMaterial(
                tokenRequestParameters: tokenParameters,
                source: CredentialSource.Static);

            return Task.FromResult(material);
        }
    }
}

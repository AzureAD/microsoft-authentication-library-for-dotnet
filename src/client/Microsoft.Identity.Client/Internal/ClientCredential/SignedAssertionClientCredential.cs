// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
            CredentialRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var tokenParams = new System.Collections.Generic.Dictionary<string, string>
            {
                [OAuth2Parameter.ClientAssertionType] = OAuth2AssertionType.JwtBearer,
                [OAuth2Parameter.ClientAssertion] = _signedAssertion
            };

            sw.Stop();

            var metadata = new CredentialMaterialMetadata(
                credentialType: CredentialType.ClientAssertion,
                credentialSource: "pre-signed",
                mtlsCertificateIdHashPrefix: null,
                mtlsCertificateRequested: requestContext.MtlsRequired,
                resolutionTimeMs: sw.ElapsedMilliseconds);

            var material = new CredentialMaterial(
                tokenRequestParameters: tokenParams,
                mtlsCertificate: null,
                metadata: metadata);

            return Task.FromResult(material);
        }

        public Task<ClientCredentialApplicationResult> AddConfidentialClientParametersAsync(
            OAuth2Client oAuth2Client,
            AuthenticationRequestParameters requestParameters,
            ICryptographyManager cryptographyManager, 
            string tokenEndpoint,
            CancellationToken cancellationToken)
        {
            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, _signedAssertion);
            return Task.FromResult(ClientCredentialApplicationResult.None);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Client assertion provided as a string JWT. Cannot return TokenBindingCertificate (no mTLS preflight).
    /// </summary>
    internal sealed class ClientAssertionStringDelegateCredential : IClientCredential
    {
        private readonly Func<AssertionRequestOptions, CancellationToken, Task<string>> _provider;

        internal ClientAssertionStringDelegateCredential(
            Func<AssertionRequestOptions, CancellationToken, Task<string>> provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public AssertionType AssertionType => AssertionType.ClientAssertion;

        public async Task<CredentialMaterial> GetCredentialMaterialAsync(
            CredentialRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            var opts = new AssertionRequestOptions
            {
                CancellationToken = cancellationToken,
                ClientID = requestContext.ClientId,
                TokenEndpoint = requestContext.TokenEndpoint,
                ClientCapabilities = requestContext.ClientCapabilities,
                Claims = requestContext.Claims,
                ClientAssertionFmiPath = requestContext.ClientAssertionFmiPath
            };

            string assertion = await _provider(opts, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(assertion))
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            sw.Stop();

            var tokenParameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                { OAuth2Parameter.ClientAssertion, assertion }
            };

            return new CredentialMaterial(
                tokenRequestParameters: tokenParameters,
                mtlsCertificate: null,
                metadata: new CredentialMaterialMetadata(
                    credentialType: CredentialType.ClientAssertion,
                    credentialSource: "callback",
                    mtlsCertificateRequested: requestContext.MtlsRequired,
                    resolutionTimeMs: sw.ElapsedMilliseconds));
        }
    }
}

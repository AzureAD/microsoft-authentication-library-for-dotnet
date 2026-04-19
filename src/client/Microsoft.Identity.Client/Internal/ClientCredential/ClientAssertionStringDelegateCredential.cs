// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Client assertion provided as a string JWT via a delegate.
    /// Cannot return a <see cref="ClientSignedAssertion.TokenBindingCertificate"/> and therefore
    /// is incompatible with mTLS.
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
            CredentialContext context,
            CancellationToken cancellationToken)
        {
            context.Logger.Verbose(() => $"[ClientAssertionStringDelegateCredential] Mode={context.Mode}");

            if (context.Mode == OAuthMode.MtlsMode)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    "A string-returning client assertion callback cannot be used over mTLS. " +
                    "Use a ClientSignedAssertion callback that can return a token-binding certificate.");
            }

            var opts = new AssertionRequestOptions
            {
                CancellationToken = cancellationToken,
                ClientID = context.ClientId,
                TokenEndpoint = context.TokenEndpoint,
                ClientCapabilities = context.ClientCapabilities,
                Claims = context.Claims,
                ClientAssertionFmiPath = context.ClientAssertionFmiPath,
                Authority = context.Authority,
                TenantId = context.TenantId
            };

            string assertion = await _provider(opts, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(assertion))
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            var parameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                { OAuth2Parameter.ClientAssertion, assertion }
            };

            return new CredentialMaterial(parameters);
        }
    }
}

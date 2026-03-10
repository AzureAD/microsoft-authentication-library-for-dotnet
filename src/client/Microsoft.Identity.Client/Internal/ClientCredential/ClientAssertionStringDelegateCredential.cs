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
    /// is incompatible with mTLS Proof-of-Possession.
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
            context.Logger.Verbose(() => $"[ClientAssertionStringDelegateCredential] Resolving client assertion material. " +
            $"Mode={context.Mode}, TokenEndpoint={context.TokenEndpoint}");

            if (context.Mode == ClientAuthMode.MtlsMode)
            {
                context.Logger.Error("[ClientAssertionStringDelegateCredential] String-returning assertion delegate " +
                    "cannot be used with mTLS Proof-of-Possession because no token-binding certificate can be supplied.");

                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    "A string-returning delegate credential cannot be used with mTLS Proof-of-Possession " +
                    "because it cannot supply a certificate for TLS transport binding. " +
                    "Use a delegate that returns a ClientSignedAssertion with a TokenBindingCertificate.");
            }

            context.Logger.Verbose(() => "[ClientAssertionStringDelegateCredential] Building assertion request " +
            "options for delegate invocation.");

            var opts = new AssertionRequestOptions
            {
                CancellationToken = cancellationToken,
                ClientID = context.ClientId,
                TokenEndpoint = context.TokenEndpoint,
                ClientCapabilities = context.ClientCapabilities,
                Claims = context.Claims,
                ClientAssertionFmiPath = context.ClientAssertionFmiPath
            };

            context.Logger.Verbose(() => "[ClientAssertionStringDelegateCredential] Invoking string assertion provider delegate.");

            string assertion = await _provider(opts, cancellationToken).ConfigureAwait(false);

            context.Logger.Verbose(() => "[ClientAssertionStringDelegateCredential] Assertion delegate returned a response. " +
            "Validating that it is not null or empty.");

            if (string.IsNullOrWhiteSpace(assertion))
            {
                context.Logger.Error("[ClientAssertionStringDelegateCredential] Assertion delegate returned a null or empty assertion.");

                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            var parameters = new Dictionary<string, string>
            {
                { OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer },
                { OAuth2Parameter.ClientAssertion, assertion }
            };
            
            context.Logger.Verbose(() => "[ClientAssertionStringDelegateCredential] Client assertion material created successfully using JwtBearer.");

            return new CredentialMaterial(parameters, CredentialSource.Callback);
        }
    }
}

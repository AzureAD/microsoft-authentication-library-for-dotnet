// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Immutable input context passed to <see cref="IClientCredential.GetCredentialMaterialAsync"/>.
    /// Consolidates all credential-resolution inputs into a single object, eliminating
    /// the direct coupling to <see cref="OAuth2.OAuth2Client"/> and
    /// <see cref="Requests.AuthenticationRequestParameters"/> that existed in the previous API.
    /// </summary>
    internal sealed class CredentialContext
    {
        /// <summary>Application (client) identifier.</summary>
        public string ClientId { get; init; }

        /// <summary>Full token endpoint URL for the current request.</summary>
        public string TokenEndpoint { get; init; }

        /// <summary>
        /// Whether this is a standard (JWT / secret) request or an mTLS-bound request.
        /// </summary>
        public CredentialTransportProtocol Mode { get; init; }

        /// <summary>User-provided claims string (may be null).</summary>
        public string Claims { get; init; }

        /// <summary>Client capabilities configured on the application.</summary>
        public IEnumerable<string> ClientCapabilities { get; init; }

        /// <summary>Platform cryptography manager used for JWT signing.</summary>
        public ICryptographyManager CryptographyManager { get; init; }

        /// <summary>Whether the x5c (certificate chain) claim should be included in the assertion.</summary>
        public bool SendX5C { get; init; }

        /// <summary>Whether to use SHA-2 for certificate-based assertions (authority-driven).</summary>
        public bool UseSha2 { get; init; }

        /// <summary>Extra claims to embed in the client assertion (request-level override).</summary>
        public string ExtraClientAssertionClaims { get; init; }

        /// <summary>FMI path used to embed a subject suffix in the client assertion.</summary>
        public string ClientAssertionFmiPath { get; init; }

        /// <summary>Canonical authority URL (e.g., https://login.microsoftonline.com/{tenantId}).</summary>
        public string Authority { get; init; }

        /// <summary>Tenant ID from the runtime authority.</summary>
        public string TenantId { get; init; }

        /// <summary>Correlation ID for end-to-end request tracing.</summary>
        public Guid CorrelationId { get; init; }

        /// <summary>Logger for credential resolution diagnostics.</summary>
        public ILoggerAdapter Logger { get; init; }

        /// <summary>
        /// Creates an <see cref="AssertionRequestOptions"/> from this context.
        /// Centralizes the mapping so credential implementations don't duplicate it.
        /// </summary>
        internal AssertionRequestOptions ToAssertionRequestOptions(CancellationToken cancellationToken)
        {
            return new AssertionRequestOptions
            {
                ClientID = ClientId,
                TokenEndpoint = TokenEndpoint,
                Claims = Claims,
                ClientCapabilities = ClientCapabilities,
                Authority = Authority,
                TenantId = TenantId,
                CorrelationId = CorrelationId,
                ClientAssertionFmiPath = ClientAssertionFmiPath,
                CancellationToken = cancellationToken
            };
        }
    }
}

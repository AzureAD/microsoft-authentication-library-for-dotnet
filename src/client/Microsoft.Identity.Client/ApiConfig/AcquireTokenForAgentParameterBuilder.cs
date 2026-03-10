// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Builder for acquiring an app-only token for an agent identity via a
    /// <see cref="IConfidentialClientApplication"/>.
    /// Use <see cref="IConfidentialClientApplication.AcquireTokenForAgent(string, IEnumerable{string})"/>
    /// to create this builder.
    /// </summary>
    /// <remarks>
    /// This flow internally:
    /// <list type="number">
    /// <item>Obtains an FMI credential (FIC) from the token exchange endpoint using the CCA's credential.</item>
    /// <item>Uses the FIC as a client assertion to acquire a token for the requested scopes.</item>
    /// </list>
    /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
    public sealed class AcquireTokenForAgentParameterBuilder
    {
        private readonly ConfidentialClientApplication _app;
        private readonly string _agentId;
        private readonly IEnumerable<string> _scopes;
        private bool _forceRefresh;
        private Guid? _correlationId;

        internal AcquireTokenForAgentParameterBuilder(
            ConfidentialClientApplication app, string agentId, IEnumerable<string> scopes)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _agentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        }

        /// <summary>
        /// Forces MSAL to refresh the token from the identity provider, bypassing the cache.
        /// </summary>
        /// <param name="forceRefresh">If <c>true</c>, ignore any cached tokens and request a new token.</param>
        /// <returns>The builder, for fluent chaining.</returns>
        public AcquireTokenForAgentParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            _forceRefresh = forceRefresh;
            return this;
        }

        /// <summary>
        /// Sets a correlation ID for telemetry and diagnostics.
        /// </summary>
        /// <param name="correlationId">A GUID to correlate requests across services.</param>
        /// <returns>The builder, for fluent chaining.</returns>
        public AcquireTokenForAgentParameterBuilder WithCorrelationId(Guid correlationId)
        {
            _correlationId = correlationId;
            return this;
        }

        /// <summary>
        /// Executes the token acquisition asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An <see cref="AuthenticationResult"/> containing the requested token.</returns>
        public Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return _app.ExecuteAgentTokenAcquisitionAsync(
                _agentId, _scopes, _forceRefresh, _correlationId, cancellationToken);
        }
    }
}

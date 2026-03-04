// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Builder for acquiring a user-delegated token for an agent identity, acting on behalf of a specified user.
    /// Use <see cref="IConfidentialClientApplication.AcquireTokenForAgentOnBehalfOfUser(string, IEnumerable{string}, string)"/>
    /// to create this builder.
    /// </summary>
    /// <remarks>
    /// This flow internally:
    /// <list type="number">
    /// <item>Obtains an FMI credential (FIC) from the token exchange endpoint using the CCA's credential.</item>
    /// <item>Obtains a User Federated Identity Credential (User FIC) for the agent.</item>
    /// <item>Exchanges the User FIC for a user-delegated token via the <c>user_fic</c> grant type.</item>
    /// </list>
    /// After a successful acquisition, you can use <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/>
    /// with the returned account for subsequent cached token lookups.
    /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
    public sealed class AcquireTokenForAgentOnBehalfOfUserParameterBuilder
    {
        private readonly ConfidentialClientApplication _app;
        private readonly string _agentId;
        private readonly IEnumerable<string> _scopes;
        private readonly string _userPrincipalName;
        private bool _forceRefresh;
        private Guid? _correlationId;

        internal AcquireTokenForAgentOnBehalfOfUserParameterBuilder(
            ConfidentialClientApplication app,
            string agentId,
            IEnumerable<string> scopes,
            string userPrincipalName)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _agentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _userPrincipalName = userPrincipalName ?? throw new ArgumentNullException(nameof(userPrincipalName));
        }

        /// <summary>
        /// Forces MSAL to refresh the token from the identity provider, bypassing the cache.
        /// </summary>
        /// <param name="forceRefresh">If <c>true</c>, ignore any cached tokens and request a new token.</param>
        /// <returns>The builder, for fluent chaining.</returns>
        public AcquireTokenForAgentOnBehalfOfUserParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            _forceRefresh = forceRefresh;
            return this;
        }

        /// <summary>
        /// Sets a correlation ID for telemetry and diagnostics.
        /// </summary>
        /// <param name="correlationId">A GUID to correlate requests across services.</param>
        /// <returns>The builder, for fluent chaining.</returns>
        public AcquireTokenForAgentOnBehalfOfUserParameterBuilder WithCorrelationId(Guid correlationId)
        {
            _correlationId = correlationId;
            return this;
        }

        /// <summary>
        /// Executes the token acquisition asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An <see cref="AuthenticationResult"/> containing the requested user-delegated token.</returns>
        public Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return _app.ExecuteAgentOnBehalfOfUserAsync(
                _agentId, _scopes, _userPrincipalName, _forceRefresh, _correlationId, cancellationToken);
        }
    }
}

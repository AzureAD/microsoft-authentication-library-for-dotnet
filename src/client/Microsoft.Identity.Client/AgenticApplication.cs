// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Application class for agentic identity flows.
    /// Orchestrates FMI credential acquisition and token exchange internally,
    /// providing a simple API for acquiring app-only and user-delegated tokens.
    /// </summary>
    /// <remarks>
    /// This class manages two internal confidential client applications:
    /// <list type="bullet">
    /// <item>A platform CCA — uses the platform certificate (SN+I) to obtain FMI credentials via token exchange.</item>
    /// <item>An agent CCA — uses the FMI credential as a client assertion to acquire tokens for target resources.</item>
    /// </list>
    /// Use <see cref="AgenticApplicationBuilder"/> to create instances of this class.
    /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide on mobile
#endif
    public sealed class AgenticApplication : IAgenticApplication
    {
        private readonly IConfidentialClientApplication _platformCca;
        private readonly IConfidentialClientApplication _agentCca;
        private readonly string _agentIdentity;
        private readonly string _tokenExchangeUrl;

        internal AgenticApplication(
            string agentIdentity,
            string tenantId,
            string authorityUri,
            string platformClientId,
            X509Certificate2 certificate,
            bool sendX5C,
            string tokenExchangeUrl,
            CacheOptions cacheOptions,
            IMsalHttpClientFactory httpClientFactory,
            IIdentityLogger logger,
            bool enablePiiLogging,
            IHttpManager httpManager = null,
            bool enableInstanceDiscovery = true)
        {
            _agentIdentity = agentIdentity;
            _tokenExchangeUrl = tokenExchangeUrl;

            // ── Platform CCA ────────────────────────────────────────────────────
            // Used to obtain FMI credentials via certificate + SN+I authentication.
            // This CCA authenticates as the platform (host) application.
            var platformBuilder = ConfidentialClientApplicationBuilder
                .Create(platformClientId)
                .WithAuthority(authorityUri, tenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(certificate, sendX5C);

            if (cacheOptions != null)
                platformBuilder = platformBuilder.WithCacheOptions(cacheOptions);
            if (httpClientFactory != null)
                platformBuilder = platformBuilder.WithHttpClientFactory(httpClientFactory);
            if (logger != null)
                platformBuilder = platformBuilder.WithLogging(logger, enablePiiLogging);
            if (httpManager != null)
                platformBuilder = platformBuilder.WithHttpManager(httpManager);
            if (!enableInstanceDiscovery)
                platformBuilder = platformBuilder.WithInstanceDiscovery(false);

            _platformCca = platformBuilder.Build();

            // ── Agent CCA ───────────────────────────────────────────────────────
            // Used for actual token acquisition. Uses the FMI credential (obtained
            // from the platform CCA) as a client assertion.
            var agentBuilder = ConfidentialClientApplicationBuilder
                .Create(agentIdentity)
                .WithAuthority(authorityUri, tenantId)
                .WithExperimentalFeatures(true)
                .WithClientAssertion(async (AssertionRequestOptions options) =>
                {
                    // Use the FMI path from the assertion options if available (set by
                    // WithFmiPathForClientAssertion at request time), otherwise fall back
                    // to the agent identity.
                    string fmiPath = options.ClientAssertionFmiPath ?? agentIdentity;
                    return await GetAppCredentialAsync(fmiPath).ConfigureAwait(false);
                });

            if (cacheOptions != null)
                agentBuilder = agentBuilder.WithCacheOptions(cacheOptions);
            if (httpClientFactory != null)
                agentBuilder = agentBuilder.WithHttpClientFactory(httpClientFactory);
            if (logger != null)
                agentBuilder = agentBuilder.WithLogging(logger, enablePiiLogging);
            if (httpManager != null)
                agentBuilder = agentBuilder.WithHttpManager(httpManager);
            if (!enableInstanceDiscovery)
                agentBuilder = agentBuilder.WithInstanceDiscovery(false);

            _agentCca = agentBuilder.Build();
        }

        /// <inheritdoc/>
        public AcquireTokenForAgentParameterBuilder AcquireTokenForAgent(IEnumerable<string> scopes)
        {
            if (scopes == null)
                throw new ArgumentNullException(nameof(scopes));

            return new AcquireTokenForAgentParameterBuilder(this, scopes);
        }

        /// <inheritdoc/>
        public AcquireTokenForAgentOnBehalfOfUserParameterBuilder AcquireTokenForAgentOnBehalfOfUser(
            IEnumerable<string> scopes, string userPrincipalName)
        {
            if (scopes == null)
                throw new ArgumentNullException(nameof(scopes));
            if (string.IsNullOrEmpty(userPrincipalName))
                throw new ArgumentNullException(nameof(userPrincipalName));

            return new AcquireTokenForAgentOnBehalfOfUserParameterBuilder(this, scopes, userPrincipalName);
        }

        /// <inheritdoc/>
        public Task<IAccount> GetAccountAsync(string accountIdentifier)
        {
            return _agentCca.GetAccountAsync(accountIdentifier);
        }

        /// <inheritdoc/>
        public AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, IAccount account)
        {
            return _agentCca.AcquireTokenSilent(scopes, account);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Internal execution methods — called by the parameter builders
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Acquires an app-only token for the agent identity.
        /// The FMI credential is obtained automatically via the client assertion delegate.
        /// </summary>
        internal async Task<AuthenticationResult> ExecuteAgentTokenAcquisitionAsync(
            IEnumerable<string> scopes,
            bool forceRefresh,
            Guid? correlationId,
            CancellationToken cancellationToken)
        {
            var builder = _agentCca.AcquireTokenForClient(scopes);

            if (forceRefresh)
                builder = builder.WithForceRefresh(true);

            if (correlationId.HasValue)
                builder = builder.WithCorrelationId(correlationId.Value);

            return await builder
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires a user-delegated token using the User FIC flow.
        /// Steps:
        ///   1. Obtain the User FIC (via agent CCA + FMI path for client assertion)
        ///   2. Use IByUsernameAndPassword with OnBeforeTokenRequest to rewrite the request
        ///      to use user_fic grant type with the User FIC assertion
        /// </summary>
        internal async Task<AuthenticationResult> ExecuteAgentOnBehalfOfUserAsync(
            IEnumerable<string> scopes,
            string userPrincipalName,
            bool forceRefresh,
            Guid? correlationId,
            CancellationToken cancellationToken)
        {
            // Build the username/password request (the actual grant type is rewritten via OnBeforeTokenRequest)
            var usernamePasswordBuilder = ((IByUsernameAndPassword)_agentCca)
                .AcquireTokenByUsernamePassword(scopes, userPrincipalName, "no_password");

            if (correlationId.HasValue)
                usernamePasswordBuilder = usernamePasswordBuilder.WithCorrelationId(correlationId.Value);

            // OnBeforeTokenRequest rewrites the request to use user_fic grant type
            return await usernamePasswordBuilder
                .OnBeforeTokenRequest(async (request) =>
                {
                    // Step 1: Obtain the User FIC assertion
                    string userFicAssertion = await GetUserFicAsync(cancellationToken).ConfigureAwait(false);

                    // Step 2: Rewrite the request body for user_fic grant
                    request.BodyParameters["user_federated_identity_credential"] = userFicAssertion;
                    request.BodyParameters["grant_type"] = "user_fic";

                    // Remove the dummy password — not needed for user_fic grant
                    request.BodyParameters.Remove("password");

                    // Remove client_secret if it's the default placeholder
                    if (request.BodyParameters.TryGetValue("client_secret", out var secret)
                        && secret.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        request.BodyParameters.Remove("client_secret");
                    }
                })
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Private credential helpers
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Obtains the FMI credential (app assertion) from the platform CCA.
        /// Uses the platform certificate with SN+I to acquire a token for the token exchange URL,
        /// targeting the specified FMI path.
        /// </summary>
        private async Task<string> GetAppCredentialAsync(string fmiPath)
        {
            var result = await _platformCca.AcquireTokenForClient(new[] { _tokenExchangeUrl })
                .WithFmiPath(fmiPath)
                .ExecuteAsync()
                .ConfigureAwait(false);

            return result.AccessToken;
        }

        /// <summary>
        /// Obtains the User FIC token using the agent CCA.
        /// This calls the token exchange endpoint with the agent identity's FMI path
        /// for the client assertion, producing a User FIC that can be exchanged for a
        /// user-delegated token.
        /// </summary>
        private async Task<string> GetUserFicAsync(CancellationToken cancellationToken)
        {
            var result = await _agentCca.AcquireTokenForClient(new[] { _tokenExchangeUrl })
                .WithFmiPathForClientAssertion(_agentIdentity)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.AccessToken;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Instance;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    /// Orchestrates a multi-leg token acquisition for agent scenarios.
    ///
    /// Two CCA instances are involved:
    ///
    ///   1. Blueprint CCA — the developer-created CCA that holds the real credential (certificate, secret, etc.).
    ///      It only participates in Leg 1: acquiring an FMI credential via AcquireTokenForClient + WithFmiPath.
    ///      Its app token cache stores the FMI credential.
    ///
    ///   2. Agent CCA — an internal CCA keyed by the agent's app ID, created and cached by this class.
    ///      Its client assertion callback delegates to the Blueprint for FMI credentials (Leg 1).
    ///      It handles both Leg 2 (AcquireTokenForClient for the assertion token, stored in its app token cache)
    ///      and Leg 3 (AcquireTokenByUserFederatedIdentityCredential for the user token, stored in its user token cache).
    ///
    /// Caching behavior:
    ///   - The Agent CCA instance is persisted in <see cref="ConfidentialClientApplication.AgentCcaCache"/>
    ///     so that subsequent calls for the same agent reuse its in-memory token caches.
    ///   - On each call, the agent CCA's user token cache is checked first via AcquireTokenSilent.
    ///     If a cached user token is found, it is returned immediately without executing Legs 2-3.
    ///   - ForceRefresh skips this silent check, but the Leg 1 (FMI credential) and Leg 2 (assertion token)
    ///     caches are still honored — only the final user token (Leg 3) is re-acquired from the network.
    /// </summary>
    internal class AgentTokenRequest : RequestBase
    {
        private readonly AcquireTokenForAgentParameters _agentParameters;

        /// <summary>
        /// The developer-created CCA that holds the real credential. Used only to acquire
        /// FMI credentials (Leg 1) and to store/retrieve the internal Agent CCA instances.
        /// </summary>
        private readonly ConfidentialClientApplication _blueprintApplication;

        public AgentTokenRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForAgentParameters agentParameters,
            ConfidentialClientApplication blueprintApplication)
            : base(serviceBundle, authenticationRequestParameters, agentParameters)
        {
            _agentParameters = agentParameters;
            _blueprintApplication = blueprintApplication;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);

            AgentIdentity agentIdentity = _agentParameters.AgentIdentity;
            string agentAppId = agentIdentity.AgentApplicationId;
            string authority = AuthenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority.ToString();

            // Retrieve (or create) the internal Agent CCA for this agent app ID.
            // This CCA is persisted across calls so its app and user token caches are retained.
            var agentCca = GetOrCreateAgentCca(agentAppId, authority);

            if (!agentIdentity.HasUserIdentifier)
            {
                // App-only flow: AcquireTokenForClient has built-in cache-first logic,
                // so no explicit silent pre-check is needed.
                return await PropagateOuterRequestParameters(
                        agentCca.AcquireTokenForClient(AuthenticationRequestParameters.Scope))
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // --- User identity flow ---

            // Check the Agent CCA's user token cache for a previously-acquired token for this user.
            // ForceRefresh skips this check so a fresh user token is always obtained from the network.
            if (!_agentParameters.ForceRefresh)
            {
                var cachedResult = await TryAcquireTokenSilentFromAgentCacheAsync(
                    agentCca,
                    agentIdentity,
                    AuthenticationRequestParameters.Scope,
                    cancellationToken).ConfigureAwait(false);
                if (cachedResult != null)
                {
                    return cachedResult;
                }
            }

            // Cache miss (or ForceRefresh) — execute Leg 2 + Leg 3.

            // Leg 2: Acquire an assertion token from the Agent CCA's app token cache (or network).
            // This is a client credential call scoped to api://AzureADTokenExchange/.default.
            // The Agent CCA's assertion callback will invoke Leg 1 (GetFmiCredentialFromBlueprintAsync)
            // to authenticate itself, but AcquireTokenForClient's built-in cache handles repeat calls.
            var assertionResult = await PropagateOuterRequestParameters(
                    agentCca.AcquireTokenForClient(new[] { TokenExchangeScope }))
                .WithFmiPathForClientAssertion(agentAppId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            string assertion = assertionResult.AccessToken;

            // Leg 3: Exchange the assertion for a user-scoped token via UserFIC.
            // This is always a network call (acquisition flow, like auth code).
            // The result is written to the Agent CCA's user token cache for future silent retrieval.
            if (agentIdentity.UserObjectId.HasValue)
            {
                return await PropagateOuterRequestParameters(
                        ((IByUserFederatedIdentityCredential)agentCca)
                            .AcquireTokenByUserFederatedIdentityCredential(
                                AuthenticationRequestParameters.Scope,
                                agentIdentity.UserObjectId.Value,
                                assertion))
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return await PropagateOuterRequestParameters(
                    ((IByUserFederatedIdentityCredential)agentCca)
                        .AcquireTokenByUserFederatedIdentityCredential(
                            AuthenticationRequestParameters.Scope,
                            agentIdentity.Username,
                            assertion))
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Searches the Agent CCA's user token cache for a previously-acquired token
        /// matching the specified user identity (by OID or UPN).
        /// Returns null if no matching account exists or the cached token is expired.
        /// </summary>
        private async Task<AuthenticationResult> TryAcquireTokenSilentFromAgentCacheAsync(
            IConfidentialClientApplication agentCca,
            AgentIdentity agentIdentity,
            IEnumerable<string> scopes,
            CancellationToken cancellationToken)
        {
#pragma warning disable CS0618 // GetAccountsAsync is marked obsolete for external callers, but we need it here to enumerate cached accounts on the internal Agent CCA
            var accounts = await agentCca.GetAccountsAsync().ConfigureAwait(false);
#pragma warning restore CS0618

            IAccount matchedAccount = FindMatchingAccount(accounts, agentIdentity);
            if (matchedAccount == null)
            {
                return null;
            }

            try
            {
                return await PropagateOuterRequestParameters(
                        agentCca.AcquireTokenSilent(scopes, matchedAccount))
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                // Token expired or requires interaction — fall through to full Leg 2 + Leg 3 flow
                return null;
            }
        }

        /// <summary>
        /// Finds an account in the Agent CCA's cache that matches the user identity.
        /// Matches by OID (HomeAccountId.ObjectId) if the caller specified a Guid,
        /// otherwise by UPN (Account.Username). Both comparisons are case-insensitive.
        /// </summary>
        private static IAccount FindMatchingAccount(IEnumerable<IAccount> accounts, AgentIdentity agentIdentity)
        {
            if (agentIdentity.UserObjectId.HasValue)
            {
                string targetOid = agentIdentity.UserObjectId.Value.ToString("D");
                return accounts.FirstOrDefault(a =>
                    string.Equals(a.HomeAccountId?.ObjectId, targetOid, StringComparison.OrdinalIgnoreCase));
            }

            return accounts.FirstOrDefault(a =>
                string.Equals(a.Username, agentIdentity.Username, StringComparison.OrdinalIgnoreCase));
        }

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            // CCS headers are handled by the internal Agent CCA's own request handlers.
            return null;
        }

        #region Agent CCA Construction and Configuration

        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";
        private const string AgentCcaKeyPrefix = "agent_";

        /// <summary>
        /// Retrieves the cached internal Agent CCA for the given agent app ID, or creates one
        /// if this is the first call. The Agent CCA is stored in the Blueprint's AgentCcaCache
        /// so its app and user token caches persist across calls.
        /// </summary>
        private IConfidentialClientApplication GetOrCreateAgentCca(string agentAppId, string authority)
        {
            string key = AgentCcaKeyPrefix + agentAppId;
            return _blueprintApplication.AgentCcaCache.GetOrAdd(key, _ => BuildAgentCca(agentAppId, authority));
        }

        /// <summary>
        /// Builds a new internal Agent CCA configured with:
        ///   - Client ID = the agent's app ID
        ///   - Authority = the Blueprint's resolved authority
        ///   - Client assertion callback = Leg 1 (FMI credential from Blueprint)
        ///   - App-level config = propagated from the Blueprint via <see cref="PropagateBlueprintConfig"/>
        /// </summary>
        private IConfidentialClientApplication BuildAgentCca(string agentAppId, string authority)
        {
            // The assertion callback lambda is stored inside the Agent CCA, which lives for the
            // lifetime of the Blueprint CCA (persisted in AgentCcaCache). Only long-lived objects
            // should be captured — specifically the Blueprint CCA reference and the agent app ID.
            // Capturing 'this' (the per-request AgentTokenRequest) would pin per-request state
            // (AuthenticationRequestParameters, RequestContext, CancellationToken, etc.) in memory
            // indefinitely and risk using stale request data on future assertion callback invocations.
            var blueprint = _blueprintApplication;

            var builder = ConfidentialClientApplicationBuilder
                .Create(agentAppId)
                .WithAuthority(authority)
                .WithExperimentalFeatures(true)
                .WithClientAssertion(async (AssertionRequestOptions opts) =>
                {
                    // Leg 1: Acquire an FMI credential from the Blueprint CCA.
                    // AcquireTokenForClient has built-in cache-first logic — only the first call
                    // hits the network; subsequent calls return the cached FMI credential.
                    string fmiPath = opts.ClientAssertionFmiPath ?? agentAppId;

                    var result = await blueprint
                        .AcquireTokenForClient(new[] { TokenExchangeScope })
                        .WithFmiPath(fmiPath)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    return result.AccessToken;
                });

            PropagateBlueprintConfig(builder);
            return builder.Build();
        }

        /// <summary>
        /// Propagates app-level configuration from the Blueprint CCA to the Agent CCA builder.
        /// Copies properties directly on the builder's internal Config to avoid awkward builder
        /// API constraints (e.g., WithLogging throwing if called twice, InstanceDiscoveryResponse
        /// requiring JSON round-tripping). This ensures the Agent CCA shares the Blueprint's
        /// HTTP behavior, logging, telemetry identity, and instance discovery settings.
        ///
        /// ExtraQueryParameters and ClientCapabilities are NOT propagated here because
        /// they are already merged into the per-request AuthenticationRequestParameters
        /// and propagated by <see cref="PropagateOuterRequestParameters{T}"/>.
        /// </summary>
        private void PropagateBlueprintConfig(ConfidentialClientApplicationBuilder builder)
        {
            var blueprintConfig = _blueprintApplication.ServiceBundle.Config;
            var agentConfig = builder.Config;

            // HTTP: factory, retry policy, and internal test HttpManager
            agentConfig.HttpClientFactory = blueprintConfig.HttpClientFactory;
            agentConfig.DisableInternalRetries = blueprintConfig.DisableInternalRetries;
            agentConfig.HttpManager = blueprintConfig.HttpManager;

            // Logging: copy whichever logger the Blueprint uses (IdentityLogger or LoggingCallback)
            agentConfig.IdentityLogger = blueprintConfig.IdentityLogger;
            agentConfig.LoggingCallback = blueprintConfig.LoggingCallback;
            agentConfig.LogLevel = blueprintConfig.LogLevel;
            agentConfig.EnablePiiLogging = blueprintConfig.EnablePiiLogging;
            agentConfig.IsDefaultPlatformLoggingEnabled = blueprintConfig.IsDefaultPlatformLoggingEnabled;

            // Telemetry: attribute network calls to the same caller
            agentConfig.ClientName = blueprintConfig.ClientName;
            agentConfig.ClientVersion = blueprintConfig.ClientVersion;

            // Instance discovery: honor the Blueprint's custom metadata or disabled discovery
            agentConfig.CustomInstanceDiscoveryMetadata = blueprintConfig.CustomInstanceDiscoveryMetadata;
            agentConfig.CustomInstanceDiscoveryMetadataUri = blueprintConfig.CustomInstanceDiscoveryMetadataUri;
            agentConfig.IsInstanceDiscoveryEnabled = blueprintConfig.IsInstanceDiscoveryEnabled;
        }

        /// <summary>
        /// Propagates per-request parameters from the outer AcquireTokenForAgent call to an inner
        /// token request builder (Leg 2, Leg 3, or Silent). This ensures that caller-specified
        /// correlation IDs, claims challenges, tenant overrides, and extra query parameters
        /// flow through to the Agent CCA's network calls.
        /// </summary>
        private T PropagateOuterRequestParameters<T>(T builder)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            var outerParams = AuthenticationRequestParameters;

            // Correlation ID: chain inner calls to the same trace
            builder.WithCorrelationId(outerParams.CorrelationId);

            // Claims: propagate merged claims + client capabilities so the inner request
            // includes any conditional access challenge from the caller
            if (!string.IsNullOrEmpty(outerParams.ClaimsAndClientCapabilities))
            {
                builder.WithClaims(outerParams.ClaimsAndClientCapabilities);
            }

            // Tenant override: if the caller used .WithTenantId() on AcquireTokenForAgent,
            // apply the same override to the inner call
            if (outerParams.AuthorityOverride != null)
            {
                var overrideAuthority = Authority.CreateAuthority(outerParams.AuthorityOverride);
                builder.WithTenantId(overrideAuthority.TenantId);
            }

            // Extra query parameters: already merged (app-level + request-level) in outerParams
            if (outerParams.ExtraQueryParameters != null && outerParams.ExtraQueryParameters.Count > 0)
            {
                builder.CommonParameters.ExtraQueryParameters = 
                    new Dictionary<string, string>(outerParams.ExtraQueryParameters, StringComparer.OrdinalIgnoreCase);
            }

            return builder;
        }

        #endregion
    }
}

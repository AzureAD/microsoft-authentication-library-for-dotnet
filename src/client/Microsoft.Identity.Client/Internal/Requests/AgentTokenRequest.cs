// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class AgentTokenRequest : RequestBase
    {
        private readonly AcquireTokenForAgentParameters _agentParameters;
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

            if (!agentIdentity.HasUserIdentifier)
            {
                // App-only flow: get a client credential token for the agent.
                // AcquireTokenForClient has built-in cache-first logic, so CCA persistence
                // is sufficient — no explicit silent call needed.
                var agentCca = GetOrCreateAgentCca(agentAppId, authority);

                return await agentCca
                    .AcquireTokenForClient(AuthenticationRequestParameters.Scope)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // User identity flow
            var mainCca = GetOrCreateAgentCca(agentAppId, authority);

            // Try cache first via AcquireTokenSilent (unless ForceRefresh is set)
            if (!_agentParameters.ForceRefresh)
            {
                var cachedResult = await TryAcquireTokenSilentAsync(
                    mainCca,
                    agentIdentity,
                    AuthenticationRequestParameters.Scope,
                    cancellationToken).ConfigureAwait(false);
                if (cachedResult != null)
                {
                    return cachedResult;
                }
            }

            // Cache miss or ForceRefresh — execute the full Leg 2 + Leg 3 flow

            // Step 1: Get assertion token via FMI path
            var assertionApp = GetOrCreateAssertionCca(agentAppId, authority);

            var assertionResult = await assertionApp
                .AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithFmiPathForClientAssertion(agentAppId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            string assertion = assertionResult.AccessToken;

            // Step 2: Exchange assertion for user token via UserFIC
            if (agentIdentity.UserObjectId.HasValue)
            {
                return await ((IByUserFederatedIdentityCredential)mainCca)
                    .AcquireTokenByUserFederatedIdentityCredential(
                        AuthenticationRequestParameters.Scope,
                        agentIdentity.UserObjectId.Value,
                        assertion)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return await ((IByUserFederatedIdentityCredential)mainCca)
                .AcquireTokenByUserFederatedIdentityCredential(
                    AuthenticationRequestParameters.Scope,
                    agentIdentity.Username,
                    assertion)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to find a cached token for the specified user on the agent CCA.
        /// Returns null if no matching account is found or the silent call fails.
        /// </summary>
        private static async Task<AuthenticationResult> TryAcquireTokenSilentAsync(
            IConfidentialClientApplication agentCca,
            AgentIdentity agentIdentity,
            IEnumerable<string> scopes,
            CancellationToken cancellationToken)
        {
#pragma warning disable CS0618 // GetAccountsAsync is obsolete for external callers but needed here to enumerate cached accounts
            var accounts = await agentCca.GetAccountsAsync().ConfigureAwait(false);
#pragma warning restore CS0618

            IAccount matchedAccount = FindMatchingAccount(accounts, agentIdentity);
            if (matchedAccount == null)
            {
                return null;
            }

            try
            {
                return await agentCca
                    .AcquireTokenSilent(scopes, matchedAccount)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                // Token expired or requires interaction — fall through to full flow
                return null;
            }
        }

        /// <summary>
        /// Finds an account in the cache that matches the agent identity by OID or UPN.
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
            // CCS headers are handled by the internal CCAs' own request handlers.
            return null;
        }

        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";
        private const string AgentCcaKeyPrefix = "agent_";
        private const string AssertionCcaKeyPrefix = "assertion_";

        private IConfidentialClientApplication GetOrCreateAgentCca(string agentAppId, string authority)
        {
            string key = AgentCcaKeyPrefix + agentAppId;
            return _blueprintApplication.AgentCcaCache.GetOrAdd(key, _ => BuildAgentCca(agentAppId, authority));
        }

        private IConfidentialClientApplication GetOrCreateAssertionCca(string agentAppId, string authority)
        {
            string key = AssertionCcaKeyPrefix + agentAppId;
            return _blueprintApplication.AgentCcaCache.GetOrAdd(key, _ => BuildAssertionApp(agentAppId, authority));
        }

        private IConfidentialClientApplication BuildAgentCca(string agentAppId, string authority)
        {
            var builder = ConfidentialClientApplicationBuilder
                .Create(agentAppId)
                .WithAuthority(authority)
                .WithExperimentalFeatures(true)
                .WithClientAssertion((AssertionRequestOptions _) => GetFmiCredentialAsync(agentAppId));

            PropagateHttpConfig(builder);
            return builder.Build();
        }

        private IConfidentialClientApplication BuildAssertionApp(string agentAppId, string authority)
        {
            var builder = ConfidentialClientApplicationBuilder
                .Create(agentAppId)
                .WithAuthority(authority)
                .WithExperimentalFeatures(true)
                .WithClientAssertion(async (AssertionRequestOptions opts) =>
                {
                    string fmiPath = opts.ClientAssertionFmiPath ?? agentAppId;
                    return await GetFmiCredentialAsync(fmiPath).ConfigureAwait(false);
                });

            PropagateHttpConfig(builder);
            return builder.Build();
        }

        /// <summary>
        /// Propagates HTTP configuration from the blueprint CCA to an internal CCA builder,
        /// ensuring that custom HTTP client factories (e.g., proxy settings) and internal
        /// HTTP managers (used in tests) are shared with the internal CCAs.
        /// </summary>
        private void PropagateHttpConfig(ConfidentialClientApplicationBuilder builder)
        {
            var blueprintConfig = _blueprintApplication.ServiceBundle.Config;

            if (blueprintConfig.HttpClientFactory != null)
            {
                builder.WithHttpClientFactory(blueprintConfig.HttpClientFactory);
            }

            if (blueprintConfig.HttpManager != null)
            {
                builder.WithHttpManager(blueprintConfig.HttpManager);
            }
        }

        private async Task<string> GetFmiCredentialAsync(string fmiPath)
        {
            var result = await _blueprintApplication
                .AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithFmiPath(fmiPath)
                .ExecuteAsync()
                .ConfigureAwait(false);

            return result.AccessToken;
        }
    }
}

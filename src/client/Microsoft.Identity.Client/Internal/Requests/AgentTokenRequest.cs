// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
                // App-only flow: get a client credential token for the agent
                var agentCca = BuildAgentCca(agentAppId, authority);

                return await agentCca
                    .AcquireTokenForClient(AuthenticationRequestParameters.Scope)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // User identity flow
            // Step 1: Get assertion token via FMI path
            var assertionApp = BuildAssertionApp(agentAppId, authority);

            var assertionResult = await assertionApp
                .AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithFmiPathForClientAssertion(agentAppId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            string assertion = assertionResult.AccessToken;

            // Step 2: Exchange assertion for user token via UserFIC
            var mainCca = BuildAgentCca(agentAppId, authority);

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

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            // CCS headers are handled by the internal CCAs' own request handlers.
            return null;
        }

        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";

        private IConfidentialClientApplication BuildAgentCca(string agentAppId, string authority)
        {
            return ConfidentialClientApplicationBuilder
                .Create(agentAppId)
                .WithAuthority(authority)
                .WithExperimentalFeatures(true)
                .WithClientAssertion((AssertionRequestOptions _) => GetFmiCredentialAsync(agentAppId))
                .Build();
        }

        private IConfidentialClientApplication BuildAssertionApp(string agentAppId, string authority)
        {
            return ConfidentialClientApplicationBuilder
                .Create(agentAppId)
                .WithAuthority(authority)
                .WithExperimentalFeatures(true)
                .WithClientAssertion(async (AssertionRequestOptions opts) =>
                {
                    string fmiPath = opts.ClientAssertionFmiPath ?? agentAppId;
                    return await GetFmiCredentialAsync(fmiPath).ConfigureAwait(false);
                })
                .Build();
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

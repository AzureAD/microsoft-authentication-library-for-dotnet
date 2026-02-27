// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using static Microsoft.Identity.Client.TelemetryCore.Internal.Events.ApiEvent;

namespace Microsoft.Identity.Client
{
    /// <inheritdoc cref="IConfidentialClientApplication"/>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public sealed partial class ConfidentialClientApplication
        : ClientApplicationBase,
            IConfidentialClientApplication,
            IByRefreshToken,
            ILongRunningWebApi,
            IByUsernameAndPassword
    {
        /// <summary>
        /// Instructs MSAL to try to auto discover the Azure region.
        /// </summary>
        public const string AttemptRegionDiscovery = "TryAutoDetect";

        internal ConfidentialClientApplication(
            ApplicationConfiguration configuration)
            : base(configuration)
        {
            GuardMobileFrameworks();

            AppTokenCacheInternal = configuration.AppTokenCacheInternalForTest ?? new TokenCache(ServiceBundle, true);
            Certificate = configuration.ClientCredentialCertificate;

            this.ServiceBundle.ApplicationLogger.Verbose(() => $"ConfidentialClientApplication {configuration.GetHashCode()} created");
        }

        /// <inheritdoc/>
        public AcquireTokenByAuthorizationCodeParameterBuilder AcquireTokenByAuthorizationCode(
            IEnumerable<string> scopes,
            string authorizationCode)
        {
            return AcquireTokenByAuthorizationCodeParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes,
                authorizationCode);
        }

        /// <inheritdoc/>
        public AcquireTokenForClientParameterBuilder AcquireTokenForClient(
            IEnumerable<string> scopes)
        {
            return AcquireTokenForClientParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes);
        }

        /// <inheritdoc/>
        public AcquireTokenOnBehalfOfParameterBuilder AcquireTokenOnBehalfOf(
            IEnumerable<string> scopes,
            UserAssertion userAssertion)
        {
            if (userAssertion == null)
            {
                ServiceBundle.ApplicationLogger.Error("User assertion for OBO request should not be null");
                throw new MsalClientException(MsalError.UserAssertionNullError);
            }

            return AcquireTokenOnBehalfOfParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes,
                userAssertion);
        }

        /// <inheritdoc/>
        public AcquireTokenOnBehalfOfParameterBuilder InitiateLongRunningProcessInWebApi(
            IEnumerable<string> scopes,
            string userToken,
            ref string longRunningProcessSessionKey)
        {
            if (string.IsNullOrEmpty(userToken))
            {
                throw new ArgumentNullException(nameof(userToken));
            }

            UserAssertion userAssertion = new UserAssertion(userToken);

            if (string.IsNullOrEmpty(longRunningProcessSessionKey))
            {
                longRunningProcessSessionKey = userAssertion.AssertionHash;
            }

            return AcquireTokenOnBehalfOfParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes,
                userAssertion,
                longRunningProcessSessionKey);
        }

        /// <inheritdoc/>
        public AcquireTokenOnBehalfOfParameterBuilder AcquireTokenInLongRunningProcess(
            IEnumerable<string> scopes,
            string longRunningProcessSessionKey)
        {
            if (string.IsNullOrEmpty(longRunningProcessSessionKey))
            {
                throw new ArgumentNullException(nameof(longRunningProcessSessionKey));
            }

            return AcquireTokenOnBehalfOfParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes,
                longRunningProcessSessionKey);
        }

        /// <summary>
        /// Stops an in-progress long-running on-behalf-of session by removing the tokens associated with the provided cache key.
        /// See <see href="https://aka.ms/msal-net-long-running-obo">Long-running OBO in MSAL.NET</see>.
        /// </summary>
        /// <param name="longRunningProcessSessionKey">OBO cache key used to remove the tokens.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if tokens are removed from the cache; false, otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="longRunningProcessSessionKey"/> is not set.</exception>
        public async Task<bool> StopLongRunningProcessInWebApiAsync(string longRunningProcessSessionKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(longRunningProcessSessionKey))
            {
                throw new ArgumentNullException(nameof(longRunningProcessSessionKey));
            }

            Guid correlationId = Guid.NewGuid();
            RequestContext requestContext = base.CreateRequestContext(correlationId, null, cancellationToken);
            requestContext.ApiEvent = new ApiEvent(correlationId);
            requestContext.ApiEvent.ApiId = ApiIds.RemoveOboTokens;

            var authority = await Instance.Authority.CreateAuthorityForRequestAsync(
              requestContext,
              null).ConfigureAwait(false);

            var authParameters = new AuthenticationRequestParameters(
                   ServiceBundle,
                   UserTokenCacheInternal,
                   new AcquireTokenCommonParameters() { ApiId = requestContext.ApiEvent.ApiId },
                   requestContext,
                   authority);

            if (UserTokenCacheInternal != null)
            {
                return await UserTokenCacheInternal.StopLongRunningOboProcessAsync(longRunningProcessSessionKey, authParameters).ConfigureAwait(false);
            }

            return false;
        }

        /// <inheritdoc/>
        public GetAuthorizationRequestUrlParameterBuilder GetAuthorizationRequestUrl(
            IEnumerable<string> scopes)
        {
            return GetAuthorizationRequestUrlParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes);
        }

        /// <inheritdoc/>
        AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder IByUsernameAndPassword.AcquireTokenByUsernamePassword(
            IEnumerable<string> scopes,
            string username,
            string password)
        {
            return AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes,
                username,
                password);
        }

        AcquireTokenByRefreshTokenParameterBuilder IByRefreshToken.AcquireTokenByRefreshToken(
            IEnumerable<string> scopes,
            string refreshToken)
        {
            return AcquireTokenByRefreshTokenParameterBuilder.Create(
                ClientExecutorFactory.CreateClientApplicationBaseExecutor(this),
                scopes,
                refreshToken);
        }

        /// <inheritdoc/>
        public ITokenCache AppTokenCache => AppTokenCacheInternal;

        /// <summary>
        /// The certificate used to create this <see cref="ConfidentialClientApplication"/>, if any.
        /// </summary>
        public X509Certificate2 Certificate { get; }

        // Stores all app tokens
        internal ITokenCacheInternal AppTokenCacheInternal { get; }

        // Cache of agent CCAs keyed by agent identity, used by AcquireTokenForAgent
        private readonly ConcurrentDictionary<string, IConfidentialClientApplication> _agentCcaCache =
            new ConcurrentDictionary<string, IConfidentialClientApplication>();

        /// <inheritdoc/>
        public AcquireTokenForAgentParameterBuilder AcquireTokenForAgent(
            string agentId, IEnumerable<string> scopes)
        {
            if (string.IsNullOrEmpty(agentId))
                throw new ArgumentNullException(nameof(agentId));
            if (scopes == null)
                throw new ArgumentNullException(nameof(scopes));

            return new AcquireTokenForAgentParameterBuilder(this, agentId, scopes);
        }

        /// <inheritdoc/>
        public AcquireTokenForAgentOnBehalfOfUserParameterBuilder AcquireTokenForAgentOnBehalfOfUser(
            string agentId, IEnumerable<string> scopes, string userPrincipalName)
        {
            if (string.IsNullOrEmpty(agentId))
                throw new ArgumentNullException(nameof(agentId));
            if (scopes == null)
                throw new ArgumentNullException(nameof(scopes));
            if (string.IsNullOrEmpty(userPrincipalName))
                throw new ArgumentNullException(nameof(userPrincipalName));

            return new AcquireTokenForAgentOnBehalfOfUserParameterBuilder(this, agentId, scopes, userPrincipalName);
        }

        /// <summary>
        /// Executes the two-step agentic token acquisition:
        ///   1. Gets FIC from the token exchange endpoint using this CCA's credential.
        ///   2. Uses the FIC as a client assertion in an agent CCA to acquire the target token.
        /// </summary>
        internal async Task<AuthenticationResult> ExecuteAgentTokenAcquisitionAsync(
            string agentId,
            IEnumerable<string> scopes,
            bool forceRefresh,
            Guid? correlationId,
            CancellationToken cancellationToken)
        {
            var agentCca = GetOrCreateAgentCca(agentId);

            var builder = agentCca.AcquireTokenForClient(scopes);

            if (forceRefresh)
                builder = builder.WithForceRefresh(true);

            if (correlationId.HasValue)
                builder = builder.WithCorrelationId(correlationId.Value);

            return await builder
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the three-step agentic user-delegated token acquisition:
        ///   1. Gets FIC from the token exchange endpoint using this CCA's credential (via the agent CCA's assertion).
        ///   2. Gets a User FIC via AcquireTokenForClient + WithFmiPathForClientAssertion on the agent CCA.
        ///   3. Uses the User FIC in a user_fic grant type request to get a user-delegated token.
        /// </summary>
        internal async Task<AuthenticationResult> ExecuteAgentOnBehalfOfUserAsync(
            string agentId,
            IEnumerable<string> scopes,
            string userPrincipalName,
            bool forceRefresh,
            Guid? correlationId,
            CancellationToken cancellationToken)
        {
            var agentCca = GetOrCreateAgentCca(agentId);
            var ficAudience = ServiceBundle.Config.FederatedCredentialAudience;

            // Step 1 + 2: Get User FIC.
            // The agent CCA's assertion callback already handles step 1 (getting the app FIC
            // from the platform CCA). WithFmiPathForClientAssertion passes the agentId to
            // that callback. The result is a User FIC token.
            var userFicResult = await agentCca.AcquireTokenForClient(new[] { ficAudience })
                .WithFmiPathForClientAssertion(agentId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            string userFicAssertion = userFicResult.AccessToken;

            // Step 3: Exchange the User FIC for a user-delegated token using user_fic grant.
            var usernamePasswordBuilder = ((IByUsernameAndPassword)agentCca)
                .AcquireTokenByUsernamePassword(scopes, userPrincipalName, "no_password");

            if (correlationId.HasValue)
                usernamePasswordBuilder = usernamePasswordBuilder.WithCorrelationId(correlationId.Value);

            return await usernamePasswordBuilder
                .OnBeforeTokenRequest(async (request) =>
                {
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

                    await Task.CompletedTask.ConfigureAwait(false);
                })
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private IConfidentialClientApplication GetOrCreateAgentCca(string agentId)
        {
            return _agentCcaCache.GetOrAdd(agentId, id =>
            {
                var config = ServiceBundle.Config;
                var authorityUri = config.Authority.AuthorityInfo.CanonicalAuthority.ToString();
                var ficAudience = config.FederatedCredentialAudience;

                // The agent CCA uses a client assertion that fetches the FIC on demand
                // from *this* (platform) CCA.
                var platformCca = this as IConfidentialClientApplication;

                var agentBuilder = ConfidentialClientApplicationBuilder
                    .Create(id)
                    .WithAuthority(authorityUri)
                    .WithExperimentalFeatures(true)
                    .WithClientAssertion(async (AssertionRequestOptions options) =>
                    {
                        string fmiPath = options.ClientAssertionFmiPath ?? id;
                        var result = await platformCca.AcquireTokenForClient(new[] { ficAudience })
                            .WithFmiPath(fmiPath)
                            .ExecuteAsync()
                            .ConfigureAwait(false);
                        return result.AccessToken;
                    });

                if (config.AccessorOptions != null)
                    agentBuilder = agentBuilder.WithCacheOptions(config.AccessorOptions);
                if (config.HttpClientFactory != null)
                    agentBuilder = agentBuilder.WithHttpClientFactory(config.HttpClientFactory);
                if (config.IdentityLogger != null)
                    agentBuilder = agentBuilder.WithLogging(config.IdentityLogger, config.EnablePiiLogging);
                if (config.HttpManager != null)
                    agentBuilder = agentBuilder.WithHttpManager(config.HttpManager);
                if (!config.IsInstanceDiscoveryEnabled)
                    agentBuilder = agentBuilder.WithInstanceDiscovery(false);

                return agentBuilder.Build();
            });
        }

        internal override async Task<AuthenticationRequestParameters> CreateRequestParametersAsync(
            AcquireTokenCommonParameters commonParameters,
            RequestContext requestContext,
            ITokenCacheInternal cache,
            CancellationToken cancellationToken)
        {
            AuthenticationRequestParameters requestParams = await base.CreateRequestParametersAsync(commonParameters, requestContext, cache, cancellationToken).ConfigureAwait(false);
            return requestParams;
        }
    }
}

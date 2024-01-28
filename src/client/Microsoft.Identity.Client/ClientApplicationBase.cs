// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using static Microsoft.Identity.Client.TelemetryCore.Internal.Events.ApiEvent;

namespace Microsoft.Identity.Client
{
    /// <inheritdoc/>
    public abstract partial class ClientApplicationBase : ApplicationBase, IClientApplicationBase
    {
        /// <summary>
        /// Details on the configuration of the ClientApplication for debugging purposes.
        /// </summary>
        public IAppConfig AppConfig => ServiceBundle.Config;

        /// <inheritdoc/>
        public ITokenCache UserTokenCache => UserTokenCacheInternal;

        internal ITokenCacheInternal UserTokenCacheInternal { get; }

        /// <inheritdoc/>
        public string Authority => ServiceBundle.Config.Authority.AuthorityInfo.CanonicalAuthority?.ToString(); // Do not use in MSAL, use AuthorityInfo instead to avoid re-parsing

        internal AuthorityInfo AuthorityInfo => ServiceBundle.Config.Authority.AuthorityInfo;

        internal ClientApplicationBase(ApplicationConfiguration config) : base(config)
        {
            ICacheSerializationProvider defaultCacheSerialization = ServiceBundle.PlatformProxy.CreateTokenCacheBlobStorage();

            if (config.UserTokenLegacyCachePersistenceForTest != null)
            {
                UserTokenCacheInternal = new TokenCache(ServiceBundle, config.UserTokenLegacyCachePersistenceForTest, false, defaultCacheSerialization);
            }
            else
            {
                UserTokenCacheInternal = config.UserTokenCacheInternalForTest ?? new TokenCache(ServiceBundle, false, defaultCacheSerialization);
            }
        }

        #region Accounts
        /// <inheritdoc/>
        public Task<IEnumerable<IAccount>> GetAccountsAsync()
        {
            return GetAccountsAsync(default(CancellationToken));
        }

        // TODO: MSAL 5 - add cancellationToken to the interface
        /// <summary>
        /// Returns all the available <see cref="IAccount">accounts</see> in the user token cache for the application.
        /// </summary>
        public Task<IEnumerable<IAccount>> GetAccountsAsync(CancellationToken cancellationToken = default)
        {
            return GetAccountsInternalAsync(ApiIds.GetAccounts, null, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<IAccount>> GetAccountsAsync(string userFlow)
        {
            return GetAccountsAsync(userFlow, default);
        }

        // TODO: MSAL 5 - add cancellationToken to the interface
        /// <summary>
        /// Get the <see cref="IAccount"/> collection by its identifier among the accounts available in the token cache,
        /// based on the user flow. This is for Azure AD B2C scenarios.
        /// </summary>
        /// <param name="userFlow">The identifier is the user flow being targeted by the specific B2C authority/>.
        /// </param>
        /// <param name="cancellationToken">Cancellation token </param>
        public async Task<IEnumerable<IAccount>> GetAccountsAsync(string userFlow, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userFlow))
            {
                throw new ArgumentException($"{nameof(userFlow)} should not be null or whitespace", nameof(userFlow));
            }

            var accounts = await GetAccountsInternalAsync(ApiIds.GetAccountsByUserFlow, null, cancellationToken).ConfigureAwait(false);

            return accounts.Where(acc =>
                acc.HomeAccountId.ObjectId.EndsWith(
                    userFlow, StringComparison.OrdinalIgnoreCase));
        }

        // TODO: MSAL 5 - add cancellationToken to the interface
        /// <summary>
        /// Get the <see cref="IAccount"/> by its identifier among the accounts available in the token cache.
        /// </summary>
        /// <param name="accountId">Account identifier. The identifier is typically the
        /// value of the <see cref="AccountId.Identifier"/> property of <see cref="AccountId"/>.
        /// You typically get the account ID from an <see cref="IAccount"/> by using the <see cref="IAccount.HomeAccountId"/> property>
        /// </param>
        /// <param name="cancellationToken">Cancellation token </param>
        public async Task<IAccount> GetAccountAsync(string accountId, CancellationToken cancellationToken = default)
        {
            var accounts = await GetAccountsInternalAsync(ApiIds.GetAccountById, accountId, cancellationToken).ConfigureAwait(false);
            return accounts.SingleOrDefault();
        }

        /// <inheritdoc/>
        public async Task<IAccount> GetAccountAsync(string accountId)
        {
            if (!string.IsNullOrWhiteSpace(accountId))
            {
                return await GetAccountAsync(accountId, default).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Removes all tokens in the cache for the specified account.
        /// </summary>
        /// <param name="account">Instance of the account that needs to be removed</param>
        public Task RemoveAsync(IAccount account)
        {
            return RemoveAsync(account, default);
        }

        // TODO: MSAL 5 - add cancellationToken to the interface

        /// <summary>
        /// Removes all tokens in the cache for the specified account.
        /// </summary>
        /// <param name="account">Instance of the account that needs to be removed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task RemoveAsync(IAccount account, CancellationToken cancellationToken = default)
        {
            Guid correlationId = Guid.NewGuid();
            RequestContext requestContext = CreateRequestContext(correlationId, cancellationToken);
            requestContext.ApiEvent = new ApiEvent(correlationId);
            requestContext.ApiEvent.ApiId = ApiIds.RemoveAccount;

            var authority = await Microsoft.Identity.Client.Instance.Authority.CreateAuthorityForRequestAsync(
              requestContext,
              null).ConfigureAwait(false);

            var authParameters = new AuthenticationRequestParameters(
                   ServiceBundle,
                   UserTokenCacheInternal,
                   new AcquireTokenCommonParameters() { ApiId = requestContext.ApiEvent.ApiId },
                   requestContext,
                   authority);

            if (account != null && UserTokenCacheInternal != null)
            {
                await UserTokenCacheInternal.RemoveAccountAsync(account, authParameters).ConfigureAwait(false);
            }

            if (AppConfig.IsBrokerEnabled && ServiceBundle.PlatformProxy.CanBrokerSupportSilentAuth())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var broker = ServiceBundle.PlatformProxy.CreateBroker(ServiceBundle.Config, null);
                if (broker.IsBrokerInstalledAndInvokable(authority.AuthorityInfo.AuthorityType))
                {
                    await broker.RemoveAccountAsync(ServiceBundle.Config, account).ConfigureAwait(false);
                }
            }
        }

        private async Task<IEnumerable<IAccount>> GetAccountsInternalAsync(ApiIds apiId, string homeAccountIdFilter, CancellationToken cancellationToken)
        {
            Guid correlationId = Guid.NewGuid();
            RequestContext requestContext = CreateRequestContext(correlationId, cancellationToken);
            requestContext.ApiEvent = new ApiEvent(correlationId);
            requestContext.ApiEvent.ApiId = apiId;

            var authority = await Microsoft.Identity.Client.Instance.Authority.CreateAuthorityForRequestAsync(
              requestContext,
              null).ConfigureAwait(false);

            var authParameters = new AuthenticationRequestParameters(
                   ServiceBundle,
                   UserTokenCacheInternal,
                   new AcquireTokenCommonParameters() { ApiId = apiId },
                   requestContext,
                   authority,
                   homeAccountIdFilter);

            // a simple session consisting of a single call
            var cacheSessionManager = new CacheSessionManager(
                UserTokenCacheInternal,
                authParameters);

            var accountsFromCache = await cacheSessionManager.GetAccountsAsync().ConfigureAwait(false);
            var accountsFromBroker = await GetAccountsFromBrokerAsync(homeAccountIdFilter, cacheSessionManager, cancellationToken).ConfigureAwait(false);
            accountsFromCache ??= Enumerable.Empty<IAccount>();
            accountsFromBroker ??= Enumerable.Empty<IAccount>();

            ServiceBundle.ApplicationLogger.Info(() => $"Found {accountsFromCache.Count()} cache accounts and {accountsFromBroker.Count()} broker accounts");
            IEnumerable<IAccount> cacheAndBrokerAccounts = MergeAccounts(accountsFromCache, accountsFromBroker);

            ServiceBundle.ApplicationLogger.Info(() => $"Returning {cacheAndBrokerAccounts.Count()} accounts");
            return cacheAndBrokerAccounts;
        }

        private async Task<IEnumerable<IAccount>> GetAccountsFromBrokerAsync(
            string homeAccountIdFilter,
            ICacheSessionManager cacheSessionManager,
            CancellationToken cancellationToken)
        {
            if (AppConfig.IsBrokerEnabled && ServiceBundle.PlatformProxy.CanBrokerSupportSilentAuth())
            {
                var broker = ServiceBundle.PlatformProxy.CreateBroker(ServiceBundle.Config, null);
                if (broker.IsBrokerInstalledAndInvokable(ServiceBundle.Config.Authority.AuthorityInfo.AuthorityType))
                {
                    var brokerAccounts =
                        (await broker.GetAccountsAsync(
                            AppConfig.ClientId,
                            AppConfig.RedirectUri,
                            AuthorityInfo,
                            cacheSessionManager,
                            ServiceBundle.InstanceDiscoveryManager).ConfigureAwait(false))
                        ?? Enumerable.Empty<IAccount>();

                    if (!string.IsNullOrEmpty(homeAccountIdFilter))
                    {
                        brokerAccounts = brokerAccounts.Where(
                            acc => homeAccountIdFilter.Equals(
                                acc.HomeAccountId.Identifier,
                                StringComparison.OrdinalIgnoreCase));
                    }

                    brokerAccounts = await FilterBrokerAccountsByEnvAsync(brokerAccounts, cancellationToken).ConfigureAwait(false);
                    return brokerAccounts;
                }
            }

            return Enumerable.Empty<IAccount>();
        }

        // Not all brokers return the accounts only for the given env
        private async Task<IEnumerable<IAccount>> FilterBrokerAccountsByEnvAsync(IEnumerable<IAccount> brokerAccounts, CancellationToken cancellationToken)
        {
            ServiceBundle.ApplicationLogger.Verbose(() => $"Filtering broker accounts by environment. Before filtering: " + brokerAccounts.Count());

            ISet<string> allEnvs = new HashSet<string>(
                brokerAccounts.Select(aci => aci.Environment),
                StringComparer.OrdinalIgnoreCase);

            var instanceMetadata = await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                AuthorityInfo,
                allEnvs,
                CreateRequestContext(Guid.NewGuid(), cancellationToken)).ConfigureAwait(false);

            brokerAccounts = brokerAccounts.Where(acc => instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(acc.Environment));

            ServiceBundle.ApplicationLogger.Verbose(() => $"After filtering: " + brokerAccounts.Count());

            return brokerAccounts;
        }

        private IEnumerable<IAccount> MergeAccounts(
            IEnumerable<IAccount> cacheAccounts,
            IEnumerable<IAccount> brokerAccounts)
        {
            List<IAccount> allAccounts = new List<IAccount>(cacheAccounts);

            foreach (IAccount account in brokerAccounts)
            {
                if (!allAccounts.Any(x => x.HomeAccountId.Equals(account.HomeAccountId))) // AccountId is equatable
                {
                    allAccounts.Add(account);
                }
                else
                {
                    ServiceBundle.ApplicationLogger.InfoPii(
                        () => "Account merge eliminated broker account with ID: " + account.HomeAccountId,
                        () => "Account merge eliminated an account");
                }
            }

            return allAccounts;
        }

        // This implementation should ONLY be called for cases where we aren't participating in
        // MATS telemetry but still need a requestcontext/logger, such as "GetAccounts()".
        // For service calls, the request context should be created in the **Executor classes as part of request execution.
        internal RequestContext CreateRequestContext(Guid correlationId, CancellationToken cancellationToken)
        {
            return new RequestContext(ServiceBundle, correlationId, cancellationToken);
        }

        #endregion

        /// <inheritdoc/>
        public AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, IAccount account)
        {
            return AcquireTokenSilentParameterBuilder.Create(
                ClientExecutorFactory.CreateClientApplicationBaseExecutor(this),
                scopes,
                account);
        }

        /// <inheritdoc/>
        public AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, string loginHint)
        {
            if (string.IsNullOrWhiteSpace(loginHint))
            {
                throw new ArgumentNullException(nameof(loginHint));
            }

            return AcquireTokenSilentParameterBuilder.Create(
                ClientExecutorFactory.CreateClientApplicationBaseExecutor(this),
                scopes,
                loginHint);
        }
    }
}

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
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using static Microsoft.Identity.Client.TelemetryCore.Internal.Events.ApiEvent;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Abstract class containing common API methods and properties. Both <see cref="Microsoft.Identity.Client.PublicClientApplication"/> and 
    /// ConfidentialClientApplication
    /// extend this class. For details see https://aka.ms/msal-net-client-applications
    /// </summary>
    public abstract partial class ClientApplicationBase : IClientApplicationBase
    {
        /// <summary>
        /// Default Authority used for interactive calls.
        /// </summary>
        internal const string DefaultAuthority = "https://login.microsoftonline.com/common/";

        internal IServiceBundle ServiceBundle { get; }

        /// <summary>
        /// Details on the configuration of the ClientApplication for debugging purposes.
        /// </summary>
        public IAppConfig AppConfig => ServiceBundle.Config;

        /// <summary>
        /// Gets the URL of the authority, or security token service (STS) from which MSAL.NET will acquire security tokens
        /// The return value of this property is either the value provided by the developer in the constructor of the application, or otherwise
        /// the value of the <see cref="DefaultAuthority"/> static member (that is <c>https://login.microsoftonline.com/common/</c>)
        /// </summary>
        public string Authority => ServiceBundle.Config.Authority.AuthorityInfo.CanonicalAuthority; // Do not use in MSAL, use AuthorityInfo instead to avoid re-parsing

        internal AuthorityInfo AuthorityInfo => ServiceBundle.Config.Authority.AuthorityInfo;

        /// <summary>
        /// User token cache. It holds access tokens, id tokens and refresh tokens for accounts. It's used
        /// and updated silently if needed when calling <see cref="AcquireTokenSilent(IEnumerable{string}, IAccount)"/>
        /// or one of the overrides of <see cref="AcquireTokenSilent(IEnumerable{string}, IAccount)"/>.
        /// It is updated by each AcquireTokenXXX method, with the exception of <c>AcquireTokenForClient</c> which only uses the application
        /// cache (see <c>IConfidentialClientApplication</c>).
        /// </summary>
        /// <remarks>On .NET Framework and .NET Core you can also customize the token cache serialization.
        /// See https://aka.ms/msal-net-token-cache-serialization. This is taken care of by MSAL.NET on mobile platforms and on UWP.
        /// It is recommended to use token cache serialization for web site and web api scenarios.
        /// </remarks>
        public ITokenCache UserTokenCache => UserTokenCacheInternal;

        internal ITokenCacheInternal UserTokenCacheInternal { get; }

        internal ClientApplicationBase(ApplicationConfiguration config)
        {
            ServiceBundle = Internal.ServiceBundle.Create(config);
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


        internal virtual async Task<AuthenticationRequestParameters> CreateRequestParametersAsync(
            AcquireTokenCommonParameters commonParameters,
            RequestContext requestContext,
            ITokenCacheInternal cache)
        {
            var authority = await Instance.Authority.CreateAuthorityForRequestAsync(
               requestContext,
               commonParameters.AuthorityOverride).ConfigureAwait(false);

            return new AuthenticationRequestParameters(
                ServiceBundle,
                cache,
                commonParameters,
                requestContext,
                authority);
        }

        #region Accounts
        /// <summary>
        /// Returns all the available <see cref="IAccount">accounts</see> in the user token cache for the application.
        /// </summary>
        public async Task<IEnumerable<IAccount>> GetAccountsAsync()
        {
            return await GetAccountsAsync(default(CancellationToken)).ConfigureAwait(false);
        }

        // TODO: MSAL 5 - add cancellationToken to the interface
        /// <summary>
        /// Returns all the available <see cref="IAccount">accounts</see> in the user token cache for the application.
        /// </summary>
        public async Task<IEnumerable<IAccount>> GetAccountsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAccountsInternalAsync(ApiIds.GetAccounts, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the <see cref="IAccount"/> collection by its identifier among the accounts available in the token cache,
        /// based on the user flow. This is for Azure AD B2C scenarios.
        /// </summary>
        /// <param name="userFlow">The identifier is the user flow being targeted by the specific B2C authority/>.
        /// </param>
        public async Task<IEnumerable<IAccount>> GetAccountsAsync(string userFlow)
        {
            return await GetAccountsAsync(userFlow, default(CancellationToken)).ConfigureAwait(false);
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
        /// You typically get the account id from an <see cref="IAccount"/> by using the <see cref="IAccount.HomeAccountId"/> property>
        /// </param>
        /// <param name="cancellationToken">Cancellation token </param>
        public async Task<IAccount> GetAccountAsync(string accountId, CancellationToken cancellationToken = default)
        {
            var accounts = await GetAccountsInternalAsync(ApiIds.GetAccountById, accountId, cancellationToken).ConfigureAwait(false);
            return accounts.SingleOrDefault();
        }

        /// <summary>
        /// Get the <see cref="IAccount"/> by its identifier among the accounts available in the token cache.
        /// </summary>
        /// <param name="accountId">Account identifier. The identifier is typically the
        /// value of the <see cref="AccountId.Identifier"/> property of <see cref="AccountId"/>.
        /// You typically get the account id from an <see cref="IAccount"/> by using the <see cref="IAccount.HomeAccountId"/> property>
        /// </param>
        public async Task<IAccount> GetAccountAsync(string accountId)
        {
            if (!string.IsNullOrWhiteSpace(accountId))
            {
                return await GetAccountAsync(accountId, default(CancellationToken)).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Removes all tokens in the cache for the specified account.
        /// </summary>
        /// <param name="account">Instance of the account that needs to be removed</param>
        public async Task RemoveAsync(IAccount account)
        {
            await RemoveAsync(account, default(CancellationToken)).ConfigureAwait(false);
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
                if (broker.IsBrokerInstalledAndInvokable())
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
            accountsFromCache = accountsFromCache ?? Enumerable.Empty<IAccount>();
            accountsFromBroker = accountsFromBroker ?? Enumerable.Empty<IAccount>();

            ServiceBundle.ApplicationLogger.Info($"Found {accountsFromCache.Count()} cache accounts and {accountsFromBroker.Count()} broker accounts");
            IEnumerable<IAccount> cacheAndBrokerAccounts = MergeAccounts(accountsFromCache, accountsFromBroker);

            ServiceBundle.ApplicationLogger.Info($"Returning {cacheAndBrokerAccounts.Count()} accounts");
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
                if (broker.IsBrokerInstalledAndInvokable())
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
            ServiceBundle.ApplicationLogger.Verbose($"Filtering broker accounts by env. Before filtering: " + brokerAccounts.Count());

            ISet<string> allEnvs = new HashSet<string>(
                brokerAccounts.Select(aci => aci.Environment),
                StringComparer.OrdinalIgnoreCase);

            var instanceMetadata = await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                AuthorityInfo,
                allEnvs,
                CreateRequestContext(Guid.NewGuid(), cancellationToken)).ConfigureAwait(false);


            brokerAccounts = brokerAccounts.Where(acc => instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(acc.Environment));

            ServiceBundle.ApplicationLogger.Verbose($"After filtering: " + brokerAccounts.Count());

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
                        "Account merge eliminated broker account with id: " + account.HomeAccountId,
                        "Account merge eliminated an account");
                }
            }

            return allAccounts;
        }

        // This implementation should ONLY be called for cases where we aren't participating in
        // MATS telemetry but still need a requestcontext/logger, such as "GetAccounts()".
        // For service calls, the request context should be created in the **Executor classes as part of request execution.
        private RequestContext CreateRequestContext(Guid correlationId, CancellationToken cancellationToken)
        {
            return new RequestContext(ServiceBundle, correlationId, cancellationToken);
        }

        #endregion

        /// <summary>
        /// [V3 API] Attempts to acquire an access token for the <paramref name="account"/> from the user token cache.
        /// See https://aka.ms/msal-net-acquiretokensilent for more details
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account for which the token is requested.</param>
        /// <returns>An <see cref="AcquireTokenSilentParameterBuilder"/> used to build the token request, adding optional
        /// parameters</returns>
        /// <exception cref="MsalUiRequiredException">will be thrown in the case where an interaction is required with the end user of the application,
        /// for instance, if no refresh token was in the cache, or the user needs to consent, or re-sign-in (for instance if the password expired),
        /// or the user needs to perform two factor authentication</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes. This means that an access token with more scopes than
        /// requested could be returned. If the access token is expired or close to expiration - within a 5 minute window -
        /// then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        ///
        /// You can set additional parameters by chaining the builder with:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithTenantId(string)"/> 
        /// to request a token for a different authority than the one set at the application construction
        /// <see cref="AcquireTokenSilentParameterBuilder.WithForceRefresh(bool)"/> to bypass the user token cache and
        /// force refreshing the token, as well as
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to
        /// specify extra query parameters
        ///
        /// </remarks>
        public AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, IAccount account)
        {
            return AcquireTokenSilentParameterBuilder.Create(
                ClientExecutorFactory.CreateClientApplicationBaseExecutor(this),
                scopes,
                account);
        }

        /// <summary>
        /// [V3 API] Attempts to acquire an access token for the <see cref="IAccount"/>
        /// having the <see cref="IAccount.Username" /> match the given <paramref name="loginHint"/>, from the user token cache.
        /// See https://aka.ms/msal-net-acquiretokensilent for more details
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Typically the username, in UPN format, e.g. johnd@contoso.com </param>
        /// <returns>An <see cref="AcquireTokenSilentParameterBuilder"/> used to build the token request, adding optional
        /// parameters</returns>
        /// <exception cref="MsalUiRequiredException">will be thrown in the case where an interaction is required with the end user of the application,
        /// for instance, if no refresh token was in the cache, or the user needs to consent, or re-sign-in (for instance if the password expired),
        /// or the user needs to perform two factor authentication</exception>
        /// <remarks>
        /// If multiple <see cref="IAccount"/> match the <paramref name="loginHint"/>, or if there are no matches, an exception is thrown.
        ///
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes. This means that an access token with more scopes than
        /// requested could be returned. If the access token is expired or close to expiration - within a 5 minute window -
        /// then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        ///
        /// You can set additional parameters by chaining the builder with:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithTenantId(string)"/> to request a token for a different authority than the one set at the application construction
        /// <see cref="AcquireTokenSilentParameterBuilder.WithForceRefresh(bool)"/> to bypass the user token cache and
        /// force refreshing the token, as well as
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to
        /// specify extra query parameters
        ///
        /// </remarks>
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

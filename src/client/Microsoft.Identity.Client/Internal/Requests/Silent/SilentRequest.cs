// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests.Silent
{
    internal class SilentRequest : RequestBase
    {
        private readonly AcquireTokenSilentParameters _silentParameters;
        private readonly ISilentAuthRequestStrategy _clientStrategy;
        private readonly Lazy<ISilentAuthRequestStrategy> _brokerStrategyLazy;
        private readonly ILoggerAdapter _logger;

        public SilentRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters silentParameters,
            ISilentAuthRequestStrategy clientStrategyOverride = null,
            ISilentAuthRequestStrategy brokerStrategyOverride = null)
            : base(serviceBundle, authenticationRequestParameters, silentParameters)
        {
            _silentParameters = silentParameters;

            _brokerStrategyLazy = new Lazy<ISilentAuthRequestStrategy>(() => brokerStrategyOverride ?? new BrokerSilentStrategy(this,
                                                                                               serviceBundle,
                                                                                               authenticationRequestParameters,
                                                                                               silentParameters,
                                                                                               serviceBundle.PlatformProxy.CreateBroker(
                                                                                                   serviceBundle.Config, null)));
            _clientStrategy = clientStrategyOverride ?? new CacheSilentStrategy(this, serviceBundle, authenticationRequestParameters, silentParameters);

            _logger = authenticationRequestParameters.RequestContext.Logger;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await UpdateRequestWithAccountAsync().ConfigureAwait(false);

            bool isBrokerConfigured = AuthenticationRequestParameters.AppConfig.IsBrokerEnabled &&
                                      ServiceBundle.PlatformProxy.CanBrokerSupportSilentAuth();

            try
            {
                if (AuthenticationRequestParameters.Account == null)
                {
                    _logger.Verbose(()=>"No account passed to AcquireTokenSilent. ");
                    throw new MsalUiRequiredException(
                       MsalError.UserNullError,
                       MsalErrorMessage.MsalUiRequiredMessage,
                       null,
                       UiRequiredExceptionClassification.AcquireTokenSilentFailed);
                }

                if (isBrokerConfigured)
                {
                    _logger.Info("Broker is configured and enabled, attempting to use broker instead.");
                    var brokerResult = await _brokerStrategyLazy.Value.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                    // fallback to local cache if broker fails
                    if (brokerResult != null)
                    {
                        _logger.Verbose(() => "Broker responded to silent request.");
                        return brokerResult;
                    }
                }

                _logger.Verbose(() => "Attempting to acquire token using local cache.");

                return await _clientStrategy.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MsalException ex)
            {
                _logger.Verbose(() => isBrokerConfigured ? $"Broker could not satisfy silent request." : $"Token cache could not satisfy silent request.");
                throw ex;
            }
        }

        internal new Task<AuthenticationResult> CacheTokenResponseAndCreateAuthenticationResultAsync(MsalTokenResponse response)
        {
            return base.CacheTokenResponseAndCreateAuthenticationResultAsync(response);
        }

        //internal for test
        internal Task<AuthenticationResult> ExecuteTestAsync(CancellationToken cancellationToken)
        {
            return ExecuteAsync(cancellationToken);
        }

        private async Task UpdateRequestWithAccountAsync()
        {
            IAccount account = await GetAccountFromParamsOrLoginHintAsync(
                _silentParameters.Account,
                _silentParameters.LoginHint).ConfigureAwait(false);

            AuthenticationRequestParameters.Account = account;

            // AcquireTokenSilent must not use "common" or "organizations". Instead, use the home tenant id.
            var tenantedAuthority = await Authority.CreateAuthorityForRequestAsync(
                AuthenticationRequestParameters.RequestContext,
                AuthenticationRequestParameters.AuthorityOverride,
                account).ConfigureAwait(false);

            AuthenticationRequestParameters.AuthorityManager =
                new AuthorityManager(
                    AuthenticationRequestParameters.RequestContext,
                    tenantedAuthority);
        }

        private async Task<IAccount> GetSingleAccountForLoginHintAsync(string loginHint)
        {
            if (!string.IsNullOrEmpty(loginHint))
            {
                IReadOnlyList<IAccount> accounts = (await CacheManager.GetAccountsAsync().ConfigureAwait(false))
                    .Where(a => !string.IsNullOrWhiteSpace(a.Username) &&
                           a.Username.Equals(loginHint, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (accounts.Count == 0)
                {
                    throw new MsalUiRequiredException(
                                MsalError.NoAccountForLoginHint,
                                MsalErrorMessage.NoAccountForLoginHint,
                                null,
                                UiRequiredExceptionClassification.AcquireTokenSilentFailed);
                }

                if (accounts.Count > 1)
                {
                    throw new MsalUiRequiredException(
                        MsalError.MultipleAccountsForLoginHint,
                        MsalErrorMessage.MultipleAccountsForLoginHint,
                        null,
                        UiRequiredExceptionClassification.AcquireTokenSilentFailed);
                }

                return accounts[0];
            }

            return null;
        }

        private async Task<IAccount> GetAccountFromParamsOrLoginHintAsync(IAccount account, string loginHint)
        {
            if (account != null)
            {
                return account;
            }

            return await GetSingleAccountForLoginHintAsync(loginHint).ConfigureAwait(false);
        }

        protected override void ValidateAccountIdentifiers(ClientInfo fromServer)
        {
            if (fromServer == null ||
                AuthenticationRequestParameters?.Account?.HomeAccountId == null ||
                PublicClientApplication.IsOperatingSystemAccount(AuthenticationRequestParameters?.Account))
            {
                return;
            }

            if (AuthenticationRequestParameters.AuthorityInfo.AuthorityType == AuthorityType.B2C &&
                fromServer.UniqueTenantIdentifier.Equals(AuthenticationRequestParameters.Account.HomeAccountId.TenantId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (fromServer.UniqueObjectIdentifier.Equals(AuthenticationRequestParameters.Account.HomeAccountId.ObjectId,
                    StringComparison.OrdinalIgnoreCase) &&
                fromServer.UniqueTenantIdentifier.Equals(AuthenticationRequestParameters.Account.HomeAccountId.TenantId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AuthenticationRequestParameters.RequestContext.Logger.Error("Returned user identifiers do not match the sent user identifier");

            AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "User identifier returned by AAD (uid:{0} utid:{1}) does not match the user identifier sent. (uid:{2} utid:{3})",
                    fromServer.UniqueObjectIdentifier,
                    fromServer.UniqueTenantIdentifier,
                    AuthenticationRequestParameters.Account.HomeAccountId.ObjectId,
                    AuthenticationRequestParameters.Account.HomeAccountId.TenantId),
                string.Empty);

            throw new MsalClientException(MsalError.UserMismatch, MsalErrorMessage.UserMismatchSaveToken);
        }
    }
}

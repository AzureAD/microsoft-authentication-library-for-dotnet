// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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

                    if (brokerResult != null)
                    {
                        _logger.Verbose(() => "Broker responded to silent request.");
                        return brokerResult;
                    }
                    else
                    {
                        _logger.Verbose(() => "Broker could not satisfy the silent request.");
                        throw new MsalUiRequiredException(
                           MsalError.FailedToAcquireTokenSilentlyFromBroker,
                           "Broker could not satisfy the silent request.",
                           null,
                           UiRequiredExceptionClassification.AcquireTokenSilentFailed);
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

        internal new async Task<AuthenticationResult> CacheTokenResponseAndCreateAuthenticationResultAsync(MsalTokenResponse response)
        {
            return await base.CacheTokenResponseAndCreateAuthenticationResultAsync(response).ConfigureAwait(false);
        }

        //internal for test
        internal async Task<AuthenticationResult> ExecuteTestAsync(CancellationToken cancellationToken)
        {
            return await ExecuteAsync(cancellationToken).ConfigureAwait(false);
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
    }
}

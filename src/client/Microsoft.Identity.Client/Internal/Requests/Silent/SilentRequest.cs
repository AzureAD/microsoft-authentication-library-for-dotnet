// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Client.Internal.Requests.Silent
{
    internal class SilentRequest : RequestBase
    {
        private readonly AcquireTokenSilentParameters _silentParameters;
        private readonly ISilentAuthRequestStrategy _clientStrategy;
        private readonly Lazy<ISilentAuthRequestStrategy> _brokerStrategyLazy;
        private readonly ICoreLogger _logger;

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
                                                                                               serviceBundle.PlatformProxy.CreateBroker(null)));
            _clientStrategy = clientStrategyOverride ?? new CacheSilentStrategy(this, serviceBundle, authenticationRequestParameters, silentParameters);

            _logger = authenticationRequestParameters.RequestContext.Logger;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await UpdateRequestWithAccountAsync().ConfigureAwait(false);
            bool isBrokerConfigured =
              AuthenticationRequestParameters.IsBrokerConfigured &&
              ServiceBundle.PlatformProxy.CanBrokerSupportSilentAuth();

            try
            {
                if (AuthenticationRequestParameters.Account == null )
                {
                    _logger.Verbose("No account passed to AcquireTokenSilent. ");
                    throw new MsalUiRequiredException(
                       MsalError.UserNullError,
                       MsalErrorMessage.MsalUiRequiredMessage,
                       null,
                       UiRequiredExceptionClassification.AcquireTokenSilentFailed);
                }

                if (!PublicClientApplication.IsCurrentBrokerAccount(AuthenticationRequestParameters.Account))
                {
                    _logger.Verbose("Attempting to acquire token using using local cache...");
                    return await _clientStrategy.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                }

                if (!isBrokerConfigured) // IsCurrentBrokerAccount = true
                {
                    _logger.Verbose("No account passed to AcquireTokenSilent. ");
                    throw new MsalUiRequiredException(
                       MsalError.CurrentBrokerAccount,
                       "Only some brokers (WAM) can log in the current account. ",
                       null,
                       UiRequiredExceptionClassification.AcquireTokenSilentFailed);
                }

                _logger.Verbose("IsCurrentBrokerAccount - Only the Windows broker (WAM) may be able to log in user with a default account...");
                return await _brokerStrategyLazy.Value.ExecuteAsync(cancellationToken).ConfigureAwait(false);

            }
            catch (MsalException ex)
            {
                _logger.Verbose("Token cache could not satisfy silent request. ");

                if (isBrokerConfigured && ShouldTryWithBrokerError(ex.ErrorCode))
                {
                    _logger.Info("Attempting to use broker instead. ");
                    return await _brokerStrategyLazy.Value.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                }

                throw;
            }
        }

        private static bool ShouldTryWithBrokerError(string errorCode)
        {
            return
                string.Equals(errorCode, MsalError.InvalidGrantError, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(errorCode, MsalError.InteractionRequired, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(errorCode, MsalError.NoTokensFoundError, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(errorCode, MsalError.NoAccountForLoginHint, StringComparison.OrdinalIgnoreCase);
        }

        protected override void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            if (_silentParameters.LoginHint != null)
            {
                apiEvent.LoginHint = _silentParameters.LoginHint;
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

            AuthenticationRequestParameters.Authority = Authority.CreateAuthorityForRequest(
                ServiceBundle.Config.AuthorityInfo,
                AuthenticationRequestParameters.AuthorityOverride,
                account?.HomeAccountId?.TenantId);
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

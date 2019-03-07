// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal class ClientApplicationBaseExecutor : AbstractExecutor, IClientApplicationBaseExecutor
    {
        private readonly ClientApplicationBase _clientApplicationBase;

        public ClientApplicationBaseExecutor(IServiceBundle serviceBundle, ClientApplicationBase clientApplicationBase)
            : base(serviceBundle, clientApplicationBase)
        {
            _clientApplicationBase = clientApplicationBase;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenSilentParameters silentParameters,
            CancellationToken cancellationToken)
        {
            LogVersionInfo();

            IAccount account = GetAccountFromParamsOrLoginHint(silentParameters);

            var customAuthority = commonParameters.AuthorityOverride == null
                                      ? _clientApplicationBase.GetAuthority(account)
                                      : Instance.Authority.CreateAuthorityWithOverride(
                                          ServiceBundle, 
                                          commonParameters.AuthorityOverride);

            var requestParameters = _clientApplicationBase.CreateRequestParameters(commonParameters, _clientApplicationBase.UserTokenCacheInternal, customAuthority);
            requestParameters.Account = account;

            var handler = new SilentRequest(ServiceBundle, requestParameters, silentParameters);
            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByRefreshTokenParameters refreshTokenParameters,
            CancellationToken cancellationToken)
        {
            LogVersionInfo();

            var requestContext = CreateRequestContext();
            if (commonParameters.Scopes == null || !commonParameters.Scopes.Any())
            {
                commonParameters.Scopes = new SortedSet<string>
                {
                    _clientApplicationBase.ClientId + "/.default"
                };
                requestContext.Logger.Info(LogMessages.NoScopesProvidedForRefreshTokenRequest);
            }

            var requestParameters = _clientApplicationBase.CreateRequestParameters(commonParameters, _clientApplicationBase.UserTokenCacheInternal);
            requestParameters.IsRefreshTokenRequest = true;

            requestContext.Logger.Info(LogMessages.UsingXScopesForRefreshTokenRequest(commonParameters.Scopes.Count()));

            var handler = new ByRefreshTokenRequest(ServiceBundle, requestParameters, refreshTokenParameters);
            return await handler.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private IAccount GetSingleAccountForLoginHint(string loginHint)
        {
            var accounts = _clientApplicationBase.UserTokenCacheInternal.GetAccounts(_clientApplicationBase.Authority)
                .Where(
                    a => !string.IsNullOrWhiteSpace(a.Username) &&
                    a.Username.Equals(loginHint, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!accounts.Any())
            {
                throw new MsalUiRequiredException(
                    MsalUiRequiredException.NoAccountForLoginHint,
                    MsalErrorMessage.NoAccountForLoginHint);
            }

            if (accounts.Count() > 1)
            {
                throw new MsalUiRequiredException(
                    MsalUiRequiredException.MultipleAccountsForLoginHint,
                    MsalErrorMessage.MultipleAccountsForLoginHint);
            }

            return accounts.First();
        }


        private IAccount GetAccountFromParamsOrLoginHint(AcquireTokenSilentParameters silentParameters)
        {
            if (silentParameters.Account != null)
            {
                return silentParameters.Account;
            }

            return GetSingleAccountForLoginHint(silentParameters.LoginHint);
        }
    }
}

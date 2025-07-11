﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests.Silent;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    ///     Handles requests that are non-interactive. Currently MSAL supports Integrated Windows Auth (IWA).
    /// </summary>
    internal class IntegratedWindowsAuthRequest : RequestBase
    {
        private readonly CommonNonInteractiveHandler _commonNonInteractiveHandler;
        private readonly AcquireTokenByIntegratedWindowsAuthParameters _integratedWindowsAuthParameters;
        private readonly Lazy<ISilentAuthRequestStrategy> _brokerStrategyLazy;

        public IntegratedWindowsAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenByIntegratedWindowsAuthParameters integratedWindowsAuthParameters)
            : base(serviceBundle, authenticationRequestParameters, integratedWindowsAuthParameters)
        {
            _integratedWindowsAuthParameters = integratedWindowsAuthParameters;
            _commonNonInteractiveHandler = new CommonNonInteractiveHandler(
                authenticationRequestParameters.RequestContext,
                serviceBundle);

            var silentParameters = new AcquireTokenSilentParameters();
            var silentRequest = new SilentRequest(ServiceBundle, authenticationRequestParameters, silentParameters);
            _brokerStrategyLazy = new Lazy<ISilentAuthRequestStrategy>(() =>  new BrokerSilentStrategy(silentRequest,
                                                                                               serviceBundle,
                                                                                               authenticationRequestParameters,
                                                                                               silentParameters,
                                                                                               serviceBundle.PlatformProxy.CreateBroker(
                                                                                                   serviceBundle.Config, null)));
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            bool isBrokerConfigured = AuthenticationRequestParameters.AppConfig.IsBrokerEnabled &&
                                      ServiceBundle.PlatformProxy.CanBrokerSupportSilentAuth();

            if(isBrokerConfigured)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info("IWA called with broker. Routing to broker default user sign in");
                AuthenticationRequestParameters.Account = PublicClientApplication.OperatingSystemAccount;
                return await _brokerStrategyLazy.Value.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            await ResolveAuthorityAsync().ConfigureAwait(false);
            await UpdateUsernameAsync().ConfigureAwait(false);
            var userAssertion = await FetchAssertionFromWsTrustAsync().ConfigureAwait(false);
            var msalTokenResponse = await SendTokenRequestAsync(
                                                GetAdditionalBodyParameters(userAssertion), cancellationToken)
                                            .ConfigureAwait(false);

            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            return GetCcsUpnHeader(_integratedWindowsAuthParameters.Username);
        }

        private async Task<UserAssertion> FetchAssertionFromWsTrustAsync()
        {
            if (!AuthenticationRequestParameters.AuthorityInfo.IsWsTrustFlowSupported)
            {
                //IWA is currently not supported in pure adfs environments. See https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2771
                throw new MsalClientException(
                            MsalError.IntegratedWindowsAuthenticationFailed,
                            "Integrated windows authentication is not supported when using WithAdfsAuthority() to specify the authority in ADFS on premises environments"
                            + " See https://aka.ms/msal-net-iwa for more details.");
            }

            var userRealmResponse = await _commonNonInteractiveHandler
                                          .QueryUserRealmDataAsync(AuthenticationRequestParameters.AuthorityInfo.UserRealmUriPrefix, _integratedWindowsAuthParameters.Username)
                                          .ConfigureAwait(false);

            if (userRealmResponse.IsFederated)
            {
                var wsTrustResponse = await _commonNonInteractiveHandler.PerformWsTrustMexExchangeAsync(
                    userRealmResponse.FederationMetadataUrl,
                    userRealmResponse.CloudAudienceUrn,
                    UserAuthType.IntegratedAuth,
                    _integratedWindowsAuthParameters.Username,
                    null,
                    _integratedWindowsAuthParameters.FederationMetadata).ConfigureAwait(false);

                // We assume that if the response token type is not SAML 1.1, it is SAML 2
                return new UserAssertion(
                    wsTrustResponse.Token,
                    wsTrustResponse.TokenType == WsTrustResponse.Saml1Assertion
                        ? OAuth2GrantType.Saml11Bearer
                        : OAuth2GrantType.Saml20Bearer);
            }

            if (userRealmResponse.IsManaged)
            {
                throw new MsalClientException(
                    MsalError.IntegratedWindowsAuthNotSupportedForManagedUser,
                    MsalErrorMessage.IwaNotSupportedForManagedUser);
            }

            throw new MsalClientException(
                MsalError.UnknownUserType,
                string.Format(
                    CultureInfo.CurrentCulture,
                    MsalErrorMessage.UnsupportedUserType,
                    userRealmResponse.AccountType));
        }

        private async Task UpdateUsernameAsync()
        {
            if (string.IsNullOrWhiteSpace(_integratedWindowsAuthParameters.Username))
            {
                string platformUsername = await _commonNonInteractiveHandler.GetPlatformUserAsync().ConfigureAwait(false);
                _integratedWindowsAuthParameters.Username = platformUsername;
            }
        }

        private static Dictionary<string, string> GetAdditionalBodyParameters(UserAssertion userAssertion)
        {
            var dict = new Dictionary<string, string>();

            if (userAssertion != null)
            {
                dict[OAuth2Parameter.ClientInfo] = "1";
                dict[OAuth2Parameter.GrantType] = userAssertion.AssertionType;
                dict[OAuth2Parameter.Assertion] = Convert.ToBase64String(Encoding.UTF8.GetBytes(userAssertion.Assertion));
            }

            return dict;
        }
    }
}

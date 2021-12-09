// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    ///     Handles requests that are non-interactive. Currently MSAL supports Integrated Windows Auth.
    /// </summary>
    internal class UsernamePasswordRequest : RequestBase
    {
        private readonly CommonNonInteractiveHandler _commonNonInteractiveHandler;
        private readonly AcquireTokenByUsernamePasswordParameters _usernamePasswordParameters;

        public UsernamePasswordRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenByUsernamePasswordParameters usernamePasswordParameters)
            : base(serviceBundle, authenticationRequestParameters, usernamePasswordParameters)
        {
            _usernamePasswordParameters = usernamePasswordParameters;
            _commonNonInteractiveHandler = new CommonNonInteractiveHandler(
                authenticationRequestParameters.RequestContext,
                serviceBundle);
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);
            await UpdateUsernameAsync().ConfigureAwait(false);
            var userAssertion = await FetchAssertionFromWsTrustAsync().ConfigureAwait(false);
            var msalTokenResponse =
                await SendTokenRequestAsync(GetAdditionalBodyParameters(userAssertion), cancellationToken).ConfigureAwait(false);
            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }

        private async Task<UserAssertion> FetchAssertionFromWsTrustAsync()
        {
            if (AuthenticationRequestParameters.AuthorityInfo.AuthorityType == AuthorityType.Adfs ||
                AuthenticationRequestParameters.AuthorityInfo.AuthorityType == AuthorityType.B2C)
            {
                return null;
            }

            var userRealmResponse = await _commonNonInteractiveHandler
                                          .QueryUserRealmDataAsync(AuthenticationRequestParameters.AuthorityInfo.UserRealmUriPrefix, _usernamePasswordParameters.Username)
                                          .ConfigureAwait(false);

            if (userRealmResponse.IsFederated)
            {
                var wsTrustResponse = await _commonNonInteractiveHandler.PerformWsTrustMexExchangeAsync(
                                          userRealmResponse.FederationMetadataUrl,
                                          userRealmResponse.CloudAudienceUrn,
                                          UserAuthType.UsernamePassword,
                                          _usernamePasswordParameters.Username,
                                          _usernamePasswordParameters.Password,
                                          _usernamePasswordParameters.FederationMetadata).ConfigureAwait(false);

                // We assume that if the response token type is not SAML 1.1, it is SAML 2
                return new UserAssertion(
                    wsTrustResponse.Token,
                    wsTrustResponse.TokenType == WsTrustResponse.Saml1Assertion
                        ? OAuth2GrantType.Saml11Bearer
                        : OAuth2GrantType.Saml20Bearer);
            }

            if (userRealmResponse.IsManaged)
            {
                // handle grant flow
                if (_usernamePasswordParameters.Password == null)
                {
                    throw new MsalClientException(MsalError.PasswordRequiredForManagedUserError);
                }

                return null;
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
            if (string.IsNullOrWhiteSpace(_usernamePasswordParameters.Username))
            {
                string platformUsername = await _commonNonInteractiveHandler.GetPlatformUserAsync().ConfigureAwait(false);
                _usernamePasswordParameters.Username = platformUsername;
            }
        }

        private Dictionary<string, string> GetAdditionalBodyParameters(UserAssertion userAssertion)
        {
            var dict = new Dictionary<string, string>();

            if (userAssertion != null)
            {
                dict[OAuth2Parameter.GrantType] = userAssertion.AssertionType;
                dict[OAuth2Parameter.Assertion] = Convert.ToBase64String(Encoding.UTF8.GetBytes(userAssertion.Assertion));
            }

            // This is hit if the account is managed, as no userAssertion is created for a managed account
            else
            {
                dict[OAuth2Parameter.GrantType] = OAuth2GrantType.Password;
                dict[OAuth2Parameter.Username] = _usernamePasswordParameters.Username;
                dict[OAuth2Parameter.Password] = new string(_usernamePasswordParameters.Password.PasswordToCharArray());
            }

            ISet<string> unionScope = new HashSet<string>()
            {
                OAuth2Value.ScopeOpenId,
                OAuth2Value.ScopeOfflineAccess,
                OAuth2Value.ScopeProfile
            };

            unionScope.UnionWith(AuthenticationRequestParameters.Scope);
            dict[OAuth2Parameter.Scope] = unionScope.AsSingleString();
            dict[OAuth2Parameter.ClientInfo] = "1";

            return dict;
        }

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            return GetCcsUpnHeader(_usernamePasswordParameters.Username);
        }
    }
}

// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.Telemetry;
using Microsoft.Identity.Core.WsTrust;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    ///     Handles requests that are non-interactive. Currently MSAL supports Integrated Windows Auth.
    /// </summary>
    internal class UsernamePasswordRequest : RequestBase
    {
        private readonly CommonNonInteractiveHandler _commonNonInteractiveHandler;
        private readonly UsernamePasswordInput _usernamePasswordInput;

        public UsernamePasswordRequest(
            IHttpManager httpManager,
            ICryptographyManager cryptographyManager,
            IWsTrustWebRequestManager wsTrustWebRequestManager,
            AuthenticationRequestParameters authenticationRequestParameters,
            ApiEvent.ApiIds apiId,
            UsernamePasswordInput usernamePasswordInput)
            : base(httpManager, cryptographyManager, authenticationRequestParameters, apiId)
        {
            _usernamePasswordInput = usernamePasswordInput ?? throw new ArgumentNullException(nameof(usernamePasswordInput));
            _commonNonInteractiveHandler = new CommonNonInteractiveHandler(
                authenticationRequestParameters.RequestContext,
                usernamePasswordInput,
                wsTrustWebRequestManager);
        }

        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);
            await UpdateUsernameAsync().ConfigureAwait(false);
            var userAssertion = await FetchAssertionFromWsTrustAsync().ConfigureAwait(false);
            var msalTokenResponse =
                await SendTokenRequestAsync(GetAdditionalBodyParameters(userAssertion), cancellationToken).ConfigureAwait(false);
            return CacheTokenResponseAndCreateAuthenticationResult(msalTokenResponse);
        }

        private async Task<UserAssertion> FetchAssertionFromWsTrustAsync()
        {
            if (AuthenticationRequestParameters.Authority.AuthorityType == Core.Instance.AuthorityType.Adfs)
            {
                return null;
            }

            var userRealmResponse = await _commonNonInteractiveHandler
                                          .QueryUserRealmDataAsync(AuthenticationRequestParameters.Authority.UserRealmUriPrefix)
                                          .ConfigureAwait(false);

            if (userRealmResponse.IsFederated)
            {
                var wsTrustResponse = await _commonNonInteractiveHandler.PerformWsTrustMexExchangeAsync(
                                          userRealmResponse.FederationMetadataUrl,
                                          userRealmResponse.CloudAudienceUrn,
                                          UserAuthType.UsernamePassword).ConfigureAwait(false);

                // We assume that if the response token type is not SAML 1.1, it is SAML 2
                return new UserAssertion(
                    wsTrustResponse.Token,
                    wsTrustResponse.TokenType == WsTrustResponse.Saml1Assertion
                        ? OAuth2GrantType.Saml11Bearer
                        : OAuth2GrantType.Saml20Bearer);
            }
            else if (userRealmResponse.IsManaged)
            {
                // handle grant flow
                if (!_usernamePasswordInput.HasPassword())
                {
                    throw new MsalClientException(MsalError.PasswordRequiredForManagedUserError);
                }

                return null;
            }
            else
            {
                throw new MsalClientException(
                    MsalError.UnknownUserType,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        MsalErrorMessage.UnsupportedUserType,
                        userRealmResponse.AccountType));
            }
        }

        private async Task UpdateUsernameAsync()
        {
            if (_usernamePasswordInput != null)
            {
                if (string.IsNullOrWhiteSpace(_usernamePasswordInput.UserName))
                {
                    string platformUsername = await _commonNonInteractiveHandler.GetPlatformUserAsync().ConfigureAwait(false);
                    _usernamePasswordInput.UserName = platformUsername;
                }
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
                dict[OAuth2Parameter.Username] = _usernamePasswordInput.UserName;
                dict[OAuth2Parameter.Password] = new string(_usernamePasswordInput.PasswordToCharArray());
            }

            ISet<string> unionScope = new HashSet<string>()
            {
                OAuth2Value.ScopeOpenId,
                OAuth2Value.ScopeOfflineAccess,
                OAuth2Value.ScopeProfile
            };

            unionScope.UnionWith(AuthenticationRequestParameters.Scope);
            dict[OAuth2Parameter.Scope] = unionScope.AsSingleString();

            return dict;
        }
    }
}
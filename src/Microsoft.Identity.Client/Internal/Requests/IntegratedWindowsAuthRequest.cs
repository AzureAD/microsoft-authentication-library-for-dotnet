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
using Microsoft.Identity.Client.Config;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    ///     Handles requests that are non-interactive. Currently MSAL supports Integrated Windows Auth (IWA).
    /// </summary>
    internal class IntegratedWindowsAuthRequest : RequestBase
    {
        private readonly CommonNonInteractiveHandler _commonNonInteractiveHandler;
        private readonly IntegratedWindowsAuthInput _iwaInput;

        public IntegratedWindowsAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            ApiEvent.ApiIds apiId,
            IntegratedWindowsAuthInput iwaInput)
            : base(serviceBundle, authenticationRequestParameters, apiId)
        {
            _iwaInput = iwaInput ?? throw new ArgumentNullException(nameof(iwaInput));
            _commonNonInteractiveHandler = new CommonNonInteractiveHandler(
                authenticationRequestParameters.RequestContext,
                _iwaInput,
                serviceBundle);
        }

        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);
            await UpdateUsernameAsync().ConfigureAwait(false);
            var userAssertion = await FetchAssertionFromWsTrustAsync().ConfigureAwait(false);
            var msalTokenResponse = await SendTokenRequestAsync(
                                            GetAdditionalBodyParameters(userAssertion), cancellationToken)
                                        .ConfigureAwait(false);
            return CacheTokenResponseAndCreateAuthenticationResult(msalTokenResponse);
        }

        private async Task<UserAssertion> FetchAssertionFromWsTrustAsync()
        {
            if (AuthenticationRequestParameters.Authority.AuthorityInfo.AuthorityType == AuthorityType.Adfs)
            {
                return null;
            }

            var userRealmResponse = await _commonNonInteractiveHandler
                                          .QueryUserRealmDataAsync(AuthenticationRequestParameters.Authority.AuthorityInfo.UserRealmUriPrefix)
                                          .ConfigureAwait(false);

            if (userRealmResponse.IsFederated)
            {
                var wsTrustResponse = await _commonNonInteractiveHandler.PerformWsTrustMexExchangeAsync(
                    userRealmResponse.FederationMetadataUrl,
                    userRealmResponse.CloudAudienceUrn,
                    UserAuthType.IntegratedAuth).ConfigureAwait(false);

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
            if (string.IsNullOrWhiteSpace(_iwaInput.UserName))
            {
                string platformUsername = await _commonNonInteractiveHandler.GetPlatformUserAsync().ConfigureAwait(false);
                _iwaInput.UserName = platformUsername;
            }
        }

        private static Dictionary<string, string> GetAdditionalBodyParameters(UserAssertion userAssertion)
        {
            var dict = new Dictionary<string, string>();

            if (userAssertion != null)
            {
                dict[OAuth2Parameter.GrantType] = userAssertion.AssertionType;
                dict[OAuth2Parameter.Assertion] = Convert.ToBase64String(Encoding.UTF8.GetBytes(userAssertion.Assertion));
            }

            return dict;
        }
    }
}
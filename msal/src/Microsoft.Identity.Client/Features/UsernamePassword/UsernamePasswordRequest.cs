//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

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
using Microsoft.Identity.Core.WsTrust;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    /// Handles requests that are non-interactive. Currently MSAL supports Integrated Windows Auth.
    /// </summary>
    internal class UsernamePasswordRequest : RequestBase
    {
        private readonly UsernamePasswordInput _usernamePasswordInput;
        private UserAssertion _userAssertion;

        private readonly CommonNonInteractiveHandler _commonNonInteractiveHandler;

        public UsernamePasswordRequest(
            IHttpManager httpManager, 
            ICryptographyManager cryptographyManager,
            IWsTrustWebRequestManager wsTrustWebRequestManager, 
            AuthenticationRequestParameters authenticationRequestParameters, 
            UsernamePasswordInput usernamePasswordInput)
       : base(httpManager, cryptographyManager, authenticationRequestParameters)
        {
            this._usernamePasswordInput = usernamePasswordInput ?? throw new ArgumentNullException(nameof(usernamePasswordInput));
            this._commonNonInteractiveHandler = new CommonNonInteractiveHandler(
                authenticationRequestParameters.RequestContext, usernamePasswordInput, wsTrustWebRequestManager);
        }

        protected override async Task SendTokenRequestAsync(CancellationToken cancellationToken)
        {
            await UpdateUsernameAsync().ConfigureAwait(false);
            await FetchAssertionFromWsTrustAsync().ConfigureAwait(false);
            await base.SendTokenRequestAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task FetchAssertionFromWsTrustAsync()
        {
            if (AuthenticationRequestParameters.Authority.AuthorityType != Core.Instance.AuthorityType.Adfs)
            {
                var userRealmResponse = await this._commonNonInteractiveHandler
                   .QueryUserRealmDataAsync(this.AuthenticationRequestParameters.Authority.UserRealmUriPrefix)
                   .ConfigureAwait(false);

                if (string.Equals(userRealmResponse.AccountType, "federated", StringComparison.OrdinalIgnoreCase))
                {
                    WsTrustResponse wsTrustResponse = await _commonNonInteractiveHandler.PerformWsTrustMexExchangeAsync(
                        userRealmResponse.FederationMetadataUrl,
                        userRealmResponse.CloudAudienceUrn,
                        UserAuthType.UsernamePassword).ConfigureAwait(false);

                    // We assume that if the response token type is not SAML 1.1, it is SAML 2
                    _userAssertion = new UserAssertion(
                        wsTrustResponse.Token,
                        (wsTrustResponse.TokenType == WsTrustResponse.Saml1Assertion) ? OAuth2GrantType.Saml11Bearer : OAuth2GrantType.Saml20Bearer);
                }
                else if (string.Equals(userRealmResponse.AccountType, "managed", StringComparison.OrdinalIgnoreCase))
                {
                    // handle grant flow
                    if (!this._usernamePasswordInput.HasPassword())
                    {
                        throw new MsalClientException(MsalError.PasswordRequiredForManagedUserError);
                    }
                }
                else
                {
                    throw new MsalClientException(MsalError.UnknownUserType,
                        string.Format(CultureInfo.CurrentCulture, 
                        MsalErrorMessage.UnsupportedUserType, 
                        userRealmResponse.AccountType,
                        this._usernamePasswordInput.UserName));
                }
            }
        }

        private async Task UpdateUsernameAsync()
        {
            if (_usernamePasswordInput != null)
            {
                if (string.IsNullOrWhiteSpace(_usernamePasswordInput.UserName))
                {
                    string platformUsername = await this._commonNonInteractiveHandler.GetPlatformUserAsync().ConfigureAwait(false);
                    this._usernamePasswordInput.UserName = platformUsername;
                }
            }
        }

        protected override void SetAdditionalRequestParameters(OAuth2Client client)
        {
            if (this._userAssertion != null)
            {
                client.AddBodyParameter(OAuth2Parameter.GrantType, this._userAssertion.AssertionType);
                client.AddBodyParameter(OAuth2Parameter.Assertion, Convert.ToBase64String(Encoding.UTF8.GetBytes(this._userAssertion.Assertion)));
            }
            // This is hit if the account is managed, as no userAssertion is created for a managed account
            else
            {
                client.AddBodyParameter(OAuth2Parameter.GrantType, OAuth2GrantType.Password);
                client.AddBodyParameter(OAuth2Parameter.Username, this._usernamePasswordInput.UserName);
                client.AddBodyParameter(OAuth2Parameter.Password, new string(this._usernamePasswordInput.PasswordToCharArray()));
            }

            ISet<string> unionScope =
                new HashSet<string>() { OAuth2Value.ScopeOpenId, OAuth2Value.ScopeOfflineAccess, OAuth2Value.ScopeProfile };

            unionScope.UnionWith(this.AuthenticationRequestParameters.Scope);
            client.AddBodyParameter(OAuth2Parameter.Scope, unionScope.AsSingleString());
        }
    }
}
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
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Instance;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Microsoft.Identity.Core.WsTrust;
using Microsoft.Identity.Core;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Flows
{
    internal class AcquireTokenUsernamePasswordHandler : AcquireTokenHandlerBase
    {
        private readonly UsernamePasswordInput _userPasswordInput;
        private UserAssertion _userAssertion;
        private readonly CommonNonInteractiveHandler _commonNonInteractiveHandler;

        public AcquireTokenUsernamePasswordHandler(IWsTrustWebRequestManager wsTrustWebRequestManager, RequestData requestData, UsernamePasswordInput userPasswordInput)
            : base(requestData)
        {
            // We enable ADFS support only when it makes sense to do so
            if (requestData.Authenticator.AuthorityType == AuthorityType.ADFS)
            {
                SupportADFS = true;
            }

            _userPasswordInput = userPasswordInput ?? throw new ArgumentNullException(nameof(userPasswordInput));
            _commonNonInteractiveHandler = new CommonNonInteractiveHandler(RequestContext, _userPasswordInput, wsTrustWebRequestManager);
            DisplayableId = userPasswordInput.UserName;
        }

        protected override async Task PreRunAsync()
        {
            await base.PreRunAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(_userPasswordInput.UserName))
            {
                string platformUsername = await _commonNonInteractiveHandler.GetPlatformUserAsync()
                    .ConfigureAwait(false);

                _userPasswordInput.UserName = platformUsername;
            }
        }

        protected override async Task PreTokenRequestAsync()
        {
            await base.PreTokenRequestAsync().ConfigureAwait(false);

            if (!SupportADFS)
            {
                var userRealmResponse = await _commonNonInteractiveHandler.QueryUserRealmDataAsync(Authenticator.UserRealmUriPrefix)
                    .ConfigureAwait(false);

                if (string.Equals(userRealmResponse.AccountType, "federated", StringComparison.OrdinalIgnoreCase))
                {
                    WsTrustResponse wsTrustResponse = await _commonNonInteractiveHandler.PerformWsTrustMexExchangeAsync(
                        userRealmResponse.FederationMetadataUrl,
                        userRealmResponse.CloudAudienceUrn,
                        UserAuthType.UsernamePassword).ConfigureAwait(false);

                    // We assume that if the response token type is not SAML 1.1, it is SAML 2
                    _userAssertion = new UserAssertion(wsTrustResponse.Token, (wsTrustResponse.TokenType == WsTrustResponse.Saml1Assertion) ? OAuthGrantType.Saml11Bearer : OAuthGrantType.Saml20Bearer);
                }
                else if (string.Equals(userRealmResponse.AccountType, "managed", StringComparison.OrdinalIgnoreCase))
                {
                    // handle password grant flow for the managed user
                    if (!_userPasswordInput.HasPassword())
                    {
                        throw new AdalException(AdalError.PasswordRequiredForManagedUserError);
                    }
                }
                else
                {
                    throw new AdalException(AdalError.UnknownUserType);
                }
            }
        }

        protected override void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters)
        {
            if (_userAssertion != null)
            {
                requestParameters[OAuthParameter.GrantType] = _userAssertion.AssertionType;
                requestParameters[OAuthParameter.Assertion] = Convert.ToBase64String(Encoding.UTF8.GetBytes(_userAssertion.Assertion));
            }
            else
            {
                // TODO: test if this is hit
                requestParameters[OAuthParameter.GrantType] = OAuthGrantType.Password;
                requestParameters[OAuthParameter.Username] = _userPasswordInput.UserName;
                requestParameters[OAuthParameter.Password] = new string(_userPasswordInput.PasswordToCharArray());
            }

            // To request id_token in response
            requestParameters[OAuthParameter.Scope] = OAuthValue.ScopeOpenId;
        }

        private bool PerformUserRealmDiscovery()
        {
            // To decide whether user realm discovery is needed or not
            // we should also consider if that is supported by the authority
            return _userAssertion == null && !SupportADFS;
        }
    }
}

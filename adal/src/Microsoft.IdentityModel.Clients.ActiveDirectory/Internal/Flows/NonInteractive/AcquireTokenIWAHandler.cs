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
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.WsTrust;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Instance;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Flows
{
    /// <summary>
    /// Handler for Integrated Windows Authentication
    /// </summary>
    internal class AcquireTokenIWAHandler : AcquireTokenHandlerBase
    {
        private readonly IntegratedWindowsAuthInput _iwaInput;
        private UserAssertion _userAssertion;
        private readonly CommonNonInteractiveHandler _commonNonInteractiveHandler;

        public AcquireTokenIWAHandler(IServiceBundle serviceBundle, RequestData requestData, IntegratedWindowsAuthInput iwaInput)
            : base(requestData)
        {
            if (iwaInput == null)
            {
                throw new ArgumentNullException(nameof(iwaInput));
            }

            // We enable ADFS support only when it makes sense to do so
            if (requestData.Authenticator.AuthorityType == AuthorityType.ADFS)
            {
                SupportADFS = true;
            }

            _iwaInput = iwaInput;
            DisplayableId = iwaInput.UserName;

            _commonNonInteractiveHandler = new CommonNonInteractiveHandler(RequestContext, _iwaInput, serviceBundle);
        }

        protected override async Task PreRunAsync()
        {
            await base.PreRunAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(_iwaInput.UserName))
            {
                string platformUsername = await _commonNonInteractiveHandler.GetPlatformUserAsync()
                    .ConfigureAwait(false);
                _iwaInput.UserName = platformUsername;
            }

            DisplayableId = _iwaInput.UserName;
        }

        protected internal /* internal for test only */ override async Task PreTokenRequestAsync()
        {
            await base.PreTokenRequestAsync().ConfigureAwait(false);

            if (!SupportADFS)
            {
                var userRealmResponse = await _commonNonInteractiveHandler.QueryUserRealmDataAsync(Authenticator.UserRealmUriPrefix)
                   .ConfigureAwait(false);

                if (userRealmResponse.IsFederated)
                {
                    WsTrustResponse wsTrustResponse = await _commonNonInteractiveHandler.PerformWsTrustMexExchangeAsync(
                        userRealmResponse.FederationMetadataUrl,
                        userRealmResponse.CloudAudienceUrn,
                        UserAuthType.IntegratedAuth).ConfigureAwait(false);

                    // We assume that if the response token type is not SAML 1.1, it is SAML 2
                    _userAssertion = new UserAssertion(wsTrustResponse.Token, (wsTrustResponse.TokenType == WsTrustResponse.Saml1Assertion) ? OAuthGrantType.Saml11Bearer : OAuthGrantType.Saml20Bearer);
                }
                else if (userRealmResponse.IsManaged)
                {
                    throw new AdalException(
                        AdalError.IntegratedWindowsAuthNotSupportedForManagedUser, 
                        AdalErrorMessage.IwaNotSupportedForManagedUser);
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
                requestParameters[OAuthParameter.Assertion] =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(_userAssertion.Assertion));
            }

            // To request id_token in response
            requestParameters[OAuthParameter.Scope] = OAuthValue.ScopeOpenId;
        }
    }
}

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
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AcquireTokenNonInteractiveHandler : AcquireTokenHandlerBase
    {
        private readonly UserCredential userCredential;

        private UserAssertion userAssertion;

        public AcquireTokenNonInteractiveHandler(RequestData requestData, UserCredential userCredential)
            : base(requestData)
        {
            if (userCredential == null)
            {
                throw new ArgumentNullException("userCredential");
            }

            // We enable ADFS support only when it makes sense to do so
            if (requestData.Authenticator.AuthorityType == AuthorityType.ADFS)
            {
                this.SupportADFS = true;
            }

            this.userCredential = userCredential;
            this.DisplayableId = this.userCredential.UserName;
        }

        public AcquireTokenNonInteractiveHandler(RequestData requestData, UserAssertion userAssertion)
            : base(requestData)
        {
            if (userAssertion == null)
            {
                throw new ArgumentNullException("userAssertion");
            }

            if (string.IsNullOrWhiteSpace(userAssertion.AssertionType))
            {
                throw new ArgumentException(AdalErrorMessage.UserCredentialAssertionTypeEmpty, "userAssertion");
            }
            this.userAssertion = userAssertion;
        }

        protected override async Task PreRunAsync()
        {
            await base.PreRunAsync().ConfigureAwait(false);

            if (this.userCredential != null)
            {
                if (string.IsNullOrWhiteSpace(this.userCredential.UserName))
                {
                    this.userCredential.UserName = await PlatformPlugin.PlatformInformation.GetUserPrincipalNameAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(userCredential.UserName))
                    {
                        PlatformPlugin.Logger.Information(this.CallState, "Could not find UPN for logged in user");
                        throw new AdalException(AdalError.UnknownUser);
                    }

                    PlatformPlugin.Logger.Verbose(this.CallState, string.Format(CultureInfo.CurrentCulture, " Logged in user with hash '{0}' detected", PlatformPlugin.CryptographyHelper.CreateSha256Hash(userCredential.UserName)));
                }

                this.DisplayableId = userCredential.UserName;
            }
            else if (this.userAssertion != null)
            {
                this.DisplayableId = userAssertion.UserName;
            }
        }

        protected override async Task PreTokenRequest()
        {
            await base.PreTokenRequest().ConfigureAwait(false);
            if (this.PerformUserRealmDiscovery())
            {
                UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(this.Authenticator.UserRealmUri, this.userCredential.UserName, this.CallState).ConfigureAwait(false);
                PlatformPlugin.Logger.Information(this.CallState, string.Format(CultureInfo.CurrentCulture, " User with hash '{0}' detected as '{1}'", PlatformPlugin.CryptographyHelper.CreateSha256Hash(this.userCredential.UserName), userRealmResponse.AccountType));

                if (string.Compare(userRealmResponse.AccountType, "federated", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (string.IsNullOrWhiteSpace(userRealmResponse.FederationMetadataUrl))
                    {
                        throw new AdalException(AdalError.MissingFederationMetadataUrl);
                    }
                    
                    WsTrustAddress wsTrustAddress = await MexParser.FetchWsTrustAddressFromMexAsync(userRealmResponse.FederationMetadataUrl, this.userCredential.UserAuthType, this.CallState).ConfigureAwait(false);
                    PlatformPlugin.Logger.Information(this.CallState, string.Format(CultureInfo.CurrentCulture, " WS-Trust endpoint '{0}' fetched from MEX at '{1}'", wsTrustAddress.Uri, userRealmResponse.FederationMetadataUrl));

                    WsTrustResponse wsTrustResponse = await WsTrustRequest.SendRequestAsync(wsTrustAddress, this.userCredential, this.CallState, userRealmResponse.CloudAudienceUrn).ConfigureAwait(false);
                    PlatformPlugin.Logger.Information(this.CallState, string.Format(CultureInfo.CurrentCulture, " Token of type '{0}' acquired from WS-Trust endpoint", wsTrustResponse.TokenType));

                    // We assume that if the response token type is not SAML 1.1, it is SAML 2
                    this.userAssertion = new UserAssertion(wsTrustResponse.Token, (wsTrustResponse.TokenType == WsTrustResponse.Saml1Assertion) ? OAuthGrantType.Saml11Bearer : OAuthGrantType.Saml20Bearer);
                }
                else if (string.Compare(userRealmResponse.AccountType, "managed", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // handle password grant flow for the managed user
                    if (this.userCredential.PasswordToCharArray() == null)
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
            if (this.userAssertion != null)
            {
                requestParameters[OAuthParameter.GrantType] = this.userAssertion.AssertionType;
                requestParameters[OAuthParameter.Assertion] = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.userAssertion.Assertion));
            }
            else
            {
                this.userCredential.ApplyTo(requestParameters);
            }

            // To request id_token in response
            requestParameters[OAuthParameter.Scope] = OAuthValue.ScopeOpenId;
        }

        private bool PerformUserRealmDiscovery()
        {
            // To decide whether user realm discovery is needed or not
            // we should also consider if that is supported by the authority
            return this.userAssertion == null &&
                   this.SupportADFS == false;
        }
    }
}

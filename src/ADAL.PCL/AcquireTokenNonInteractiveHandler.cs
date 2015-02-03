//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AcquireTokenNonInteractiveHandler : AcquireTokenHandlerBase
    {
        private readonly UserCredential userCredential;

        private UserAssertion userAssertion;
        
        public AcquireTokenNonInteractiveHandler(Authenticator authenticator, TokenCache tokenCache, string resource, string clientId, UserCredential userCredential)
            : base(authenticator, tokenCache, resource, new ClientKey(clientId), TokenSubjectType.User)
        {
            if (userCredential == null)
            {
                throw new ArgumentNullException("userCredential");
            }

            this.userCredential = userCredential;
        }

        public AcquireTokenNonInteractiveHandler(Authenticator authenticator, TokenCache tokenCache, string resource, string clientId, UserAssertion userAssertion)
            : base(authenticator, tokenCache, resource, new ClientKey(clientId), TokenSubjectType.User)
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
            await base.PreRunAsync();

            if (this.userCredential != null)
            {
                if (string.IsNullOrWhiteSpace(this.userCredential.UserName))
                {
                    this.userCredential.UserName = await PlatformPlugin.PlatformInformation.GetUserPrincipalNameAsync();
                    if (string.IsNullOrWhiteSpace(userCredential.UserName))
                    {
                        PlatformPlugin.Logger.Information(this.CallState, "Could not find UPN for logged in user");
                        throw new AdalException(AdalError.UnknownUser);
                    }

                    PlatformPlugin.Logger.Verbose(this.CallState, string.Format("Logged in user with hash '{0}' detected", PlatformPlugin.CryptographyHelper.CreateSha256Hash(userCredential.UserName)));
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
            await base.PreTokenRequest();
            if (this.userAssertion == null)
            {
                UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(this.Authenticator.UserRealmUri, this.userCredential.UserName, this.CallState);
                PlatformPlugin.Logger.Information(this.CallState, string.Format("User with hash '{0}' detected as '{1}'", PlatformPlugin.CryptographyHelper.CreateSha256Hash(this.userCredential.UserName), userRealmResponse.AccountType));

                if (string.Compare(userRealmResponse.AccountType, "federated", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (string.IsNullOrWhiteSpace(userRealmResponse.FederationMetadataUrl))
                    {
                        throw new AdalException(AdalError.MissingFederationMetadataUrl);
                    }

                    Uri wsTrustUrl = await MexParser.FetchWsTrustAddressFromMexAsync(userRealmResponse.FederationMetadataUrl, this.userCredential.UserAuthType, this.CallState);
                    PlatformPlugin.Logger.Information(this.CallState, string.Format("WS-Trust endpoint '{0}' fetched from MEX at '{1}'", wsTrustUrl, userRealmResponse.FederationMetadataUrl));

                    WsTrustResponse wsTrustResponse = await WsTrustRequest.SendRequestAsync(wsTrustUrl, this.userCredential, this.CallState);
                    PlatformPlugin.Logger.Information(this.CallState, string.Format("Token of type '{0}' acquired from WS-Trust endpoint", wsTrustResponse.TokenType));

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
                requestParameters[OAuthParameter.GrantType] = OAuthGrantType.Password;
                requestParameters[OAuthParameter.Username] = this.userCredential.UserName;
                requestParameters[OAuthParameter.Password] = this.userCredential.Password;
            }

            // To request id_token in response
            requestParameters[OAuthParameter.Scope] = OAuthValue.ScopeOpenId;
        }
    }
}

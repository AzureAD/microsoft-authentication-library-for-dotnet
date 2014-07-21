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

        private UserAssertion samlAssertion;
        
        public AcquireTokenNonInteractiveHandler(Authenticator authenticator, TokenCache tokenCache, string resource, string clientId, UserCredential userCredential, bool callSync)
            : base(authenticator, tokenCache, resource, new ClientKey(clientId), TokenSubjectType.User, callSync)
        {
            if (userCredential == null)
            {
                throw new ArgumentNullException("userCredential");
            }

            this.userCredential = userCredential;
        }

        protected override async Task PreRunAsync()
        {
            await base.PreRunAsync();

            // We cannot move the following lines to UserCredential as one of these calls in async. 
            // It cannot be moved to constructor or property or a pure sync or async call. This is why we moved it here which is an async call already.
            if (string.IsNullOrWhiteSpace(this.userCredential.UserName))
            {
#if ADAL_NET
                this.userCredential.UserName = PlatformSpecificHelper.GetUserPrincipalName();
#else
                this.userCredential.UserName = await PlatformSpecificHelper.GetUserPrincipalNameAsync();
#endif
                if (string.IsNullOrWhiteSpace(userCredential.UserName))
                {
                    Logger.Information(this.CallState, "Could not find UPN for logged in user");
                    throw new AdalException(AdalError.UnknownUser);
                }

                Logger.Information(this.CallState, "Logged in user '{0}' detected", userCredential.UserName);
            }

            this.DisplayableId = userCredential.UserName;
        }

        protected override async Task PreTokenRequest()
        {
            await base.PreTokenRequest();
            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(this.Authenticator.UserRealmUri, this.userCredential.UserName, this.CallState);
            Logger.Information(this.CallState, "User '{0}' detected as '{1}'", this.userCredential.UserName, userRealmResponse.AccountType);

            if (string.Compare(userRealmResponse.AccountType, "federated", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (string.IsNullOrWhiteSpace(userRealmResponse.FederationMetadataUrl))
                {
                    throw new AdalException(AdalError.MissingFederationMetadataUrl);
                }

                Uri wsTrustUrl = await MexParser.FetchWsTrustAddressFromMexAsync(userRealmResponse.FederationMetadataUrl, this.userCredential.UserAuthType, this.CallState);
                Logger.Information(this.CallState, "WS-Trust endpoint '{0}' fetched from MEX at '{1}'", wsTrustUrl, userRealmResponse.FederationMetadataUrl);

                WsTrustResponse wsTrustResponse = await WsTrustRequest.SendRequestAsync(wsTrustUrl, this.userCredential, this.CallState);
                Logger.Information(this.CallState, "Token of type '{0}' acquired from WS-Trust endpoint", wsTrustResponse.TokenType);

                // We assume that if the response token type is not SAML 1.1, it is SAML 2
                this.samlAssertion = new UserAssertion(wsTrustResponse.Token, (wsTrustResponse.TokenType == WsTrustResponse.Saml1Assertion) ? OAuthGrantType.Saml11Bearer : OAuthGrantType.Saml20Bearer);
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

        protected override void AddAditionalRequestParameters(RequestParameters requestParameters)
        {
            if (this.samlAssertion != null)
            {
                requestParameters[OAuthParameter.GrantType] = samlAssertion.AssertionType;
                requestParameters[OAuthParameter.Assertion] = Convert.ToBase64String(Encoding.UTF8.GetBytes(samlAssertion.Assertion));
            }
            else
            {
                requestParameters[OAuthParameter.GrantType] = OAuthGrantType.Password;
                requestParameters[OAuthParameter.Username] = this.userCredential.UserName;
#if ADAL_NET
                if (this.userCredential.SecurePassword != null)
                {
                    requestParameters.AddSecureParameter(OAuthParameter.Password, this.userCredential.SecurePassword);
                }
                else
                {
                    requestParameters[OAuthParameter.Password] = this.userCredential.Password;
                }
#endif
            }

            // To request id_token in response
            requestParameters[OAuthParameter.Scope] = OAuthValue.ScopeOpenId;
        }
    }
}

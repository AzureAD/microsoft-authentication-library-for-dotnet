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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AcquireTokenNonInteractiveHandler : AcquireTokenHandlerBase
    {
        private UserCredential userCredential;
        
        public AcquireTokenNonInteractiveHandler(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, UserCredential userCredential, bool callSync)
            : base(authenticator, tokenCache, resource, clientKey, TokenSubjectType.User, callSync)
        {
            if (userCredential == null)
            {
                throw new ArgumentNullException("userCredential");
            }

            this.userCredential = userCredential;
        }

#if ADAL_WINRT
        protected override async Task SetUserIdentifiersAsync()
#else
        protected override void SetUserIdentifiers()
#endif
        {
            // We cannot move the following lines to UserCredential as one of these calls in async. 
            // It cannot be moved to constructor or property or a pure sync or async call. This is why we moved it here which is an async call already.
            if (string.IsNullOrWhiteSpace(userCredential.UserName))
            {
#if ADAL_WINRT
                this.userCredential.UserName = await PlatformSpecificHelper.GetUserPrincipalNameAsync();
#else
                this.userCredential.UserName = PlatformSpecificHelper.GetUserPrincipalName();
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

        protected override async Task<AuthenticationResult> SendTokenRequestAsync()
        {
            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(this.Authenticator.UserRealmUri, this.userCredential.UserName, this.CallState);
            Logger.Information(this.CallState, "User '{0}' detected as '{1}'", this.userCredential.UserName, userRealmResponse.AccountType);

            AuthenticationResult result;
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
                var samlCredential = new UserAssertion(
                    wsTrustResponse.Token,
                    (wsTrustResponse.TokenType == WsTrustResponse.Saml1Assertion) ? OAuthGrantType.Saml11Bearer : OAuthGrantType.Saml20Bearer);

                result = await OAuth2Request.SendTokenRequestWithUserAssertionAsync(this.Authenticator.TokenUri, this.Resource, this.ClientKey.ClientId, samlCredential, this.CallState);
            }
            else if (string.Compare(userRealmResponse.AccountType, "managed", StringComparison.OrdinalIgnoreCase) == 0)
            {
                //handle password grant flow for the managed user
                if (this.userCredential.PasswordToCharArray() == null)
                {
                    throw new AdalException(AdalError.PasswordRequiredForManagedUserError);
                }

                result = await OAuth2Request.SendTokenRequestWithUserCredentialAsync(this.Authenticator.TokenUri, this.Resource, this.ClientKey.ClientId, this.userCredential, this.CallState);
            }
            else
            {
                throw new AdalException(AdalError.UnknownUserType);
            }

            return result;
        }
    }
}

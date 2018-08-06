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
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.WsTrust;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class NonInteractiveRequest : RequestBase
    {
        protected UserCredential UserCredential;
        public UserAssertion UserAssertion;

        public NonInteractiveRequest(AuthenticationRequestParameters authenticationRequestParameters, UserCredential userCredential)
            : base(authenticationRequestParameters)
        {
            UserCredential = userCredential;
        }

        protected override async Task SendTokenRequestAsync()
        {
            if (UserCredential != null)
            {
                if (string.IsNullOrWhiteSpace(UserCredential.UserName))
                {
                    UserCredential.UserName = await PlatformPlugin.PlatformInformation.GetUserPrincipalNameAsync().ConfigureAwait(false);
                    string msg;
                    if (string.IsNullOrWhiteSpace(UserCredential.UserName))
                    {
                        msg = "Could not find UPN for logged in user";
                        AuthenticationRequestParameters.RequestContext.Logger.Info(msg);
                        AuthenticationRequestParameters.RequestContext.Logger.InfoPii(msg);

                        throw new MsalException(MsalError.UnknownUser);
                    }

                    msg = "Logged in user detected";
                    AuthenticationRequestParameters.RequestContext.Logger.Verbose(msg);

                    var piiMsg = msg + string.Format(CultureInfo.CurrentCulture, " with user name '{0}'",
                                     UserCredential.UserName);
                    AuthenticationRequestParameters.RequestContext.Logger.VerbosePii(piiMsg);
                }
            }
            if (AuthenticationRequestParameters.Authority.AuthorityType != Core.Instance.AuthorityType.Adfs)
            {
                var userRealmResponse = await Core.Realm.UserRealmDiscoveryResponse.CreateByDiscoveryAsync(
                    string.Format(CultureInfo.InvariantCulture, "https://{0}/common/userrealm/", AuthenticationRequestParameters.Authority.Host),
                    this.UserCredential.UserName, AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);
                if (userRealmResponse == null)
                {
                    throw new MsalException(MsalError.UserRealmDiscoveryFailed);
                }

                AuthenticationRequestParameters.RequestContext.Logger.InfoPii(string.Format(CultureInfo.CurrentCulture,
                    " User with user name '{0}' detected as '{1}'", UserCredential.UserName,
                    userRealmResponse.AccountType));

                if (string.Compare(userRealmResponse.AccountType, "federated", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (string.IsNullOrWhiteSpace(userRealmResponse.FederationMetadataUrl))
                    {
                        throw new MsalException(MsalError.MissingFederationMetadataUrl);
                    }

                    WsTrustAddress wsTrustAddress = null;
                    try
                    {
                        wsTrustAddress = await MexParser.FetchWsTrustAddressFromMexAsync(
                            userRealmResponse.FederationMetadataUrl, UserCredential.UserAuthType, AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);
                        if (wsTrustAddress == null)
                        {
                            throw new MsalException(MsalError.WsTrustEndpointNotFoundInMetadataDocument);
                        }
                    }
                    catch (System.Xml.XmlException ex)
                    {
                        throw new MsalException(MsalError.ParsingWsMetadataExchangeFailed, MsalError.ParsingWsMetadataExchangeFailed, ex);
                    }
                    AuthenticationRequestParameters.RequestContext.Logger.InfoPii(string.Format(CultureInfo.CurrentCulture, " WS-Trust endpoint '{0}' fetched from MEX at '{1}'",
                            wsTrustAddress.Uri, userRealmResponse.FederationMetadataUrl));

                    WsTrustResponse wsTrustResponse = await WsTrustRequest.SendRequestAsync(
                        wsTrustAddress, UserCredential, AuthenticationRequestParameters.RequestContext, userRealmResponse.CloudAudienceUrn).ConfigureAwait(false);
                    if (wsTrustResponse == null)
                    {
                        throw new MsalException(MsalError.ParsingWsTrustResponseFailed);
                    }


                    var msg = string.Format(CultureInfo.CurrentCulture,
                        " Token of type '{0}' acquired from WS-Trust endpoint", wsTrustResponse.TokenType);
                    AuthenticationRequestParameters.RequestContext.Logger.Info(msg);
                    AuthenticationRequestParameters.RequestContext.Logger.InfoPii(msg);

                    // We assume that if the response token type is not SAML 1.1, it is SAML 2
                    UserAssertion = new UserAssertion(wsTrustResponse.Token, (wsTrustResponse.TokenType == WsTrustResponse.Saml1Assertion) ? OAuth2GrantType.Saml11Bearer : OAuth2GrantType.Saml20Bearer);
                }
                else
                {
                    throw new MsalException(MsalError.UnknownUserType,
                        string.Format(CultureInfo.CurrentCulture, MsalErrorMessage.UnsupportedUserType, userRealmResponse.AccountType));
                }
            }
            await base.SendTokenRequestAsync().ConfigureAwait(false);
        }

        protected override void SetAdditionalRequestParameters(OAuth2Client client)
        {
            if (UserAssertion != null)
            {
                client.AddBodyParameter(OAuth2Parameter.GrantType, UserAssertion.AssertionType);
                client.AddBodyParameter(OAuth2Parameter.Assertion, Convert.ToBase64String(Encoding.UTF8.GetBytes(UserAssertion.Assertion)));
            }
        }
    }
}
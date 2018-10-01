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

using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Realm;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.Identity.Core.WsTrust
{
    internal class CommonNonInteractiveHandler
    {
        private readonly RequestContext requestContext;
        private readonly IUsernameInput usernameInput;
        private readonly IPlatformProxy platformProxy;

        public CommonNonInteractiveHandler(RequestContext requestContext, IUsernameInput usernameInput)
        {
            this.requestContext = requestContext;
            this.usernameInput = usernameInput;
            this.platformProxy = PlatformProxyFactory.GetPlatformProxy();
        }

        /// <summary>
        /// Gets the currently logged in user. Works for Windows when user is AD or AAD joined. Throws otherwise if cannot be found.
        /// </summary>
        public async Task<string> GetPlatformUserAsync()
        {
            var logger = this.requestContext.Logger;
            string platformUsername = await this.platformProxy.GetUserPrincipalNameAsync().ConfigureAwait(false);            
            if (string.IsNullOrWhiteSpace(platformUsername))
            {
                logger.Error("Could not find UPN for logged in user.");

                throw CoreExceptionFactory.Instance.GetClientException(
                    CoreErrorCodes.UnknownUser,
                    CoreErrorMessages.UnknownUser);

            }

            logger.InfoPii($"Logged in user detected with user name '{platformUsername}'", "Logged in user detected");

            return platformUsername;
        }

        public async Task<WsTrustResponse> QueryWsTrustAsync(
            MexParser mexParser,
            UserRealmDiscoveryResponse userRealmResponse,
            Func<string, WsTrustAddress, IUsernameInput, StringBuilder> wsTrustMessageBuilder)
        {

            WsTrustAddress wsTrustAddress = await QueryForWsTrustAddressAsync(userRealmResponse, mexParser).ConfigureAwait(false);

            return await QueryWsTrustAsync(
                wsTrustMessageBuilder,
                userRealmResponse.CloudAudienceUrn,
                wsTrustAddress).ConfigureAwait(false);
        }

        public async Task<UserRealmDiscoveryResponse> QueryUserRealmDataAsync(string userRealmUriPrefix)
        {
            var logger = this.requestContext.Logger;

            var userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(
                userRealmUriPrefix,
                usernameInput.UserName,
                requestContext).ConfigureAwait(false);

            if (userRealmResponse == null)
            {
                throw CoreExceptionFactory.Instance.GetClientException(
                    CoreErrorCodes.UserRealmDiscoveryFailed,
                    CoreErrorMessages.UserRealmDiscoveryFailed);
            }

            logger.InfoPii(
                string.Format(
                    CultureInfo.CurrentCulture,
                    " User with user name '{0}' detected as '{1}'", 
                    usernameInput.UserName,
                    userRealmResponse.AccountType),
                string.Empty);

            return userRealmResponse;
        }

        private async Task<WsTrustResponse> QueryWsTrustAsync(
            Func<string, WsTrustAddress, IUsernameInput, StringBuilder> wsTrustMessageBuilder,
            string cloudAudience,
            WsTrustAddress wsTrustAddress)
        {
            WsTrustResponse wsTrustResponse;
            StringBuilder wsTrustRequest = null;
            try
            {
                wsTrustRequest = wsTrustMessageBuilder(cloudAudience, wsTrustAddress, this.usernameInput);

                wsTrustResponse = await WsTrustRequest.SendRequestAsync(
                    wsTrustAddress,
                    wsTrustRequest.ToString(),
                    this.requestContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw CoreExceptionFactory.Instance.GetClientException(
                    CoreErrorCodes.ParsingWsTrustResponseFailed,
                    ex.Message,
                    ex);
            }
            finally
            {
                wsTrustRequest?.SecureClear();
            }


            if (wsTrustResponse == null)
            {
                throw CoreExceptionFactory.Instance.GetClientException(
                    CoreErrorCodes.ParsingWsTrustResponseFailed,
                    CoreErrorMessages.ParsingWsTrustResponseFailed);
            }

            this.requestContext.Logger.Info(string.Format(CultureInfo.CurrentCulture,
                " Token of type '{0}' acquired from WS-Trust endpoint", wsTrustResponse.TokenType));

            return wsTrustResponse;
        }


        private async Task<WsTrustAddress> QueryForWsTrustAddressAsync(
            UserRealmDiscoveryResponse userRealmResponse, 
            MexParser mexParser)
        {
            if (string.IsNullOrWhiteSpace(userRealmResponse.FederationMetadataUrl))
            {
                throw CoreExceptionFactory.Instance.GetClientException(
                    CoreErrorCodes.MissingFederationMetadataUrl,
                    CoreErrorMessages.MissingFederationMetadataUrl);
            }

            WsTrustAddress wsTrustAddress = null;
            try
            {
                wsTrustAddress = await mexParser.FetchWsTrustAddressFromMexAsync(
                    userRealmResponse.FederationMetadataUrl)
                    .ConfigureAwait(false);

                if (wsTrustAddress == null)
                {
                    CoreExceptionFactory.Instance.GetClientException(
                      CoreErrorCodes.WsTrustEndpointNotFoundInMetadataDocument,
                      CoreErrorMessages.WsTrustEndpointNotFoundInMetadataDocument);
                }
            }
            catch (XmlException ex)
            {
                throw CoreExceptionFactory.Instance.GetClientException(
                    CoreErrorCodes.ParsingWsMetadataExchangeFailed,
                    CoreErrorMessages.ParsingMetadataDocumentFailed,
                    ex);
            }

            this.requestContext.Logger.InfoPii(
                string.Format(CultureInfo.CurrentCulture, " WS-Trust endpoint '{0}' fetched from MEX at '{1}'",
                    wsTrustAddress.Uri, userRealmResponse.FederationMetadataUrl),
                "Fetched and parsed MEX");

            return wsTrustAddress;
        }
    }
}
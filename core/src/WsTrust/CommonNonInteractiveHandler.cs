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
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Identity.Core.Realm;

namespace Microsoft.Identity.Core.WsTrust
{
    internal class CommonNonInteractiveHandler
    {
        private readonly RequestContext _requestContext;
        private readonly IUsernameInput _usernameInput;
        private readonly IPlatformProxy _platformProxy;
        private readonly IWsTrustWebRequestManager _wsTrustWebRequestManager;

        public CommonNonInteractiveHandler(
            RequestContext requestContext,
            IUsernameInput usernameInput,
            IWsTrustWebRequestManager wsTrustWebRequestManager,
            IPlatformProxy platformProxy = null)
        {
            _requestContext = requestContext;
            _usernameInput = usernameInput;
            _platformProxy = platformProxy ?? PlatformProxyFactory.GetPlatformProxy();
            _wsTrustWebRequestManager = wsTrustWebRequestManager;
        }

        /// <summary>
        /// Gets the currently logged in user. Works for Windows when user is AD or AAD joined. Throws otherwise if cannot be found.
        /// </summary>
        public async Task<string> GetPlatformUserAsync()
        {
            string platformUsername = await _platformProxy.GetUserPrincipalNameAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(platformUsername))
            {
                _requestContext.Logger.Error("Could not find UPN for logged in user.");

                throw CoreExceptionFactory.Instance.GetClientException(
                    CoreErrorCodes.UnknownUser,
                    CoreErrorMessages.UnknownUser);
            }

            _requestContext.Logger.InfoPii($"Logged in user detected with user name '{platformUsername}'", "Logged in user detected");
            return platformUsername;
        }

        public async Task<UserRealmDiscoveryResponse> QueryUserRealmDataAsync(string userRealmUriPrefix)
        {
            var userRealmResponse = await _wsTrustWebRequestManager.GetUserRealmAsync(
                userRealmUriPrefix,
                _usernameInput.UserName,
                _requestContext).ConfigureAwait(false);

            if (userRealmResponse == null)
            {
                throw CoreExceptionFactory.Instance.GetClientException(
                    CoreErrorCodes.UserRealmDiscoveryFailed,
                    CoreErrorMessages.UserRealmDiscoveryFailed);
            }

            _requestContext.Logger.InfoPii(
                $"User with user name '{_usernameInput.UserName}' detected as '{userRealmResponse.AccountType}'",
                string.Empty);

            return userRealmResponse;
        }

        public async Task<WsTrustResponse> PerformWsTrustMexExchangeAsync(
            string federationMetadataUrl, string cloudAudienceUrn, UserAuthType userAuthType)
        {
            MexDocument mexDocument;

            try
            {
                mexDocument = await _wsTrustWebRequestManager.GetMexDocumentAsync(
                federationMetadataUrl, _requestContext).ConfigureAwait(false);
            }
            catch (XmlException ex)
            {
                throw CoreExceptionFactory.Instance.GetClientException(
                    CoreErrorCodes.ParsingWsMetadataExchangeFailed,
                    CoreErrorMessages.ParsingMetadataDocumentFailed,
                    ex);
            }

            WsTrustEndpoint wsTrustEndpoint = userAuthType == UserAuthType.IntegratedAuth
                ? mexDocument.GetWsTrustWindowsTransportEndpoint()
                : mexDocument.GetWsTrustUsernamePasswordEndpoint();

            if (wsTrustEndpoint == null)
            {
                throw CoreExceptionFactory.Instance.GetClientException(
                  CoreErrorCodes.WsTrustEndpointNotFoundInMetadataDocument,
                  CoreErrorMessages.WsTrustEndpointNotFoundInMetadataDocument);
            }

            _requestContext.Logger.InfoPii(
                string.Format(CultureInfo.InvariantCulture, "WS-Trust endpoint '{0}' being used from MEX at '{1}'", wsTrustEndpoint.Uri, federationMetadataUrl),
                "Fetched and parsed MEX");

            WsTrustResponse wsTrustResponse = await GetWsTrustResponseAsync(
                userAuthType,
                cloudAudienceUrn,
                wsTrustEndpoint,
                _usernameInput).ConfigureAwait(false);

            _requestContext.Logger.Info($"Token of type '{wsTrustResponse.TokenType}' acquired from WS-Trust endpoint");

            return wsTrustResponse;
        }

        internal async Task<WsTrustResponse> GetWsTrustResponseAsync(
            UserAuthType userAuthType,
            string cloudAudienceUrn,
            WsTrustEndpoint endpoint,
            IUsernameInput usernameInput)
        {
            // TODO: need to clean up the casting to UsernamePasswordInput as well as removing the PasswordToCharArray
            // since we're putting the strings onto the managed heap anyway.
            string wsTrustRequestMessage = userAuthType == UserAuthType.IntegratedAuth
                ? endpoint.BuildTokenRequestMessageWindowsIntegratedAuth(cloudAudienceUrn)
                : endpoint.BuildTokenRequestMessageUsernamePassword(
                    cloudAudienceUrn,
                    usernameInput.UserName,
                    new string(((UsernamePasswordInput)usernameInput).PasswordToCharArray()));

            try
            {
                WsTrustResponse wsTrustResponse = await _wsTrustWebRequestManager.GetWsTrustResponseAsync(
                    endpoint, wsTrustRequestMessage, _requestContext).ConfigureAwait(false);

                _requestContext.Logger.Info($"Token of type '{wsTrustResponse.TokenType}' acquired from WS-Trust endpoint");
                return wsTrustResponse;
            }
            catch (Exception ex)
            {
                throw CoreExceptionFactory.Instance.GetClientException(
                    CoreErrorCodes.ParsingWsTrustResponseFailed,
                    ex.Message,
                    ex);
            }
        }
    }
}
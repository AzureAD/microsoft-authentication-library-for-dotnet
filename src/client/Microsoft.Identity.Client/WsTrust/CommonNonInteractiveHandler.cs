// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Security;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.WsTrust
{
    internal class CommonNonInteractiveHandler
    {
        private readonly RequestContext _requestContext;
        private readonly IServiceBundle _serviceBundle;

        public CommonNonInteractiveHandler(
            RequestContext requestContext,
            IServiceBundle serviceBundle)
        {
            _requestContext = requestContext;
            _serviceBundle = serviceBundle;
        }

        /// <summary>
        /// Gets the currently logged in user. Works for Windows when user is AD or AAD joined. Throws otherwise if cannot be found.
        /// </summary>
        public async Task<string> GetPlatformUserAsync()
        {
            string platformUsername = await _serviceBundle.PlatformProxy.GetUserPrincipalNameAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(platformUsername))
            {
                _requestContext.Logger.Error("Could not find UPN for logged in user. ");

                throw new MsalClientException(
                    MsalError.UnknownUser,
                    MsalErrorMessage.UnknownUser);
            }

            _requestContext.Logger.InfoPii(
                () => $"Logged in user detected with user name '{platformUsername}'",
                () => "Logged in user detected. ");
            return platformUsername;
        }

        public async Task<UserRealmDiscoveryResponse> QueryUserRealmDataAsync(string userRealmUriPrefix, string username)
        {
            var userRealmResponse = await _serviceBundle.WsTrustWebRequestManager.GetUserRealmAsync(
                userRealmUriPrefix,
                username,
                _requestContext).ConfigureAwait(false);

            if (string.Equals(userRealmResponse.DomainName, Constants.UserRealmMsaDomainName))
            {
                throw new MsalClientException(
                    MsalError.RopcDoesNotSupportMsaAccounts,
                    MsalErrorMessage.RopcDoesNotSupportMsaAccounts);
            }

            _requestContext.Logger.InfoPii(
                () => $"User with user name '{username}' detected as '{userRealmResponse.AccountType}'. ",
                () => $"User detected as '{userRealmResponse.AccountType}'. ");

            return userRealmResponse;
        }

        public async Task<WsTrustResponse> PerformWsTrustMexExchangeAsync(
            string federationMetadataUrl, string cloudAudienceUrn, UserAuthType userAuthType, string username, string password, string federationMetadataFilename)
        {
            MexDocument mexDocument;

            try
            {
                mexDocument = await _serviceBundle.WsTrustWebRequestManager.GetMexDocumentAsync(
                federationMetadataUrl,
                _requestContext,
                federationMetadataFilename).ConfigureAwait(false);
            }
            catch (XmlException ex)
            {
                throw new MsalClientException(
                    MsalError.ParsingWsMetadataExchangeFailed,
                    MsalErrorMessage.ParsingMetadataDocumentFailed,
                    ex);
            }

            WsTrustEndpoint wsTrustEndpoint = userAuthType == UserAuthType.IntegratedAuth
                ? mexDocument.GetWsTrustWindowsTransportEndpoint()
                : mexDocument.GetWsTrustUsernamePasswordEndpoint();

            if (wsTrustEndpoint == null)
            {
                throw new MsalClientException(
                  MsalError.WsTrustEndpointNotFoundInMetadataDocument,
                  MsalErrorMessage.WsTrustEndpointNotFoundInMetadataDocument);
            }

            _requestContext.Logger.VerbosePii(
                () => string.Format(CultureInfo.InvariantCulture, "WS-Trust endpoint '{0}' being used from MEX at '{1}'", wsTrustEndpoint.Uri, federationMetadataUrl),
                () => "Fetched and parsed MEX. ");

            WsTrustResponse wsTrustResponse = await GetWsTrustResponseAsync(
                userAuthType,
                cloudAudienceUrn,
                wsTrustEndpoint,
                username,
                password).ConfigureAwait(false);

            _requestContext.Logger.Info(() => $"Token of type '{wsTrustResponse.TokenType}' acquired from WS-Trust endpoint. ");

            return wsTrustResponse;
        }

        internal async Task<WsTrustResponse> GetWsTrustResponseAsync(
            UserAuthType userAuthType,
            string cloudAudienceUrn,
            WsTrustEndpoint endpoint,
            string username,
            string password)
        {
            string wsTrustRequestMessage = userAuthType == UserAuthType.IntegratedAuth
                ? endpoint.BuildTokenRequestMessageWindowsIntegratedAuth(cloudAudienceUrn)
                : endpoint.BuildTokenRequestMessageUsernamePassword(
                    cloudAudienceUrn,
                    username,
                    password);

            try
            {
                WsTrustResponse wsTrustResponse = await _serviceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(
                    endpoint, wsTrustRequestMessage, _requestContext).ConfigureAwait(false);

                _requestContext.Logger.Info(() => $"Token of type '{wsTrustResponse.TokenType}' acquired from WS-Trust endpoint. ");
                return wsTrustResponse;
            }
            catch (Exception ex) when (ex is not MsalClientException)
            {
                throw new MsalClientException(
                    MsalError.ParsingWsTrustResponseFailed,
                    MsalErrorMessage.ParsingWsTrustResponseFailedDueToConfiguration +
                    " Error Message: " + ex.Message,
                    ex);
            }
        }
    }
}

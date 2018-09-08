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

namespace Microsoft.Identity.Core
{

    /// <summary>
    /// The active directory authentication error message.
    /// </summary>
    internal static class MsalErrorMessage
    {
        public const string AccessingMetadataDocumentFailed = "Accessing WS metadata exchange failed";

        public const string AssemblyNotFoundTemplate =
            "Assembly required for the platform not found. Make sure assembly '{0}' exists";

        public const string AssemblyLoadFailedTemplate =
            "Loading an assembly required for the platform failed. Make sure assembly for the correct platform '{0}' exists";

        public const string AuthenticationUiFailed = "The browser based authentication dialog failed to complete";
        
        public const string DeprecatedAuthorityError = "login.windows.net has been deprecated. Use login.microsoftonline.com instead.";

        public const string CertificateKeySizeTooSmallTemplate =
            "The certificate used must have a key size of at least {0} bits";

        public const string EmailAddressSuffixMismatch =
            "No identity provider email address suffix matches the provided address";

        public const string EncodedTokenTooLong = "Encoded token size is beyond the upper limit";
        public const string FailedToAcquireTokenSilently = "Failed to acquire token silently. Call method AcquireToken";
        public const string FailedToRefreshToken = "Failed to refresh token";
        public const string FederatedServiceReturnedErrorTemplate = "Federated service at {0} returned error: {1}";
        public const string IdentityProtocolLoginUrlNull = "The LoginUrl property in identityProvider cannot be null";
        public const string IdentityProtocolMismatch = "No identity provider matches the requested protocol";

        public const string IdentityProviderRequestFailed =
            "Token request to identity provider failed. Check InnerException for more details";

        public const string InvalidArgumentLength = "Parameter has invalid length";
        public const string InvalidAuthenticateHeaderFormat = "Invalid authenticate header format";
        public const string InvalidAuthorityTypeTemplate = "This method overload is not supported by '{0}'";
        public const string InvalidCredentialType = "Invalid credential type";
        public const string InvalidFormatParameterTemplate = "Parameter '{0}' has invalid format";
        public const string InvalidTokenCacheKeyFormat = "Invalid token cache key format";
        public const string MissingAuthenticateHeader = "WWW-Authenticate header was expected in the response";

        public const string MultipleTokensMatched =
            "The cache contains multiple tokens satisfying the requirements. Try to clear token cache";

        public const string NetworkNotAvailable = "The network is down so authentication cannot proceed";
        public const string NoDataFromSTS = "No data received from security token service";
        public const string NullParameterTemplate = "Parameter '{0}' cannot be null";
        public const string ParsingMetadataDocumentFailed = "Parsing WS metadata exchange failed";
        public const string ParsingWsTrustResponseFailed = "Parsing WS-Trust response failed";
        public const string PasswordRequiredForManagedUserError = "Password is required for managed user";
        public const string RedirectUriContainsFragment = "'redirectUri' must NOT include a fragment component";
        public const string LoginHintNullForUiOption = "Null login_hint is not allowed for UIBehavior.ActAsCurrentUser";
        public const string ServiceReturnedError = "Service returned error. Check InnerException for more details";

        public const string BrokerReponseHashMismatch =
            "Unencrypted broker response hash did not match the expected hash";

        public const string StsMetadataRequestFailed =
            "Metadata request to Access Control service failed. Check InnerException for more details";

        public const string StsTokenRequestFailed =
            "Token request to security token service failed.  Check InnerException for more details";

        public const string UnauthorizedHttpStatusCodeExpected =
            "Unauthorized Http Status Code (401) was expected in the response";

        public const string UnauthorizedResponseExpected = "Unauthorized http response (status code 401) was expected";
        public const string UnexpectedAuthorityValidList = "Unexpected list of valid addresses";

        public const string UnknownUser = "Could not identify logged in user";
        public const string UnsupportedUserType = "Unsupported User Type";

        public const string UnsupportedMultiRefreshToken =
            "This authority does not support refresh token for multiple resources. Pass null as a resource";

        public const string UserMismatch = "User '{0}' returned by service does not match user '{1}' in the request";
        public const string UserCredentialAssertionTypeEmpty = "credential.AssertionType cannot be empty";

        public const string NoPromptFailedErrorMessage =
            "One of two conditions was encountered: "
            +
            "1. The UiBehavior.Never flag was passed, but the constraint could not be honored, because user interaction was required. "
            +
            "2. An error occurred during a silent web authentication that prevented the http authentication flow from completing in a short enough time frame";

        public const string UserRealmDiscoveryFailed = "User realm discovery failed";

        public const string WsTrustEndpointNotFoundInMetadataDocument =
            "WS-Trust endpoint not found in metadata document";

        public const string GetUserNameFailed = "Failed to get user name";

        public const string MissingFederationMetadataUrl =
            "Federation Metadata Url is missing for federated user. This user type is unsupported.";

        public const string SpecifyAnyUser =
            "If you do not need access token for any specific user, pass userId=UserIdentifier.AnyUser instead of userId=null.";

        public const string IntegratedAuthFailed =
            "Integrated authentication failed. You may try an alternative authentication method";

        public const string DuplicateQueryParameterTemplate = "Duplicate query parameter '{0}' in extraQueryParameters";
        public const string DeviceCertificateNotFoundTemplate = "Device Certificate was not found for {0}";
    }

}

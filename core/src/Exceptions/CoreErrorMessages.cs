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
    internal class CoreErrorMessages
    {
        public const string UnknownUser = "Could not identify the user logged into the OS. See http://aka.ms/msal-net-iwa for details.";

        public const string HttpRequestUnsuccessful = "Response status code does not indicate success: {0} ({1}).";

        public const string AuthorityInvalidUriFormat = "'authority' should be in Uri format";

        public const string AuthorityNotSupported = "'authority' is not supported";

        public const string AuthorityValidationFailed = "Authority validation failed";

        public const string AuthorityUriInsecure = "'authority' should use the 'https' scheme";

        public const string AuthorityUriInvalidPath =
         "'authority' Uri should have at least one segment in the path (i.e. https://<host>/<path>/...)";

        public const string B2cAuthorityUriInvalidPath =
          "B2C 'authority' Uri should have at least 3 segments in the path (i.e. https://<host>/tfp/<tenant>/<policy>/...)";

        public const string UnsupportedAuthorityValidation =
            "Authority validation is not supported for this type of authority";

        public const string AuthenticationCanceled = "User canceled authentication";

        public const string Unknown = "Unknown error";

        public const string AuthorizationServerInvalidResponse = "The authorization server returned an invalid response";

        public const string NonHttpsRedirectNotSupported = "Non-HTTPS url redirect is not supported in webview";

        public const string IDTokenMustHaveTwoParts = "ID Token must contain at least 2 parts.";
        public const string FailedToParseIDToken = "Failed to parse the returned id token.";

        public const string InvalidAuthorityOpenId = "invalid authority while getting the open id config endpoint";
        public const string UpnRequiredForAuthroityValidation = "UPN is required for ADFS authority validation.";
        public const string CannotFindTheAuthEndpont = "Cannot find the auth endpoint";

        public const string UapCannotFindUpn =
           "Cannot find the user logged into Windows, but found a domain the name. Possible cause: the UWP application does not request the Enterprise Authentication capability.";

        public const string UapCannotFindDomainUser =
            "Cannot find the user logged into Windows. Possible causes: the application does not request the User Account Information, Enterprise Authentication and Private Networks (Client & Server) capabilities or the user is not AD or AAD joined.";

        public const string PlatformNotSupported = "Platform Not Supported";

        public const string FederatedServiceReturnedErrorTemplate = "Federated service at {0} returned error: {1}";
        public const string UserRealmDiscoveryFailed = "User realm discovery failed";
        public const string MissingFederationMetadataUrl =
         "Federation Metadata Url is missing for federated user. This user type is unsupported.";
        public const string WsTrustEndpointNotFoundInMetadataDocument =
           "WS-Trust endpoint not found in metadata document";
        public const string ParsingMetadataDocumentFailed = "Parsing WS metadata exchange failed";
        public const string ParsingWsTrustResponseFailed = "Parsing WS-Trust response failed";
        public const string UnknownUserType = "Unknown User Type";

        public const string InternalErrorCacheEmptyUsername =
            "Internal error - trying to remove an ADAL user with an empty username. Possible cache corruption. See https://aka.ms/adal_token_cache_serialization";
        public const string InternalErrorCacheEmptyIdentifier =
            "Internal error - trying to remove an ADAL user with an empty identifier. Possible cache corruption. See https://aka.ms/adal_token_cache_serialization";

        public const string GetUserNameFailed = "Failed to get user name from the operating system.";

        public const string NonParsableOAuthError = "An error response was returned by the OAuth2 server, but it could not be parsed. Please inspect the exception properties for details.";

        public const string CannotAccessPublisherKeyChain =
           "The application cannot access the iOS keychain for the application publisher (the TeamId is null). " +
           "This is needed to enable Single Sign On between applications of the same publisher. " +
           "This is an iOS configuration issue. See https://aka.ms/msal-net-enable-keychain-access for more details on enabling keychain access.";
    }
}
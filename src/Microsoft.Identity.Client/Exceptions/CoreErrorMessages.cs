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
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Client
{
    internal static class CoreErrorMessages
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
            "Authority validation is not supported for this type of authority. See http://aka.ms/valid-authorities for details";

        public const string AuthenticationCanceled = "User canceled authentication. On an Android device, this could be " +
            "due to the lack of capabilities, such as custom tabs, for the system browser." +
            " See https://aka.ms/msal-net-system-browsers for more information.";

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
            "Internal error - trying to remove an MSAL user with an empty username. Possible cache corruption. See https://aka.ms/adal_token_cache_serialization";
        public const string InternalErrorCacheEmptyIdentifier =
            "Internal error - trying to remove an MSAL user with an empty identifier. Possible cache corruption. See https://aka.ms/adal_token_cache_serialization";

        public const string GetUserNameFailed = "Failed to get user name from the operating system.";

        public const string NonParsableOAuthError = "An error response was returned by the OAuth2 server, but it could not be parsed. Please inspect the exception properties for details.";

        public const string CannotAccessPublisherKeyChain =
           "The application cannot access the iOS keychain for the application publisher (the TeamId is null). " +
           "This is needed to enable Single Sign On between applications of the same publisher. " +
           "This is an iOS configuration issue. See https://aka.ms/msal-net-enable-keychain-access for more details on enabling keychain access.";

        public const string MissingEntitlements =
            "The application does not have keychain access groups enabled in the Entitlements.plist. " +
            "As a result, there was a failure to save to the iOS keychain. " +
            "The keychain access group '{0}' is not enabled in the Entitlements.plist. " +
            "See https://aka.ms/msal-net-enable-keychain-groups for more details on enabling keychain access groups and entitlements.";

        public const string AndroidActivityNotFound = "The Activity cannot be found to launch the given Intent. To ensure authentication, a browser with custom tab support " +
            "is recommended. See https://aka.ms/msal-net-system-browsers for more details on using system browser on Android.";

        public const string DefaultRedirectUriIsInvalid = "The OAuth2 redirect uri {0} should not be used with the system browser, because the operating system cannot go back to the app. Consider using the default redirect uri for this platform. See https://aka.ms/msal-client-apps for more details.";

        public const string RedirectUriContainsFragment = "'redirectUri' must NOT include a fragment component";

        public const string NoRedirectUri = "No redirectUri was configured. MSAL does not provide any defaults.";


        public const string ClientApplicationBaseExecutorNotImplemented =
            "ClientApplicationBase implementation does not implement IClientApplicationBaseExecutor.";

        public const string ActivityRequiredForParentObjectAndroid = "Activity is required for parent object on Android.";

        public const string LoggingCallbackAlreadySet = "LoggingCallback has already been set";
        public const string TelemetryCallbackAlreadySet = "TelemetryCallback has already been set";
        public const string NoClientIdWasSpecified = "No ClientId was specified.";
        public const string AdfsNotCurrentlySupportedAuthorityType = "ADFS is not currently a supported authority type.";
        public const string TenantIdAndAadAuthorityInstanceAreMutuallyExclusive = "TenantId and AadAuthorityAudience are both set, but they're mutually exclusive.";
        public const string InstanceAndAzureCloudInstanceAreMutuallyExclusive = "Instance and AzureCloudInstance are both set but they're mutually exclusive.";
        public const string NoRefreshTokenProvided = "A refresh token must be provided.";

        public const string NullTokenCacheError = "Token cache is set to null. Acquire by refresh token requests cannot be executed.";

        public const string NoRefreshTokenInResponse = "Acquire by refresh token request completed, but no refresh token was found";

        public const string ConfidentialClientDoesntImplementIConfidentialClientApplicationExecutor =
            "ConfidentialClientApplication implementation does not implement IConfidentialClientApplicationExecutor.";

        public const string ClientSecretAndCertificateAreMutuallyExclusive = "ClientSecret and Certificate are mutually exclusive properties.  Only specify one.";
        public const string ClientIdMustBeAGuid = "Error: ClientId is not a Guid.";

        public static string InvalidRedirectUriReceived(string invalidRedirectUri)
        {
            return string.Format(CultureInfo.InvariantCulture, "Invalid RedirectURI was received ({0})  Not parseable into System.Uri class.", invalidRedirectUri);
        }

        public const string TelemetryClassIsObsolete =
            "Telemetry is now specified per ClientApplication.  See https://aka.ms/msal-net-3-breaking-changes and https://aka.ms/msal-net-application-configuration";

        public const string LoggingClassIsObsolete =
            "Logging is now specified per ClientApplication.  See https://aka.ms/msal-net-3-breaking-changes and https://aka.ms/msal-net-application-configuration";

        public const string AuthorityDoesNotHaveTwoSegments =
            "Authority should be in the form <host>/<audience>, for example https://login.microsoftonline.com/common";
        public const string AzureAdMyOrgRequiresSpecifyingATenant = "When specifying AadAuthorityAudience.AzureAdMyOrg, you must also specify a tenant domain or tenant guid.";

        public const string CustomWebUiReturnedNullUri = "ICustomWebUi returned a null uri";

        public static string CustomWebUiRedirectUriWasNotMatchedToProperUri(string expectedUri, string actualUri)
        {
            return string.Format(CultureInfo.InvariantCulture, "Redirect Uri was not a match to the proper uri.  Expected ({0}) Actual ({1})", expectedUri, actualUri);
        }

        public const string CustomWebUiAuthorizationCodeFailed = "CustomWebUi AcquireAuthorizationCode failed";

        public const string BrokerNotSupportedOnThisPlatform = "Broker is only supported on mobile platforms (Android and iOS). See https://aka.ms/msal-brokers for details";
    }
}
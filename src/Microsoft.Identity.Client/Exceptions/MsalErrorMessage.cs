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

using System.Globalization;

namespace Microsoft.Identity.Client.Exceptions
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
        public const string LoginHintNullForUiOption = "Null login_hint is not allowed for Prompt.ActAsCurrentUser";
        public const string ServiceReturnedError = "Service returned error. Check InnerException for more details";

        public const string BrokerResponseHashMismatch =
            "Unencrypted broker response hash did not match the expected hash";

        public const string StsMetadataRequestFailed =
            "Metadata request to Access Control service failed. Check InnerException for more details";

        public const string StsTokenRequestFailed =
            "Token request to security token service failed.  Check InnerException for more details";

        public const string UnauthorizedHttpStatusCodeExpected =
            "Unauthorized Http Status Code (401) was expected in the response";

        public const string UnauthorizedResponseExpected = "Unauthorized http response (status code 401) was expected";
        public const string UnexpectedAuthorityValidList = "Unexpected list of valid addresses";

        public const string UnsupportedUserType = "Unsupported User Type '{0}'. Please see https://aka.ms/msal-net-up";

        public const string UnsupportedMultiRefreshToken =
            "This authority does not support refresh token for multiple resources. Pass null as a resource";

        public const string UserMismatch = "User '{0}' returned by service does not match user '{1}' in the request";
        public const string UserCredentialAssertionTypeEmpty = "credential.AssertionType cannot be empty";

        public const string NoPromptFailedErrorMessage =
            "One of two conditions was encountered: "
            +
            "1. The Prompt.Never flag was passed, but the constraint could not be honored, because user interaction was required. "
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
        public const string MsalUiRequiredMessage =
            "No account or login hint was passed to the AcquireTokenSilent call.";

        public const string UserMismatchSaveToken = "Returned user identifier does not match the sent user identifier when saving the token to the cache.";
        public const string IwaNotSupportedForManagedUser = "Integrated Windows Auth is not supported for managed users. See https://aka.ms/msal-net-iwa for details.";
        public const string ActivityRequired = "On the Android platform, you have to pass the Activity to the UIParent object. See https://aka.ms/msal-interactive-android for details.";
        public const string BrokerResponseReturnedError = "Broker response returned an error which does not contain an error or error description. See https://aka.ms/msal-brokers for details. ";
        public const string BrokerResponseError = "Broker response returned error: ";
        public const string CannotInvokeBroker = "MSAL cannot invoke the broker. The Authenticator App (Broker) may not be installed on the user's device or there was an error invoking the broker. " +
            "Check logs for more details and see https://aka.ms/msal-brokers. ";
        public const string NoAccountForLoginHint = "You are trying to acquire a token silently using a login hint. No account was found in the token cache having this login hint.";
        public const string MultipleAccountsForLoginHint = "You are trying to acquire a token silently using a login hint. Multiple accounts were found in the token cache having this login hint. Please choose an account manually an pass it in to AcquireTokenSilently.";

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

        public const string AuthenticationCanceled = "User canceled authentication.";

        public const string AuthenticationCanceledAndroid = "User canceled authentication. On an Android device, this could be " +
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
        public const string UnknownUserType = "Unknown User Type";

        public const string InternalErrorCacheEmptyUsername =
            "Internal error - trying to remove an MSAL user with an empty username. Possible cache corruption. See https://aka.ms/adal_token_cache_serialization";
        public const string InternalErrorCacheEmptyIdentifier =
            "Internal error - trying to remove an MSAL user with an empty identifier. Possible cache corruption. See https://aka.ms/adal_token_cache_serialization";

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

        public const string CustomWebUiReturnedInvalidUri = "ICustomWebUi returned an invalid uri - it is empty or has no query.";

        public static string CustomWebUiRedirectUriMismatch(string expectedUri, string actualUri)
        {
            return string.Format(CultureInfo.InvariantCulture, "Redirect Uri mismatch.  Expected ({0}) Actual ({1})", expectedUri, actualUri);
        }

        public const string CustomWebUiAuthorizationCodeFailed = "CustomWebUi AcquireAuthorizationCode failed";

        public const string TokenCacheJsonSerializerFailedParse = "MSAL V3 Deserialization failed to parse the cache contents. Is this possibly an earlier format needed for DeserializeMsalV2? (See https://aka.ms/msal-net-3x-cache-breaking-change)";
        public const string TokenCacheDictionarySerializerFailedParse = "MSAL V2 Deserialization failed to parse the cache contents. Is this possibly an earlier format needed for DeserializeMsalV3?  (See https://aka.ms/msal-net-3x-cache-breaking-change)";
        public const string BrokerNotSupportedOnThisPlatform = "Broker is only supported on mobile platforms (Android and iOS). See https://aka.ms/msal-brokers for details";

        public const string MsalExceptionFailedToParse = "Attempted to deserialize an MsalException but the type was unknown.";
    }
}

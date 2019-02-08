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

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// This exception class represents errors that are local to the library or the device. Contrary to
    /// <see cref="MsalServiceException"/> which represent errors happening from the Azure AD service or
    /// the network. For more details, see https://aka.ms/msal-net-exceptions
    /// </summary>
    public class MsalClientException : MsalException
    {
        /// <summary>
        /// Multiple Tokens were matched. 
        /// <para>What happens?</para>This exception happens in the case of applications managing several identitities, 
        /// when calling <see cref="ClientApplicationBase.AcquireTokenSilentAsync(System.Collections.Generic.IEnumerable{string}, IAccount)"/>
        /// or one of its overrides and the user token cache contains multiple tokens for this client application and the the specified Account, but from different authorities.
        /// <para>Mitigation [App Development]</para>specify the authority to use in the acquire token operation
        /// </summary>
        public const string MultipleTokensMatchedError = "multiple_matching_tokens_detected";

        /// <summary>
        /// Non HTTPS redirects are not supported
        /// <para>What happens?</para>This error happens when you have registered a non-https redirect URI for the 
        /// public client application other than <c>urn:ietf:wg:oauth:2.0:oob</c>
        /// <para>Mitigation [App registration and development]</para>Register in the application a Reply URL starting with "https://"
        /// </summary>
        public const string NonHttpsRedirectNotSupported = "non_https_redirect_failed";

        /// <summary>
        /// The request could not be preformed because the network is down.
        /// <para>Mitigation [App development]</para> In the application you could either inform the user that there are network issues
        /// or retry later
        /// </summary>
        public const string NetworkNotAvailableError = "network_not_available";

#if !DESKTOP && !NET_CORE
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif
        /// <summary>
        /// Duplicate query parameter was found in extraQueryParameters.
        /// <para>What happens?</para> You have used <c>extraQueryParameter</c> of overrides
        /// of token acquisition operations in public client and confidential client application and are passing a parameter which is already present in the
        /// URL (either because you had it in another way, or the library added it).
        /// <para>Mitigation [App Development]</para> RemoveAccount the duplicate parameter from the token acquisition override.
        /// </summary>
        /// <seealso cref="ConfidentialClientApplication.GetAuthorizationRequestUrlAsync(System.Collections.Generic.IEnumerable{string}, string, string, string, System.Collections.Generic.IEnumerable{string}, string)"/>
        public const string DuplicateQueryParameterError = "duplicate_query_parameter";
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved

        /// <summary>
        /// The request could not be performed because of a failure in the UI flow.
        /// <para>What happens?</para>The library failed to invoke the Web View required to perform interactive authentication.
        /// The exception might include the reason
        /// <para>Mitigation</para>If the exception includes the reason, you could inform the user. This might be, for instance, a browser
        /// implementing chrome tabs is missing on the Android phone (that's only an example: this exception can apply to other
        /// platforms as well)
        /// </summary>
        public const string AuthenticationUiFailedError = "authentication_ui_failed";

        /// <summary>
        /// Authentication canceled.
        /// <para>What happens?</para>The user had canceled the authentication, for instance by closing the authentication dialog
        /// <para>Mitigation</para>None, you cannot get a token to call the protected API. You might want to inform the user
        /// </summary>
        public const string AuthenticationCanceledError = "authentication_canceled";

        /// <summary>
        /// JSON parsing failed.
        /// <para>What happens?</para>A Json blob read from the token cache or received from the STS was not parseable. 
        /// This can happen when reading the token cache, or receiving an IDToken from the STS.
        /// <para>Mitigation</para>Make sure that the token cache was not tampered
        /// </summary>
        public const string JsonParseError = "json_parse_failed";

        /// <summary>
        /// JWT was invalid.
        /// <para>What happens?</para>The library expected a JWT (for instance a token from the cache, or received from the STS), but
        /// the format is invalid
        /// <para>Mitigation</para>Make sure that the token cache was not tampered
        /// </summary>
        public const string InvalidJwtError = "invalid_jwt";

        /// <summary>
        /// State returned from the STS was different from the one sent by the library
        /// <para>What happens?</para>The library sends to the STS a state associated to a request, and expects the reply to be consistent. 
        /// This errors indicates that the reply is not associated with the request. This could indicate an attempt to replay a response
        /// <para>Mitigation</para> None
        /// </summary>
        public const string StateMismatchError = "state_mismatch";

        /// <summary>
        /// Tenant discovery failed.
        /// <para>What happens?</para>While reading the openid configuration associated with the authority, the Authorize endpoint,
        /// or Token endpoint, or the Issuer was not found
        /// <para>Mitigation</para>This indicates and authority which is not Open ID Connect compliant. Specify a different authority
        /// in the constructor of the application, or the token acquisition override
        /// /// </summary>
        public const string TenantDiscoveryFailedError = "tenant_discovery_failed";

        /// <summary>
        /// The library is loaded on a platform which is not supported.
        /// </summary>
        public const string PlatformNotSupported = "platform_not_supported";

#if iOS
        /// <summary>
        /// Xamarin.iOS specific. This error indicates that keychain access has not be enabled for the application.
        /// From MSAL 2.x and ADAL 4.x, the keychain for the publisher needs to be accessed in order to provide 
        /// Single Sign On between applications of the same publisher.
        /// <para>Mitigation</para> In order to access the keychain on iOS, you will need to ensure the Entitlements.plist
        /// file is configured and included under &amp;lt;CodesignEntitlements&amp;gt;Entitlements.plist&amp;lt;/CodesignEntitlements&amp;gt;
        /// in the csproj file of the iOS app.
        /// <para>For more details</para> See https://aka.ms/msal-net-enable-keychain-access
        /// </summary>
        public const string CannotAccessPublisherKeyChain = "cannot_access_publisher_keychain";

        /// <summary>
        /// Xamarin.iOS specific. This error indicates that saving a token to the keychain failed. 
        /// <para>Mitigation</para> In order to access the keychain on iOS, you will need to set the
        /// keychain access groups in the Entitlements.plist for the application.
        /// <para>For more details</para> See https://aka.ms/msal-net-enable-keychain-groups 
        /// </summary>
        public const string MissingEntitlements = "missing_entitlements";
#endif

#if ANDROID

        /// <summary>
        /// Xamarin.Android specific. This error indicates that chrome, or a browser implementing chrome tabs, is not installed on the device. 
        /// The library sdk uses chrome custom tab for authorize request if applicable or falls back to chrome browser.
        /// <para>Mitigation</para>If you really need to use the System web browser (for instance to get SSO with the browser), notify the end 
        /// user that chrome or a browser implementing chrome custom tabs needs to be installed on the device. 
        /// Otherwise you can also use <see cref="UIParent.IsSystemWebviewAvailable"/> to check if a required browser is available on the device
        /// and require the library to use the embedded web view if it is not by setting the boolean to <c>true</c> in the following
        /// constructor: <see cref="UIParent.UIParent(Android.App.Activity, bool)"/>
        /// <para>For more details</para> See https://aka.ms/msal-net-uses-web-browser
        /// </summary>
        [Obsolete("Catch AndroidActivityNotFound instead (See https://aka.ms/msal-net-uses-web-browser)", true)]
        public const string ChromeNotInstalledError = "chrome_not_installed";

        /// <summary>
        /// Xamarin.Android specific. This error indicates that chrome is installed on the device but disabled. The sdk uses chrome custom tab for
        /// authorize request if applicable or falls back to chrome browser.
        /// <para>Mitigation</para>If you really need to use the System web browser (for instance to get SSO with the browser), notify the end 
        /// user that chrome or a browser implementing chrome custom tabs needs to be installed on the device. 
        /// Otherwise you can also use <see cref="UIParent.IsSystemWebviewAvailable"/> to check if a required browser is available on the device
        /// and require the library to use the embedded web view if it is not by setting the boolean to <c>true</c> in the following
        /// constructor: <see cref="UIParent.UIParent(Android.App.Activity, bool)"/>
        /// <para>For more details</para> See https://aka.ms/msal-net-uses-web-browser
        /// </summary>
        [Obsolete("Catch AndroidActivityNotFound instead (See https://aka.ms/msal-net-uses-web-browser)", true)]
        public const string ChromeDisabledError = "chrome_disabled";

        /// <summary>
        /// Xamarin.Android specific. This error indicates that a system browser was not installed on the user's device, and authentication
        /// using system browser could not be attempted because there was no available Android activity to handle the intent.
        /// <para>Mitigation</para>If you want to use the System web browser (for instance to get SSO with the browser), notify the end 
        /// user that chrome or a browser implementing chrome custom tabs needs to be installed on the device. For a list of supported browsers with 
        /// custom tab support, please see https://aka.ms/msal-net-system-browsers.
        /// Otherwise you can use <see cref="UIParent.IsSystemWebviewAvailable"/> to check if a browser with custom tabs is available on the device
        /// and require the library to use the embedded web view if there is no such browser available by setting the boolean to <c>true</c> in the following
        /// constructor: <see cref="UIParent.UIParent(Android.App.Activity, bool)"/>
        /// <para>For more details</para> See https://aka.ms/msal-net-uses-web-browser
        /// </summary>
        public const string AndroidActivityNotFound = "android_activity_not_found";

        /// <summary>
        /// The intent to launch AuthenticationActivity is not resolvable by the OS or the intent.
        /// </summary>
        public const string UnresolvableIntentError = "unresolvable_intent";

        /// <summary>
        /// Failed to create shared preferences on the Android platform. 
        /// <para>What happens?</para> The library uses Android shared preferences to store the token cache
        /// <para>Mitigation</para> Make sure the application is configured to use this platform feature (See also
        /// the AndroidManifest.xml file, and https://aka.ms/msal-net-android-specificities
        /// </summary>
        public const string FailedToCreateSharedPreference = "shared_preference_creation_failed";

#endif

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code.
        /// </summary>
        /// <param name="errorCode">
        /// The error code returned by the service or generated by client. This is the code you can rely on
        /// for exception handling.</param>
        public MsalClientException(string errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code and error message.
        /// </summary>        
        /// <param name="errorCode">
        /// The error code returned by the service or generated by client. This is the code you can rely on
        /// for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        public MsalClientException(string errorCode, string errorMessage) : base(errorCode, errorMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code, error message and inner exception.
        /// </summary>
        /// <param name="errorCode">
        /// The error code returned by the service or generated by client. This is the code you can rely on
        /// for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        /// <param name="innerException"></param>
        public MsalClientException(string errorCode, string errorMessage, Exception innerException) : base(errorCode, errorMessage, innerException)
        {
        }
    }
}

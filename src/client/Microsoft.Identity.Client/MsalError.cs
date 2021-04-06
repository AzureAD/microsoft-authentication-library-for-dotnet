// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Error code returned as a property in MsalException
    /// </summary>
    public static class MsalError
    {
        /// <summary>
        /// Standard OAuth2 protocol error code. It indicates that the application needs to expose the UI to the user
        /// so that the user does an interactive action in order to get a new token.
        /// <para>Mitigation:</para> If your application is a <see cref="T:IPublicClientApplication"/> call <c>AcquireTokenInteractive</c>
        /// perform an interactive authentication. If your application is a <see cref="T:ConfidentialClientApplication"/> chances are that the Claims member
        /// of the exception is not empty. See <see cref="P:MsalServiceException.Claims"/> for the right mitigation
        /// </summary>
        public const string InvalidGrantError = "invalid_grant";

        /// <summary>
        /// Standard OAuth2 protocol error code. It indicates that the application needs to expose the UI to the user
        /// so the user can do an interactive log-in to get a token with updated claims.
        /// <para>Mitigation:</para> If your application is a <see cref="T:IPublicClientApplication"/> call <c>AcquireTokenInteractive</c>
        /// perform an interactive authentication. If your application is a <see cref="T:ConfidentialClientApplication"/> chances are that the Claims member
        /// of the exception is not empty. See <see cref="P:MsalServiceException.Claims"/> for the right mitigation
        /// </summary>
        public const string InteractionRequired = "interaction_required";

        /// <summary>
        /// No token was found in the token cache.
        /// <para>Mitigation:</para> If your application is a <see cref="IPublicClientApplication"/> call <c>AcquireTokenInteractive</c> so
        /// that the user of your application signs-in and accepts consent.
        /// <list type="bullet">
        /// <item>
        /// If it's a web app you should have previously called IConfidentialClientApplication.AcquireTokenByAuthorizationCode
        /// as described in https://aka.ms/msal-net-authorization-code. You need to make sure that you have requested the right scopes. For details
        /// See https://github.com/Azure-Samples/ms-identity-aspnetcore-webapp-tutorial
        /// </item>
        /// <item>This error should not happen in web APIs</item>
        /// </list>
        /// </summary>
        public const string NoTokensFoundError = "no_tokens_found";

        /// <summary>
        /// This error code comes back from <see cref="IClientApplicationBase.AcquireTokenSilent(System.Collections.Generic.IEnumerable{string}, IAccount)"/> calls when a null user is
        /// passed as the <c>account</c> parameter. This can be because you have called AcquireTokenSilent with an <c>account</c> parameter
        /// set to <c>accounts.FirstOrDefault()</c> but <c>accounts</c> is empty.
        /// <para>Mitigation</para>
        /// Pass a different account, or otherwise call <see cref="IPublicClientApplication.AcquireTokenInteractive(System.Collections.Generic.IEnumerable{string})"/>
        /// </summary>
        public const string UserNullError = "user_null";


        /// <summary>
        /// This error code comes back from <see cref="IClientApplicationBase.AcquireTokenSilent(System.Collections.Generic.IEnumerable{string}, IAccount)"/> calls when the 
        /// <see cref="PublicClientApplication.OperatingSystemAccount"/> user is passed as the <c>account</c> parameter. Only some brokers (WAM) can login the current user.
        /// <para>Mitigation</para>
        /// On Windows, use the broker via .WithBroker(true), use a different account, or otherwise call <see cref="IPublicClientApplication.AcquireTokenInteractive(System.Collections.Generic.IEnumerable{string})"/>
        /// </summary>
        public const string CurrentBrokerAccount = "current_broker_account";

        /// <summary>
        /// This error code denotes that no account was found having the given login hint.
        /// <para>What happens?</para>
        /// <see cref="IClientApplicationBase.AcquireTokenSilent(System.Collections.Generic.IEnumerable{string}, string)"/>
        /// or <see cref="AcquireTokenInteractiveParameterBuilder.WithLoginHint(string)"/>
        /// was called with a <c>loginHint</c> parameter which does not match any account in <see cref="IClientApplicationBase.GetAccountsAsync()"/>
        /// <para>Mitigation</para>
        /// If you are certain about the loginHint, call <see cref="IPublicClientApplication.AcquireTokenInteractive(System.Collections.Generic.IEnumerable{string})"/>
        /// </summary>
        public const string NoAccountForLoginHint = "no_account_for_login_hint";

        /// <summary>
        /// This error code denotes that multiple accounts were found having the same login hint and MSAL
        /// cannot choose one. Please use <see cref="AcquireTokenInteractiveParameterBuilder.WithAccount(IAccount)"/> to specify the account
        /// </summary>
        public const string MultipleAccountsForLoginHint = "multiple_accounts_for_login_hint";

        /// <summary>
        /// This error code comes back from <see cref="ClientApplicationBase.AcquireTokenSilent(System.Collections.Generic.IEnumerable{string}, IAccount)"/> calls when
        /// the user cache had not been set in the application constructor. This should never happen in MSAL.NET 3.x as the cache is created by the application
        /// </summary>
        [Obsolete("This error code is not in use")]
        public const string TokenCacheNullError = "token_cache_null";

        /// <summary>
        /// One of two conditions was encountered:
        /// <list type="bullet">
        /// <item><description>The <c>Prompt.NoPrompt</c> was passed in an interactive token call, but the constraint could not be honored because user interaction is required,
        /// for instance because the user needs to re-sign-in, give consent for more scopes, or perform multiple factor authentication.
        /// </description></item>
        /// <item><description>
        /// An error occurred during a silent web authentication that prevented the authentication flow from completing in a short enough time frame.
        /// </description></item>
        /// </list>
        /// <para>Remediation:</para>call <c>AcquireTokenInteractive</c> so that the user of your application signs-in and accepts consent.
        /// </summary>
        public const string NoPromptFailedError = "no_prompt_failed";

        /// <summary>
        /// Service is unavailable and returned HTTP error code within the range of 500-599
        /// <para>Mitigation</para> you can retry after a delay.
        /// </summary>
        public const string ServiceNotAvailable = "service_not_available";

        /// <summary>
        /// The HTTP Request to the STS timed out.
        /// <para>Mitigation</para> you can retry after a delay.
        /// </summary>
        public const string RequestTimeout = "request_timeout";

        /// <summary>
        /// loginHint should be a UPN
        /// <para>What happens?</para> An override of a token acquisition operation was called in <see cref="T:IPublicClientApplication"/> which
        /// takes a <c>loginHint</c> as a parameters, but this login hint was not using the UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c>
        /// expected by the service
        /// <para>Remediation</para> Make sure in your code that you enforce <c>loginHint</c> to be a UPN
        /// </summary>
        public const string UpnRequired = "upn_required";

        /// <summary>
        /// No passive auth endpoint was found in the OIDC configuration of the authority
        /// <para>What happens?</para> When the libraries go to the authority and get its open id connect configuration
        /// it expects to find a Passive Auth Endpoint entry, and could not find it.
        /// <para>remediation</para> Check that the authority configured for the application, or passed on some overrides of token acquisition tokens
        /// supporting authority override is correct
        /// </summary>
        public const string MissingPassiveAuthEndpoint = "missing_passive_auth_endpoint";

        /// <summary>
        /// Invalid authority
        /// <para>What happens</para> When the library attempts to discover the authority and get the endpoints it needs to
        /// acquire a token, it got an un-authorize HTTP code or an unexpected response
        /// <para>remediation</para> Check that the authority configured for the application, or passed on some overrides of token acquisition tokens
        /// supporting authority override is correct
        /// </summary>
        public const string InvalidAuthority = "invalid_authority";

        /// <summary>
        /// Invalid authority type.
        /// MSAL.NET does not know how to interact with the authority specified when the application was built.
        /// <para>Mitigation</para>
        /// Use a different authority
        /// </summary>
        public const string InvalidAuthorityType = "invalid_authority_type";

        /// <summary>
        /// Unknown Error occurred.
        /// <para>Mitigation</para> None. You might want to inform the end user.
        /// </summary>
        public const string UnknownError = "unknown_error";

        /// <summary>
        /// Unknown broker error occurred.
        /// <para>Mitigation</para> None. You might want to inform the end user.
        /// </summary>
        public const string UnknownBrokerError = "unknown_broker_error";

        /// <summary>
        /// Authentication failed.
        /// <para>What happens?</para>
        /// The authentication failed. For instance the user did not enter the right password
        /// <para>Mitigation</para>
        /// Inform the user to retry.
        /// </summary>
        public const string AuthenticationFailed = "authentication_failed";

        /// <summary>
        /// Authority validation failed.
        /// <para>What happens?</para>
        /// The validation of the authority failed. This might be because the authority is not
        /// compliant with the OIDC standard, or there might be a security issue
        /// <para>Mitigation</para>
        /// Use a different authority. If you are absolutely sure that you can trust the authority
        /// you can use the <see cref="AbstractApplicationBuilder{T}.WithAuthority(AadAuthorityAudience, bool)"/> passing
        /// the <c>validateAuthority</c> parameter to <c>false</c> (not recommended)
        /// </summary>
        public const string AuthorityValidationFailed = "authority_validation_failed";

        /// <summary>
        /// Invalid owner window type.
        /// <para>What happens?</para>
        /// You used <c>"AcquireTokenInteractiveParameterBuilder.WithParentActivityOrWindow(object)</c>
        /// but the parameter you passed is invalid.
        /// <para>Remediation</para>
        /// On .NET Standard, the expected object is an <c>Activity</c> on Android, a <c>UIViewController</c> on iOS,
        /// a <c>NSWindow</c> on MAC, and a <c>IWin32Window</c> or <c>IntPr</c> on Windows.
        /// If you are in a WPF application, you can use <c>WindowInteropHelper(wpfControl).Handle</c> to get the window
        /// handle associated with a WPF control
        /// </summary>
        public const string InvalidOwnerWindowType = "invalid_owner_window_type";

        /// <summary>
        /// Encoded token too long.
        /// <para>What happens</para>
        /// In a confidential client application call, the client assertion built by MSAL is longer than
        /// the max possible length for a JWT token.
        /// </summary>
        public const string EncodedTokenTooLong = "encoded_token_too_long";

        /// <summary>
        /// User Mismatch.
        /// </summary>
        public const string UserMismatch = "user_mismatch";

        /// <summary>
        /// Failed to refresh token.
        /// <para>What happens?</para>
        /// The token could not be refreshed. This can be because the user has not used the application for a long time.
        /// and therefore the refresh token maintained in the token cache has expired
        /// <para>Mitigation</para>
        /// If you are in a public client application, that supports interactivity, send an interactive request
        /// <see cref="IPublicClientApplication.AcquireTokenInteractive(System.Collections.Generic.IEnumerable{string})"/>. Otherwise,
        /// use a different method to acquire tokens.
        /// </summary>
        public const string FailedToRefreshToken = "failed_to_refresh_token";

        /// <summary>
        /// Failed to acquire token silently. Used in broker scenarios.
        /// <para>What happens</para>
        /// you called <see cref="IClientApplicationBase.AcquireTokenSilent(System.Collections.Generic.IEnumerable{string}, IAccount)"/>
        /// or <see cref="IClientApplicationBase.AcquireTokenSilent(System.Collections.Generic.IEnumerable{string}, string)"/> and your
        /// mobile (Xamarin) application leverages the broker (Microsoft Authenticator or Microsoft Company Portal), but the broker
        /// was not able to acquire the token silently.
        /// <para>Mitigation</para>
        /// Call <see cref="IPublicClientApplication.AcquireTokenInteractive(System.Collections.Generic.IEnumerable{string})"/>
        /// </summary>
        public const string FailedToAcquireTokenSilentlyFromBroker = "failed_to_acquire_token_silently_from_broker";

        /// <summary>
        /// RedirectUri validation failed.
        /// <para>What happens?</para>
        /// The redirect URI / reply URI is invalid
        /// <para>How to fix</para>
        /// Pass a valid redirect URI.
        /// </summary>
        public const string RedirectUriValidationFailed = "redirect_uri_validation_failed";

        /// <summary>
        /// The request could not be preformed because of an unknown failure in the UI flow.*
        /// <para>Mitigation</para>
        /// Inform the user.
        /// </summary>
        public const string AuthenticationUiFailed = "authentication_ui_failed";

        /// <summary>
        /// Internal error
        /// </summary>
        public const string InternalError = "internal_error";

        /// <summary>
        /// Accessing WS Metadata Exchange Failed.
        /// <para>What happens?</para>
        /// You tried to use <see cref="IPublicClientApplication.AcquireTokenByUsernamePassword(System.Collections.Generic.IEnumerable{string}, string, System.Security.SecureString)"/>
        /// and the account is a federated account.
        /// <para>Mitigation</para>
        /// None. The WS metadata was not found or does not correspond to what was expected.
        /// </summary>
        public const string AccessingWsMetadataExchangeFailed = "accessing_ws_metadata_exchange_failed";

        /// <summary>
        /// Federated service returned error.
        /// <para>Mitigation</para>
        /// None. The federated service returned an error. You can try to look at the
        /// Body of the exception for a better understanding of the error and choose
        /// the mitigation
        /// </summary>
        public const string FederatedServiceReturnedError = "federated_service_returned_error";

        /// <summary>
        /// User Realm Discovery Failed.
        /// </summary>
        public const string UserRealmDiscoveryFailed = "user_realm_discovery_failed";

        /// <summary>
        /// Resource Owner Password Credential (ROPC) grant attempted with an MSA (Microsoft personal) account.
        /// ROPC does not support MSA accounts.
        /// See https://aka.ms/msal-net-ropc for details.
        /// </summary>
        public const string RopcDoesNotSupportMsaAccounts = "ropc_not_supported_for_msa";

        /// <summary>
        /// Federation Metadata URL is missing for federated user.
        /// </summary>
        public const string MissingFederationMetadataUrl = "missing_federation_metadata_url";

        /// <summary>
        /// Parsing WS Metadata Exchange Failed.
        /// </summary>
        public const string ParsingWsMetadataExchangeFailed = "parsing_ws_metadata_exchange_failed";

        /// <summary>
        /// WS-Trust Endpoint Not Found in Metadata Document.
        /// </summary>
        public const string WsTrustEndpointNotFoundInMetadataDocument = "wstrust_endpoint_not_found";

        /// <summary>
        /// You can get this error when using <see cref="IPublicClientApplication.AcquireTokenByUsernamePassword(System.Collections.Generic.IEnumerable{string}, string, System.Security.SecureString)"/>
        /// In the case of a Federated user (that is owned by a federated IdP, as opposed to a managed user owned in an Azure AD tenant)
        /// ID3242: The security token could not be authenticated or authorized.
        /// The user does not exist or has entered the wrong password
        /// </summary>
        public const string ParsingWsTrustResponseFailed = "parsing_wstrust_response_failed";

        /// <summary>
        /// <para>What happens</para>
        /// You can get this error when using <see cref="IPublicClientApplication.AcquireTokenByUsernamePassword(System.Collections.Generic.IEnumerable{string}, string, System.Security.SecureString)"/>
        /// The user is not recognized as a managed user, or a federated user. Azure AD was not
        /// able to identify the IdP that needs to process the user
        /// <para>Mitigation</para>
        /// Inform the user. the login that the user provided might be incorrect.
        /// </summary>
        public const string UnknownUserType = "unknown_user_type";

        /// <summary>
        /// <para>What happens</para>
        /// You can get this error when using <see cref="IPublicClientApplication.AcquireTokenByUsernamePassword(System.Collections.Generic.IEnumerable{string}, string, System.Security.SecureString)"/>
        /// The user is not known by the IdP
        /// <para>Mitigation</para>
        /// Inform the user. The login that the user provided might be incorrect (for instance empty)
        /// </summary>
        public const string UnknownUser = "unknown_user";

        /// <summary>
        /// Failed to get user name.
        /// </summary>
        public const string GetUserNameFailed = "get_user_name_failed";

        /// <summary>
        /// Password is required for managed user.
        /// <para>What happens?</para>
        /// If can got this error when using <see cref="IPublicClientApplication.AcquireTokenByUsernamePassword(System.Collections.Generic.IEnumerable{string}, string, System.Security.SecureString)"/>
        /// and you (or the user) did not provide a password.
        /// </summary>
        public const string PasswordRequiredForManagedUserError = "password_required_for_managed_user";

        /// <summary>
        /// Request is invalid.
        /// <para>What happens?</para>
        /// This can happen because you are using a token acquisition method which is not compatible with the authority. For instance:
        /// you called <see cref="IPublicClientApplication.AcquireTokenByUsernamePassword(System.Collections.Generic.IEnumerable{string}, string, System.Security.SecureString)"/>
        /// but you used an authority ending with '/common' or '/consumers' as this requires a tenanted authority or '/organizations'.
        /// <para>Mitigation</para>
        /// Adjust the authority to the AcquireTokenXX method you use (don't use 'common' or 'consumers' with <see cref="IPublicClientApplication.AcquireTokenByUsernamePassword(System.Collections.Generic.IEnumerable{string}, string, System.Security.SecureString)"/>
        /// <see cref="IPublicClientApplication.AcquireTokenByIntegratedWindowsAuth(System.Collections.Generic.IEnumerable{string})"/>
        /// </summary>
        public const string InvalidRequest = "invalid_request";

        /// <summary>
        /// Cannot access the user from the OS (UWP)
        /// <para>What happens</para>
        /// You called <see cref="IPublicClientApplication.AcquireTokenByIntegratedWindowsAuth(System.Collections.Generic.IEnumerable{string})"/>, but the domain user
        /// name could not be found.
        ///<para>Mitigation</para>
        /// This might be because you need to add more capabilities to your UWP application in the Package.appxmanifest.
        /// See https://aka.ms/msal-net-uwp
        /// </summary>
        public const string UapCannotFindDomainUser = "user_information_access_failed";

        /// <summary>
        /// Cannot get the user from the OS (UWP)
        /// <para>What happens</para>
        /// You called <see cref="IPublicClientApplication.AcquireTokenByIntegratedWindowsAuth(System.Collections.Generic.IEnumerable{string})"/>, but the domain user
        /// name could not be found.
        ///<para>Mitigation</para>
        /// This might be because you need to add more capabilities to your UWP application in the Package.appxmanifest.
        /// See https://aka.ms/msal-net-uwp
        /// </summary>
        public const string UapCannotFindUpn = "uap_cannot_find_upn";

        /// <summary>
        /// An error response was returned by the OAuth2 server and it could not be parsed
        /// </summary>
        public const string NonParsableOAuthError = "non_parsable_oauth_error";

        /// <summary>
        /// <para>What happens?</para>
        /// In the context of Device code flow (See https://aka.ms/msal-net-device-code-flow),
        /// this error happens when the device code expired before the user signed-in on another device (this is usually after 15 mins).
        /// <para>Mitigation</para>
        /// None. Inform the user that they took too long to sign-in at the provided URL and enter the provided code.
        /// </summary>
        public const string CodeExpired = "code_expired";

        /// <summary>
        /// Integrated Windows Auth is only supported for "federated" users
        /// </summary>
        public const string IntegratedWindowsAuthNotSupportedForManagedUser = "integrated_windows_auth_not_supported_managed_user";

        /// <summary>
        /// On Android, you need to call <c>AcquireTokenInteractiveParameterBuilder.WithParentActivityOrWindow(object)</c> passing
        /// the activity. See https://aka.ms/msal-interactive-android
        /// </summary>
        public const string ActivityRequired = "activity_required";

        /// <summary>
        /// Broker response hash did not match
        /// </summary>
        public const string BrokerResponseHashMismatch = "broker_response_hash_mismatch";

        /// <summary>
        /// Broker response returned an error
        /// </summary>
        public const string BrokerResponseReturnedError = "broker_response_returned_error";

        /// <summary>
        /// Broker response nonce does not match the request nonce sent by MSAL.NET for iOS broker >= v6.3.19
        /// </summary>
        public const string BrokerNonceMismatch = "broker_nonce_mismatch";

        /// <summary>
        /// MSAL is not able to invoke the broker. Possible reasons are the broker is not installed on the user's device,
        /// or there were issues with the UiParent or CallerViewController being null. See https://aka.ms/msal-brokers
        /// </summary>
        public const string CannotInvokeBroker = "cannot_invoke_broker";

        /// <summary>
        /// MSAL is not able to find a suitable account to use with broker
        /// </summary>
        public const string NoAndroidBrokerAccountFound = "no_broker_account_found";

        /// <summary>
        /// MSAL cannot find the broker application to authenticate accounts.
        /// </summary>
        public const string NoAndroidBrokerInstalledOnDevice = "No_Broker_Installed_On_Device";

        /// <summary>
        /// Null intent was returned so broker activity cannot be launched
        /// </summary>
        public const string NullIntentReturnedFromAndroidBroker = "null_intent_returned_from_broker";

        /// <summary>
        /// Failed to validate android broker signature
        /// </summary>
        public const string AndroidBrokerSignatureVerificationFailed = "broker_signature_verification_failed";

        /// <summary>
        /// Failed To Complete Android Broker Operation
        /// </summary>
        public const string AndroidBrokerOperationFailed = "android_broker_operation_failed";

        /// <summary>
        /// Unable to query the Android account manager because no username or home account ID is provided in the silent authentication request.
        /// </summary>
        public const string NoUsernameOrAccountIDProvidedForSilentAndroidBrokerAuthentication = "no_username_or_accountid_provided_for_silent_android_broker_authentication";

        /// <summary>
        /// Error code used when the HTTP response returns HttpStatusCode.NotFound
        /// </summary>
        public const string HttpStatusNotFound = "not_found";

        /// <summary>
        /// ErrorCode used when the HTTP response returns something different from 200 (OK)
        /// </summary>
        /// <remarks>
        /// HttpStatusCode.NotFound have a specific error code. <see cref="MsalError.HttpStatusNotFound"/>
        /// </remarks>
        public const string HttpStatusCodeNotOk = "http_status_not_200";

        /// <summary>
        /// Error code used when the <see cref="Extensibility.ICustomWebUi"/> has returned a URI, but it is invalid - it is either null or has no code.
        /// Consider throwing an exception if you are unable to intercept the URI containing the code.
        /// </summary>
        public const string CustomWebUiReturnedInvalidUri = "custom_webui_returned_invalid_uri";

        /// <summary>
        /// Error code used when the CustomWebUI has returned a URI, but it does not match the Authority and AbsolutePath of
        /// the configured redirect URI.
        /// </summary>
        public const string CustomWebUiRedirectUriMismatch = "custom_webui_invalid_mismatch";

        /// <summary>
        /// Access denied.
        /// </summary>
        public const string AccessDenied = "access_denied";

        /// <summary>
        /// Cannot Access User Information or the user is not a user domain.
        /// <para>What happens?</para>
        /// You tried to use <see cref="IPublicClientApplication.AcquireTokenByIntegratedWindowsAuth(System.Collections.Generic.IEnumerable{string})"/>
        /// but the user is not a domain user (the machine is not domain or AAD joined)
        /// </summary>
        public const string CannotAccessUserInformationOrUserNotDomainJoined = "user_information_access_failed";

        /// <summary>
        /// RedirectUri validation failed.
        /// </summary>
        public const string DefaultRedirectUriIsInvalid = "redirect_uri_validation_failed";

        /// <summary>
        /// No Redirect URI.
        /// <para>What happens?</para>
        /// You need to provide a Reply URI / Redirect URI, but have not called <see cref="AbstractApplicationBuilder{T}.WithRedirectUri(string)"/>
        /// </summary>
        public const string NoRedirectUri = "no_redirect_uri";

        /// <summary>
        /// Multiple Tokens were matched.
        /// <para>What happens?</para>This exception happens in the case of applications managing several identities,
        /// when calling <see cref="ClientApplicationBase.AcquireTokenSilent(System.Collections.Generic.IEnumerable{string}, IAccount)"/>
        /// or one of its overrides and the user token cache contains multiple tokens for this client application and the specified Account, but from different authorities.
        /// <para>Mitigation [App Development]</para>specify the authority to use in the acquire token operation
        /// </summary>
        public const string MultipleTokensMatchedError = "multiple_matching_tokens_detected";

        /// <summary>
        /// Non HTTPS redirects are not supported
        /// <para>What happens?</para>This error happens when you have registered a non-HTTPS redirect URI for the
        /// public client application other than <c>urn:ietf:wg:oauth:2.0:oob</c>
        /// <para>Mitigation [App registration and development]</para>Register in the application a Reply URL starting with "https://"
        /// </summary>
        public const string NonHttpsRedirectNotSupported = "non_https_redirect_failed";

        /// <summary>
        /// The request could not be preformed because the network is down.
        /// <para>Mitigation [App development]</para> In the application you could either inform the user that there are network issues
        /// or retry later
        /// </summary>
        [Obsolete("MSAL no longer throws this error - it will allow the HttpClient exceptions to propagate. App developers may write their own logic for detecting access to the network issues, for example by using Xamarin.Essentials. ")]
        public const string NetworkNotAvailableError = "network_not_available";

        /// <summary>
        /// The B2C authority host is not the same as the one used when creating the client application.
        /// </summary>
        public const string B2CAuthorityHostMismatch = "B2C_authority_host_mismatch";

        /// <summary>
        /// Duplicate query parameter was found in extraQueryParameters.
        /// <para>What happens?</para> You have used <c>extraQueryParameter</c> of overrides
        /// of token acquisition operations in public client and confidential client application and are passing a parameter which is already present in the
        /// URL (either because you had it in another way, or the library added it).
        /// <para>Mitigation [App Development]</para> RemoveAccount the duplicate parameter from the token acquisition override.
        /// </summary>
        public const string DuplicateQueryParameterError = "duplicate_query_parameter";

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
        /// <para>What happens?</para>A JSON blob read from the token cache or received from the STS was not parseable.
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
        /// <para>What happens?</para>While reading the OpenId configuration associated with the authority, the Authorize endpoint,
        /// or Token endpoint, or the Issuer was not found
        /// <para>Mitigation</para>This indicates and authority which is not Open ID Connect compliant. Specify a different authority
        /// in the constructor of the application, or the token acquisition override
        /// /// </summary>
        public const string TenantDiscoveryFailedError = "tenant_discovery_failed";

        /// <summary>
        /// The library is loaded on a platform which is not supported.
        /// </summary>
        public const string PlatformNotSupported = "platform_not_supported";

        /// <summary>
        /// An authorization Uri has been intercepted, but it cannot be parsed. See the log for more details.
        /// </summary>
        public const string InvalidAuthorizationUri = "invalid_authorization_uri";

        /// <summary>
        /// <para>What happens?</para>The current redirect URL is not a loopback URL.
        /// <para>Mitigation</para> To use the OS browser, a loopback URL, with or without a port, must be configured both during app registration and when initializing the IPublicClientApplication object. See https://aka.ms/msal-net-os-browser for details.
        /// </summary>
        public const string LoopbackRedirectUri = "loopback_redirect_uri";

        /// <summary>
        /// <para>What happens?</para>MSAL has intercepted a Uri possibly containing an authorization code, but it does not match 
        /// the configured redirect URL.
        /// <para>Mitigation</para>If you are using an ICustomWebUi implementation, make sure the
        /// redirect URL matches the URL containing the auth code. If you are not using an ICustomWebUI,
        /// this could be a man-in-the middle attack.
        /// </summary>
        public const string LoopbackResponseUriMismatch = "loopback_response_uri_mismatch";

        /// <summary>
        /// <para>What happens?</para>MSAL tried to open the browser on Linux using the xdg-open, gnome-open, or kfmclient tools, but failed.
        /// <para>Mitigation</para>Make sure you can open a page using xdg-open tool. See https://aka.ms/msal-net-os-browser for details.
        /// </summary>
        public const string LinuxXdgOpen = "linux_xdg_open_failed";

        /// <summary>
        /// The selected WebView is not available on this platform. You can switch to a different WebView using <see cref="AcquireTokenInteractiveParameterBuilder.WithUseEmbeddedWebView(bool)"/>. See https://aka.ms/msal-net-os-browser for details
        /// </summary>
        public const string WebViewUnavailable = "no_system_webview";

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        /// <summary>
        /// <para>What happens?</para>You configured MSAL interactive authentication to use an embedded WebView and you also configured <see cref="SystemWebViewOptions"/>.
        /// These are mutually exclusive.
        /// <para>Mitigation</para>Either set <see cref="AcquireTokenInteractiveParameterBuilder.WithUseEmbeddedWebView(bool)"/> to true or do not use
        /// <see cref="AcquireTokenInteractiveParameterBuilder.WithSystemWebViewOptions(SystemWebViewOptions)"/>
        /// </summary>
        public const string SystemWebviewOptionsNotApplicable = "embedded_webview_not_compatible_default_browser";
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved

        /// <summary>
        /// <para>What happens?</para>You configured MSAL confidential client authentication with more than one authentication type (Certificate, Secret, Client Assertion)
        /// </summary>
        public const string ClientCredentialAuthenticationTypesAreMutuallyExclusive = "Client_Credential_Authentication_Types_Are_Mutually_Exclusive";

        /// <summary>
        /// <para>What happens?</para>You configured MSAL confidential client authentication without an authentication type (Certificate, Secret, Client Assertion)
        /// <para>Mitigation</para>Either call ConfidentialClientApplicationBuilder.WithClientSecret, ConfidentialClientApplicationBuilder.WithCertificate, ConfidentialClientApplicationBuilder.WithClientAssertion
        /// </summary>
        public const string ClientCredentialAuthenticationTypeMustBeDefined = "Client_Credentials_Required_In_Confidential_Client_Application";

        #region InvalidGrant suberrors
        /// <summary>
        /// Issue can be resolved by user interaction during the interactive authentication flow.
        /// See https://aka.ms/msal-net-UiRequiredException for details
        /// </summary>
        internal const string BasicAction = "basic_action";

        /// <summary>
        /// Issue can be resolved by additional remedial interaction with the system, outside of the interactive authentication flow.
        /// Interactive authentication is still required to inform the user of what is needed. Calling application may choose to hide flows that require additional_action if the user is unlikely to complete the remedial action.
        /// Example: 
        /// </summary>
        internal const string AdditionalAction = "additional_action";

        /// <summary>
        /// Issue cannot be resolved at this time. Launching interactive authentication flow will show a message explaining the condition.
        /// </summary>
        internal const string MessageOnly = "message_only";

        /// <summary>
        /// User's password has expired.
        /// </summary>
        internal const string UserPasswordExpired = "user_password_expired";

        /// <summary>
        /// User consent is missing, or has been revoked.
        /// </summary>
        internal const string ConsentRequired = "consent_required";

        /// <summary>
        /// Internal to MSALs. Indicates that no further silent calls should be made with this refresh token.
        /// </summary>
        internal const string BadToken = "bad_token";

        /// <summary>
        /// Internal to MSALs. Indicates that no further silent calls should be made with this refresh token.
        /// </summary>
        internal const string TokenExpired = "token_expired";

        /// <summary>
        /// Internal to MSALs. Needed in ios/android to complete the end-to-end true MAM flow. This suberror code is re-mapped to a different top level error code (IntuneAppProtectionPoliciesRequired), and not InteractionRequired
        /// </summary>
        internal const string ProtectionPolicyRequired = "protection_policy_required";

        /// <summary>
        /// Internal to MSALs. Used in scenarios where an application is using family refresh token even though it is not part of FOCI (or vice versa). Needed to handle cases where app changes FOCI membership after being shipped. This is handled internally and doesn't need to be exposed to the calling app. Please see FOCI design document for more details.
        /// </summary>
        internal const string ClientMismatch = "client_mismatch";

        /// <summary>
        /// Internal to MSALs. Indicates that device should be re-registered.
        /// </summary>
        internal const string DeviceAuthenticationFailed = "device_authentication_failed";

        #endregion

        /// <summary>
        /// AAD service error indicating that the configured authority does not exist
        /// </summary>
        public const string InvalidInstance = "invalid_instance";
        /// <summary>
        /// <para>What happens?</para>You have configured your own instance metadata, but the json provided seems to be invalid. 
        /// <para>Mitigation</para>See https://aka.ms/msal-net-custom-instance-metadata for an example of a valid json that can be used.
        /// </summary>
        public const string InvalidUserInstanceMetadata = "invalid-custom-instance-metadata";

        /// <summary>
        /// <para>What happens?</para>You have configured your own instance metadata, and have also set validate authority to true. These are mutually exclusive. 
        /// <para>Mitigation</para>Set the validate authority flag to false. See https://aka.ms/msal-net-custom-instance-metadata for more details.
        /// </summary>
        public const string ValidateAuthorityOrCustomMetadata = "validate_authority_or_custom_instance_metadata";

        /// <summary>
        /// <para>What happens?</para>You haven't set a client ID.
        /// <para>Mitigation</para>Use the application ID (a GUID) from the application portal as client ID in this SDK
        /// </summary>
        public const string NoClientId = "no_client_id";

        /// <summary>
        /// <para>What happens?</para>You've specified a client ID that is not a <see cref="Guid"/>
        /// <para>Mitigation</para>Use the application ID (a GUID) from the application portal as client ID in this SDK
        /// </summary>
        public const string ClientIdMustBeAGuid = "client_id_must_be_guid";

        /// <summary>
        /// <para>What happens?</para>You have configured both a telemetry callback and a telemetry config. 
        /// <para>Mitigation</para>Only one telemetry mechanism can be configured.
        /// </summary>
        public const string TelemetryConfigOrTelemetryCallback = "telemetry_config_or_telemetry_callback";

        /// <summary>
        /// Azure AD service error indicating a configuration issue. 
        /// <para>Mitigation</para>See error message for details, then take corrective action in the app registration portal
        /// See https://aka.ms/msal-net-invalid-client for details.
        /// </summary>
        public const string InvalidClient = "invalid_client";

        /// <summary>
        /// <para>What happens?</para>You have configured MSAL to request SSH certificates from AAD, and you are trying to format an HTTP authentication header. 
        /// <para>Mitigation</para>SSH certificates should not used as Bearer tokens. Developers are responsible for sending the certificates to the target machines.
        /// </summary>
        public const string SSHCertUsedAsHttpHeader = "ssh_cert_used_as_http_header";

        /// <summary>
        /// <para>What happens?</para>Windows Authentication Broker, which handles the interaction between the user and AAD, has failed.
        /// <para>Mitigation</para>See the error message for more details. 
        /// </summary>
        public const string WABError = "wab_error";

        /// <summary>
        /// <para>What happens?</para>You asked for one type of token, but did not receive it. 
        /// <para>Mitigation</para>This happens if the Identity Provider (AAD, B2C, ADFS etc.) does not support / implement the requested token type. In case of ADFS, consider upgrading to the latest version.
        /// </summary>
        public const string TokenTypeMismatch = "token_type_mismatch";

        /// <summary>
        /// <para>What happens?</para>You are trying to use a feature which was marked as experimental
        /// <para>Mitigation</para>When creating the PublicClientApplication or ConfidentialClientApplication, use .WithExperimentalFeatures() option. See https://aka.ms/msal-net-experimental-features for details.
        /// </summary>
        public const string ExperimentalFeature = "experimental_feature";

        /// <summary>
        /// <para>What happens?</para>A broker application is required, but one could not be found or communicated with.
        /// <para>Mitigation</para>Install a broker application, such as Authenticator, from the application store
        /// </summary>
        public const string BrokerApplicationRequired = "broker_application_required";

        /// <summary>
        /// <para>What happens?</para>You are trying to authenticate with the broker but MSAL is unable to read the response from the broker.
        /// <para>Mitigation</para>The currently installed broker may not support MSAL.Xamarin, you need to ensure that you have installed either Intune Company Portal (5.0.4689.0 or greater) or Microsoft Authenticator (6.2001.0140 or greater). see https://aka.ms/Brokered-Authentication-for-Android"
        /// </summary>
        public const string FailedToGetBrokerResponse = "failed_to_get_broker_response";

        /// <summary>
        /// <para>What happens?</para>You have configured a claims request, but the claims string is not in JSON format
        /// <para>Mitigation</para>Ensure that the claims parameter is valid JSON. Inspect the inner exception for details about JSON parsing. 
        /// </summary>
        public const string InvalidJsonClaimsFormat = "invalid_json_claims_format";

        /// <summary>
        /// <para>What happens?</para>The authority configured at the application level is different than the authority configured at the request level
        /// <para>Mitigation</para>Ensure the same authority type is used
        /// </summary>
        public const string AuthorityTypeMismatch = "authority_type_mismatch";


        /// <summary>
        /// <para>What happens?</para>You have configured your own instance metadata using both an Uri and a string. Only one is supported.
        /// <para>Mitigation</para>Call WithInstanceDiscoveryMetadata only once. See https://aka.ms/msal-net-custom-instance-metadata for more details.
        /// </summary>
        public const string CustomMetadataInstanceOrUri = "custom_metadata_instance_or_uri";

        /// <summary>
        /// <para>What happens?</para>No scopes have been requested
        /// <para>Mitigation</para>At least one scope must be specified for this authentication flow
        /// </summary>
        public const string ScopesRequired = "scopes_required_client_credentials";

        /// <summary>
        /// <para>What happens?</para>The certificate provided does not have a private key.
        /// <para>Mitigation</para>Ensure the certificate has a private key.
        /// </summary>
        public const string CertWithoutPrivateKey = "cert_without_private_key";

        /// <summary>
        /// Device certificate not found.
        /// </summary>
        public const string DeviceCertificateNotFound = "device_certificate_not_found";

        /// <summary>
        /// <para>What happens?</para>The ADAL cache is invalid as it contains multiple refresh token entries for one user.
        /// <para>Mitigation</para>Delete the ADAL cache. If you do not maintain an ADAL cache, this may be a bug in MSAL.
        /// </summary>
        public const string InvalidAdalCacheMultipleRTs = "invalid_adal_cache";

        /// <summary>
        /// Region discovery failed.
        /// </summary>
        public const string RegionDiscoveryFailed = "region_discovery_failed";

        /// <summary>
        /// <para>What happens?</para>WithAzureRegion is set to true
        /// <para>Mitigation</para>Region discovery cannot be performed for ADFS authority.
        /// </summary>
        public const string RegionDiscoveryNotEnabled = "region_discovery_unavailable";

        /// <summary>
        /// <para>What happens?</para>MSAL cannot use the certificate for signing.
        /// <para>Mitigation</para>Possible cause: use of CNG certificates with .Net classic 4.6 or lower. Either target a higher version of .NET desktop - 4.6.1 and above, or use a different certificate type (non-CNG) 
        /// or sign your own assertion as described at https://aka.ms/msal-net-signed-assertion
        /// </summary>
        public const string CryptoNet45 = "crypto_net45";

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

        /// <summary>
        /// Xamarin.iOS + broker specific. This error indicates that the UIViewController is null.
        /// <para>Mitigation</para> In order to access the broker, you will need to pass in a UIViewController.
        /// <para>For more details</para> see https://aka.ms/msal-net-ios-broker
        /// </summary>
        public const string UIViewControllerRequiredForiOSBroker = "uiviewcontroller_required_for_ios_broker";

        /// <summary>
        /// Xamarin.iOS + broker specific. This error indicates that the writing of the application token from iOS broker
        /// to the keychain threw an exception. No SecStatusCode was returned.
        /// <para>Mitigation</para> Check the logs.
        /// <para>For more details</para> see https://aka.ms/msal-net-ios-broker
        /// </summary>
        public const string WritingApplicationTokenToKeychainFailed = "writing_application_token_to_keychain_failed";

        /// <summary>
        /// Xamarin.iOS + broker specific. This error indicates that the reading of the application token from 
        /// the keychain threw an exception. No SecStatusCode was returned.
        /// <para>Mitigation</para> Check the logs.
        /// <para>For more details</para> see https://aka.ms/msal-net-ios-broker
        /// </summary>
        public const string ReadingApplicationTokenFromKeychainFailed = "reading_application_token_from_keychain_failed";
        
        /// <summary>
        /// A broker key was generated but it was not saved to the KeyChain. 
        /// <para>Mitigation</para> Make sure the app has permissions to write to the keychain group 'com.microsoft.adalcache'.
        /// Capture and inspect the logs to see why the save operation failed.
        /// </summary>
        public const string BrokerKeySaveFailed = "ios_broker_key_save_failed";

        /// <summary>
        /// A broker key was generated but it was not retrieved from the KeyChain. 
        /// <para>Mitigation</para> Make sure the app has permissions to write to the keychain group 'com.microsoft.adalcache'.
        /// Capture and inspect the logs to see why the fetch operation failed.
        /// </summary>
        public const string BrokerKeyFetchFailed = "ios_broker_key_fetch_failed";
#endif

#if ANDROID
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
        /// When calling AcquireTokenInteractive with the WAM broker, the call must be made from the UI thread.
        /// </summary>
        public const string WamUiThread = "wam_ui_thread_only";

        /// <summary>
        /// The Windows broker (WAM) is only supported in conjunction  with "work and school" accounts 
        /// and with Microsoft accounts.
        /// </summary>
        public const string WamNoB2C = "wam_no_b2c";

        /// <summary>
        /// WAM service exception that can occur when calling AcquireTokenInteractive
        /// </summary>
        public const string WamInteractiveError = "wam_interactive_error";

        /// <summary>
        /// WAM service exception that can occur when calling AcquireTokenInteractive and the account picker 
        /// pops up
        /// </summary>
        public const string WamPickerError = "wam_interactive_picker_error";

        /// <summary>
        /// <para>What happens?</para>The embedded browser cannot be started because a runtime component is missing.
        /// <para>Mitigation</para>"The embedded browser needs WebView2 runtime to be installed. An end user of the app can download and install the WebView2 runtime from https://go.microsoft.com/fwlink/p/?LinkId=2124703 and restart the app.
        ///  or the app developer can install the WebView2 runtime https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution
        /// </summary>
        public const string WebView2NotInstalled = "webview2_runtime_not_installed";


        /// <summary>
        /// <para>What happens?</para>You configured both Regional Authority and Authority Validation. Authority validation is not currently supported for regional authorities.
        /// <para>Mitigation</para>Set the validateAuthority flag to false to use Azure Regional authority. Do not disable authority validation if you read the authority from an untrusted source, 
        /// for example from the WWWAuthenticate header of an HTTP request that resulted in a 401 response. 
        ///  </summary>
        public const string RegionalAuthorityValidation = "regional_authority_validation";

    }
}

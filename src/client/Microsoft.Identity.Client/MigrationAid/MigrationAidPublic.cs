// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;

#if ANDROID
using Android.App;
#endif

namespace Microsoft.Identity.Client
{
    public partial interface IPublicClientApplication
    {
#if WINDOWS_APP
        /// <summary>
        /// Flag to enable authentication with the user currently signed-in on Windows.
        /// When set to true, the application will try to connect to the corporate network using Windows Integrated Authentication.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PublicClientApplication is now immutable, you can set this property using the PublicClientApplicationBuilder and read it using IAppConfig.  See https://aka.ms/msal-net-3-breaking-changes and https://aka.ms/msal-net-application-configuration", true)]
        bool UseCorporateNetwork { get; set; }
#endif // WINDOWS_APP

        #region MSAL3X deprecations

        // expose the interactive API without UIParent only for platforms that
        // do not need it to operate like desktop, UWP, iOS.

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user is required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.</remarks>
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes);

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the <paramref name="loginHint"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint);

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the provided <paramref name="account"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account);

        /// <summary>
        /// Interactive request to acquire token for a login with control of the UI behavior and possibility of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            Prompt prompt,
            string extraQueryParameters);

        /// <summary>
        /// Interactive request to acquire token for an account with control of the UI behavior and possibility of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            Prompt prompt,
            string extraQueryParameters);

        /// <summary>
        /// Interactive request to acquire token for a given login, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">scopes that you can request the end user to consent upfront, in addition to the scopes for the protected web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            Prompt prompt,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent, string authority);

        /// <summary>
        /// Interactive request to acquire token for a given account, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Scopes that you can request the end user to consent upfront, in addition to the scopes for the protected web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            Prompt prompt,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent,
            string authority);

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The interactive window will be parented to the specified
        /// window. The user will be required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.</remarks>
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The interactive window will be parented to the specified
        /// window. . The user will need to sign-in but an account will be proposed
        /// based on the <paramref name="loginHint"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and login</returns>
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the provided <paramref name="account"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token for a login with control of the UI behavior and possibility of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            Prompt prompt,
            string extraQueryParameters, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token for an account with control of the UI behavior and possibility of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            Prompt prompt,
            string extraQueryParameters, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token for a given login, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Scopes that you can request the end user to consent upfront, in addition to the scopes for the protected web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            Prompt prompt,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent, string authority, UIParent parent);

        /// <summary>
        /// Interactive request to acquire token for a given account, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Scopes that you can request the end user to consent upfront, in addition to the scopes for the protected web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            Prompt prompt,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent,
            string authority, UIParent parent);

        /// <summary>
        /// Non-interactive request to acquire a security token from the authority, via Username/Password Authentication.
        /// See https://aka.ms/msal-net-up.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user application requests token on behalf.
        /// Generally in UserPrincipalName (UPN) format, e.g. john.doe@contoso.com</param>
        /// <param name="securePassword">User password.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [Obsolete("Use AcquireTokenByUsernamePassword instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]

        Task<AuthenticationResult> AcquireTokenByUsernamePasswordAsync(
            IEnumerable<string> scopes,
            string username,
            System.Security.SecureString securePassword);

        /// <summary>
        /// Acquires a security token on a device without a web browser, by letting the user authenticate on
        /// another device. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>

        [Obsolete("Use AcquireTokenWithDeviceCode instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback);

        /// <summary>
        /// Acquires a security token on a device without a web browser, by letting the user authenticate on
        /// another device, with possibility of passing extra parameters. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>

        [Obsolete("Use AcquireTokenWithDeviceCode instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            string extraQueryParameters,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback);

        /// <summary>
        /// Acquires a security token on a device without a web browser, by letting the user authenticate on
        /// another device, with possibility of cancelling the token acquisition before it times out. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information. This step is cancelable</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <param name="cancellationToken">A CancellationToken which can be triggered to cancel the operation in progress.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenWithDeviceCode instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback,
            CancellationToken cancellationToken);

        /// <summary>
        /// Acquires a security token on a device without a web browser, by letting the user authenticate on
        /// another device, with possibility of passing extra query parameters and cancelling the token acquisition before it times out. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information. This step is cancelable</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <param name="cancellationToken">A CancellationToken which can be triggered to cancel the operation in progress.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenWithDeviceCode instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            string extraQueryParameters,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback,
            CancellationToken cancellationToken);

        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows, via Integrated Windows Authentication.
        /// See https://aka.ms/msal-net-iwa.
        /// The account used in this overrides is pulled from the operating system as the current user principal name
        /// </summary>
        /// <remarks>
        /// On Windows Universal Platform, the following capabilities need to be provided:
        /// Enterprise Authentication, Private Networks (Client and Server), User Account Information
        /// </remarks>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the currently signed-in user in Windows</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenByIntegratedWindowsAuth instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenByIntegratedWindowsAuthAsync(IEnumerable<string> scopes);

        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows, via Integrated Windows Authentication.
        /// See https://aka.ms/msal-net-iwa.
        /// The account used in this overrides is pulled from the operating system as the current user principal name
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user account for which to acquire a token with Integrated Windows authentication.
        /// Generally in UserPrincipalName (UPN) format, e.g. john.doe@contoso.com</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the currently signed-in user in Windows</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenByIntegratedWindowsAuth instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenByIntegratedWindowsAuthAsync(
            IEnumerable<string> scopes,
            string username);

        #endregion MSAL3X deprecations
    }

    /// <summary>
    /// Abstract class containing common API methods and properties.
    /// For details see https://aka.ms/msal-net-client-applications
    /// </summary>
    public partial class PublicClientApplication
    {
#if WINDOWS_APP
        /// <summary>
        /// Flag to enable authentication with the user currently signed-in on Windows.
        /// When set to true, the application will try to connect to the corporate network using Windows Integrated Authentication.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PublicClientApplication is now immutable, you can set this property using the PublicClientApplicationBuilder and read it using IAppConfig.  See https://aka.ms/msal-net-3-breaking-changes and https://aka.ms/msal-net-application-configuration", true)]
        public bool UseCorporateNetwork { get; set; }
#endif

#if iOS
        /// <summary>
        /// Xamarin iOS specific property enabling the application to share the token cache with other applications sharing the same keychain security group.
        /// If you use this property, you MUST add the capability to your Application Entitlement.
        /// When using this property, the value must contain the TeamId prefix, which is why this is now obsolete.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use iOSKeychainSecurityGroup instead (See https://aka.ms/msal-net-ios-keychain-security-group)", true)]
        public string KeychainSecurityGroup { get { throw new NotImplementedException(); } }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("See https://aka.ms/msal-net-3-breaking-changes and https://aka.ms/msal-net-application-configuration", true)]
        public string iOSKeychainSecurityGroup
        {
            get => throw new NotImplementedException("See https://aka.ms/msal-net-3-breaking-changes and https://aka.ms/msal-net-application-configuration");
            set => throw new NotImplementedException("See https://aka.ms/msal-net-3-breaking-changes and https://aka.ms/msal-net-application-configuration");
        }
#endif

        #region MSAL3X deprecations

        /// <summary>
        /// Constructor of the application. It will use https://login.microsoftonline.com/common as the default authority.
        /// </summary>
        /// <param name="clientId">Client ID (also known as App ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)/. REQUIRED</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use PublicClientApplicationBuilder instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public PublicClientApplication(string clientId) : this(clientId, DefaultAuthority)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Constructor of the application.
        /// </summary>
        /// <param name="clientId">Client ID (also named Application ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)/. REQUIRED</param>
        /// <param name="authority">Authority of the security token service (STS) from which MSAL.NET will acquire the tokens.
        /// Usual authorities are:
        /// <list type="bullet">
        /// <item><description><c>https://login.microsoftonline.com/tenant/</c>, where <c>tenant</c> is the tenant ID of the Azure AD tenant
        /// or a domain associated with this Azure AD tenant, in order to sign-in user of a specific organization only</description></item>
        /// <item><description><c>https://login.microsoftonline.com/common/</c> to signing users with any work and school accounts or Microsoft personal account</description></item>
        /// <item><description><c>https://login.microsoftonline.com/organizations/</c> to signing users with any work and school accounts</description></item>
        /// <item><description><c>https://login.microsoftonline.com/consumers/</c> to signing users with only personal Microsoft account (live)</description></item>
        /// </list>
        /// Note that this setting needs to be consistent with what is declared in the application registration portal
        /// </param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use PublicClientApplicationBuilder instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public PublicClientApplication(string clientId, string authority)
            : base(PublicClientApplicationBuilder
                .Create(clientId)
                .WithRedirectUri(((IPlatformProxyPublic)new PlatformProxyFactoryPublic().CreatePlatformProxy(null)).GetDefaultRedirectUri(clientId))
                .WithAuthority(new Uri(authority), true)
                .BuildConfiguration())
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user is required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.</remarks>
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the <paramref name="loginHint"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the provided <paramref name="account"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for a login with control of the UI prompt and possibility of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            Prompt prompt,
            string extraQueryParameters)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for an account with control of the UI prompt and possibility of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            Prompt prompt,
            string extraQueryParameters)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for a given login, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Scopes that you can request the end user to consent upfront, in addition to the scopes for the protected web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            Prompt prompt,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent,
            string authority)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for a given account, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Scopes that you can request the end user to consent upfront, in addition to the scopes for the protected web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            Prompt prompt,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent,
            string authority)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The interactive window will be parented to the specified
        /// window. The user will be required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, UIParent parent)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The interactive window will be parented to the specified
        /// window. The user will need to sign-in but an account will be proposed
        /// based on the <paramref name="loginHint"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and login</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint, UIParent parent)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the provided <paramref name="account"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account, UIParent parent)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for a login with control of the UI prompt and possibility of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            Prompt prompt,
            string extraQueryParameters,
            UIParent parent)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for an account with control of the UI prompt and possibility of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            Prompt prompt,
            string extraQueryParameters,
            UIParent parent)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for a given login, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">scopes that you can request the end user to consent upfront, in addition to the scopes for the protected web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges)]
        public Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            string loginHint,
            Prompt prompt,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent,
            string authority,
            UIParent parent)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Interactive request to acquire token for a given account, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="prompt">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">scopes that you can request the end user to consent upfront, in addition to the scopes for the protected web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenInteractive instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account,
            Prompt prompt,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent,
            string authority,
            UIParent parent)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Non-interactive request to acquire a security token from the authority, via Username/Password Authentication.
        /// Available only on .net desktop and .net core. See https://aka.ms/msal-net-up for details.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user application requests token on behalf.
        /// Generally in UserPrincipalName (UPN) format, e.g. john.doe@contoso.com</param>
        /// <param name="securePassword">User password.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [Obsolete("Use AcquireTokenByUsernamePassword instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<AuthenticationResult> AcquireTokenByUsernamePasswordAsync(IEnumerable<string> scopes, string username, SecureString securePassword)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Acquires a security token on a device without a web browser, by letting the user authenticate on
        /// another device. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenWithDeviceCode instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Acquires a security token on a device without a web browser, by letting the user authenticate on
        /// another device, with possibility of passing extra parameters. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenWithDeviceCode instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            string extraQueryParameters,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Acquires a security token on a device without a web browser, by letting the user authenticate on
        /// another device, with possibility of cancelling the token acquisition before it times out. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information. This step is cancelable</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <param name="cancellationToken">A CancellationToken which can be triggered to cancel the operation in progress.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenWithDeviceCode instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback,
            CancellationToken cancellationToken)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Acquires a security token on a device without a web browser, by letting the user authenticate on
        /// another device, with possibility of passing extra query parameters and cancelling the token acquisition before it times out. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information. This step is cancelable</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <param name="cancellationToken">A CancellationToken which can be triggered to cancel the operation in progress.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenWithDeviceCode instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            string extraQueryParameters,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback,
            CancellationToken cancellationToken)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Acquires an access token from an existing refresh token and stores it and the refresh token into
        /// the application user token cache, where it will be available for further AcquireTokenSilentAsync calls.
        /// This method can be used in migration to MSAL from ADAL v2 and in various integration
        /// scenarios where you have a RefreshToken available.
        /// (see https://aka.ms/msal-net-migration-adal2-msal2)
        /// </summary>
        /// <param name="scopes">Scope to request from the token endpoint.
        /// Setting this to null or empty will request an access token, refresh token and ID token with default scopes</param>
        /// <param name="refreshToken">The refresh token (for example previously obtained from ADAL 2.x)</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenByRefreshToken instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> IByRefreshToken.AcquireTokenByRefreshTokenAsync(IEnumerable<string> scopes, string refreshToken)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows, via Integrated Windows Authentication.
        /// See https://aka.ms/msal-net-iwa.
        /// The account used in this overrides is pulled from the operating system as the current user principal name
        /// </summary>
        /// <remarks>
        /// On Windows Universal Platform, the following capabilities need to be provided:
        /// Enterprise Authentication, Private Networks (Client and Server), User Account Information
        /// Supported on .net desktop and UWP
        /// </remarks>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the currently signed-in user in Windows</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenByIntegratedWindowsAuth instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenByIntegratedWindowsAuthAsync(IEnumerable<string> scopes)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows, via Integrated Windows Authentication.
        /// See https://aka.ms/msal-net-iwa.
        /// The account used in this overrides is pulled from the operating system as the current user principal name
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user account for which to acquire a token with Integrated Windows authentication.
        /// Generally in UserPrincipalName (UPN) format, e.g. john.doe@contoso.com</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the currently signed-in user in Windows</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenByIntegratedWindowsAuth instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenByIntegratedWindowsAuthAsync(
            IEnumerable<string> scopes,
            string username)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Constructor to create application instance. This constructor is only available for Desktop and NetCore apps
        /// </summary>
        /// <param name="clientId">Client id of the application</param>
        /// <param name="authority">Default authority to be used for the application</param>
        /// <param name="userTokenCache">Instance of TokenCache.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use PublicClientApplicationBuilder instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public PublicClientApplication(string clientId, string authority, TokenCache userTokenCache)
            : this(PublicClientApplicationBuilder
                   .Create(clientId)
                   .WithAuthority(new Uri(authority), true)
                   .BuildConfiguration())
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        #endregion MSAL3X deprecations
    }

    /// <summary>
    /// Interface defining common API methods and properties.
    /// For details see https://aka.ms/msal-net-client-applications
    /// </summary>
    public partial interface IPublicClientApplication
    {
#if iOS
        /// <summary>
        /// Xamarin iOS specific property enabling the application to share the token cache with other applications sharing the same keychain security group.
        /// If you use this property, you MUST add the capability to your Application Entitlement.
        /// When using this property, the value must contain the TeamId prefix, which is why this is now obsolete.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use iOSKeychainSecurityGroup instead (See https://aka.ms/msal-net-ios-keychain-security-group)", true)]
        string KeychainSecurityGroup { get; }

        /// <summary>
        /// Xamarin iOS specific property enabling the application to share the token cache with other applications sharing the same keychain security group.
        /// If you use this property, you MUST add the capability to your Application Entitlement.
        /// In this property, the value should not contain the TeamId prefix, MSAL will resolve the TeamId at runtime.
        /// For more details, please see https://aka.ms/msal-net-sharing-cache-on-ios
        /// </summary>
        /// <remarks>This API may change in future release.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("See https://aka.ms/msal-net-3-breaking-changes and https://aka.ms/msal-net-application-configuration", true)]
        string iOSKeychainSecurityGroup { get; set; }
#endif
    }

    /// <summary>
    /// Structure containing static members that you can use to specify how the interactive overrides
    /// of AcquireTokenAsync in <see cref="PublicClientApplication"/> should prompt the user.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("UIBehavior struct is now obsolete.  Please use Prompt struct instead." + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
    public struct UIBehavior
    {
    }

    /// <summary>
    ///
    /// </summary>
    [Obsolete("In MSAL.NET 3.x, you should directly pass the Activity (on Xamarin.Android), or Window (on .NET Framework and UWP) using AcquireTokenInteractiveParameterBuilder.WithParentActivityOrWindow" + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class UIParent
    {
        /// <summary>
        /// </summary>
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UIParent() // do not delete this ctor because it exists on NetStandard
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
        }

        /// <summary>
        /// </summary>
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UIParent(object parent, bool useEmbeddedWebView)
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
        }

        /// <summary>
        /// Checks Android device for chrome packages.
        /// Returns true if chrome package for launching system webview is enabled on device.
        /// Returns false if chrome package is not found.
        /// </summary>
        /// <example>
        /// The following code decides, in a Xamarin.Forms app, which browser to use based on the presence of the
        /// required packages.
        /// <code>
        /// bool useSystemBrowser = UIParent.IsSystemWebviewAvailable();
        /// App.UIParent = new UIParent(Xamarin.Forms.Forms.Context as Activity, !useSystemBrowser);
        /// </code>
        /// </example>
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool IsSystemWebviewAvailable()
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
        }

#if ANDROID
        /// <summary>
        /// Initializes an instance for a provided activity.
        /// </summary>
        /// <param name="activity">parent activity for the call. REQUIRED.</param>
        [CLSCompliant(false)]
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UIParent(Android.App.Activity activity)
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
        }

        /// <summary>
        /// Initializes an instance for a provided activity with flag directing the application
        /// to use the embedded webview instead of the system browser. See https://aka.ms/msal-net-uses-web-browser
        /// </summary>
        [CLSCompliant(false)]
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UIParent(Android.App.Activity activity, bool useEmbeddedWebview) : this(activity)
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
        }
#endif
    }
}

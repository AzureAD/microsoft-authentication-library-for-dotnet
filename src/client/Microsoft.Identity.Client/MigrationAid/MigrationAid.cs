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
    internal static class MigrationHelper
    {
        public static NotImplementedException CreateMsalNet3BreakingChangesException()
        {
            return new NotImplementedException("See https://aka.ms/msal-net-3-breaking-changes");
        }
    }

    /// <summary>
    /// In MSAL.NET 1.x, was representing a User. From MSAL 2.x use <see cref="IAccount"/> which represents an account
    /// (a user has several accounts). See https://aka.ms/msal-net-2-released for more details.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use IAccount instead (See https://aka.ms/msal-net-2-released)", true)]
    public interface IUser
    {
        /// <summary>
        /// In MSAL.NET 1.x was the displayable ID of a user. From MSAL 2.x use the <see cref="IAccount.Username"/> of an account.
        /// See https://aka.ms/msal-net-2-released for more details
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use IAccount.Username instead (See https://aka.ms/msal-net-2-released)", true)]
        string DisplayableId { get; }

        /// <summary>
        /// In MSAL.NET 1.x was the name of the user (which was not very useful as the concatenation of
        /// some claims). From MSAL 2.x rather use <see cref="IAccount.Username"/>. See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use IAccount.Username instead (See https://aka.ms/msal-net-2-released)", true)]
        string Name { get; }

        /// <summary>
        /// In MSAL.NET 1.x was the URL of the identity provider (e.g. https://login.microsoftonline.com/tenantId).
        /// From MSAL.NET 2.x use <see cref="IAccount.Environment"/> which retrieves the host only (e.g. login.microsoftonline.com).
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use IAccount.Environment instead to get the Identity Provider host (See https://aka.ms/msal-net-2-released)", true)]
        string IdentityProvider { get; }

        /// <summary>
        /// In MSAL.NET 1.x was an identifier for the user in the guest tenant.
        /// From MSAL.NET 2.x, use <see cref="IAccount.HomeAccountId"/><see cref="AccountId.Identifier"/> to get
        /// the user identifier (globally unique across tenants). See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use IAccount.HomeAccountId.Identifier instead to get the user identifier (See https://aka.ms/msal-net-2-released)", true)]
        string Identifier { get; }
    }

    /// <summary>
    /// Interface defining common API methods and properties. Both <see cref="T:PublicClientApplication"/> and <see cref="T:ConfidentialClientApplication"/>
    /// extend this class. For details see https://aka.ms/msal-net-client-applications
    /// </summary>    
    public partial interface IClientApplicationBase
    {
        /// <summary>
        /// In MSAL 1.x returned an enumeration of <see cref="IUser"/>. From MSAL 2.x, use <see cref="GetAccountsAsync()"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use GetAccountsAsync instead (See https://aka.ms/msal-net-2-released)", true)]
        IEnumerable<IUser> Users { get; }

        /// <summary>
        /// In MSAL 1.x, return a user from its identifier. From MSAL 2.x, use <see cref="GetAccountsAsync()"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        /// <param name="identifier">Identifier of the user to retrieve</param>
        /// <returns>the user in the cache with the identifier passed as an argument</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use GetAccountAsync instead and pass IAccount.HomeAccountId.Identifier (See https://aka.ms/msal-net-2-released)", true)]
        IUser GetUser(string identifier);

        /// <summary>
        /// In MSAL 1.x removed a user from the cache. From MSAL 2.x, use <see cref="RemoveAsync(IAccount)"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        /// <param name="user">User to remove from the cache</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use RemoveAccountAsync instead (See https://aka.ms/msal-net-2-released)", true)]
        void Remove(IUser user);

        /// <summary>
        /// Identifier of the component (libraries/SDK) consuming MSAL.NET.
        /// This will allow for disambiguation between MSAL usage by the app vs MSAL usage by component libraries.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use WithComponent on AbstractApplicationBuilder<T> to configure this instead.  See https://aka.ms/msal-net-3-breaking-changes or https://aka.ms/msal-net-application-configuration", true)]
        string Component { get; set; }

        /// <summary>
        /// Sets or Gets a custom query parameters that may be sent to the STS for dogfood testing or debugging. This is a string of segments
        /// of the form <c>key=value</c> separated by an ampersand character.
        /// Unless requested otherwise, this parameter should not be set by application developers as it may have adverse effect on the application.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use ExtraQueryParameters on each call instead.  See https://aka.ms/msal-net-3-breaking-changes or https://aka.ms/msal-net-application-configuration", true)]
        string SliceParameters { get; set; }

        /// <summary>
        /// Gets a boolean value telling the application if the authority needs to be verified against a list of known authorities. The default
        /// value is <c>true</c>. It should currently be set to <c>false</c> for Azure AD B2C authorities as those are customer specific
        /// (a list of known B2C authorities cannot be maintained by MSAL.NET)
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Can be set on AbstractApplicationBuilder<T>.WithAuthority as needed.  See https://aka.ms/msal-net-3-breaking-changes or https://aka.ms/msal-net-application-configuration", true)]
        bool ValidateAuthority { get; }

        /// <summary>
        /// The redirect URI (also known as Reply URI or Reply URL), is the URI at which Azure AD will contact back the application with the tokens.
        /// This redirect URI needs to be registered in the app registration (https://aka.ms/msal-net-register-app)
        /// In MSAL.NET, <see cref="T:PublicClientApplication"/> define the following default RedirectUri values:
        /// <list type="bullet">
        /// <item><description><c>urn:ietf:wg:oauth:2.0:oob</c> for desktop (.NET Framework and .NET Core) applications</description></item>
        /// <item><description><c>msal{ClientId}</c> for Xamarin iOS and Xamarin Android (as this will be used by the system web browser by default on these
        /// platforms to call back the application)
        /// </description></item>
        /// </list>
        /// These default URIs could change in the future.
        /// In <see cref="Microsoft.Identity.Client.ConfidentialClientApplication"/>, this can be the URL of the web application / web API.
        /// </summary>
        /// <remarks>This is especially important when you deploy an application that you have initially tested locally;
        /// you then need to add the reply URL of the deployed application in the application registration portal.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Should be set using AbstractApplicationBuilder<T>.WithRedirectUri and can be viewed with ClientApplicationBase.AppConfig.RedirectUri. See https://aka.ms/msal-net-3-breaking-changes or https://aka.ms/msal-net-application-configuration", true)]
        string RedirectUri { get; set; }

        #region MSAL3X deprecations
        /// <summary>
        /// Attempts to acquire an access token for the <paramref name="account"/> from the user token cache.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account for which the token is requested. <see cref="IAccount"/></param>
        /// <returns>An <see cref="AuthenticationResult"/> containing the requested token</returns>
        /// <exception cref="MsalUiRequiredException">can be thrown in the case where an interaction is required with the end user of the application,
        /// for instance so that the user consents, or re-signs-in (for instance if the password expired), or performs two factor authentication</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If the access token is expired or
        /// close to expiration (within 5 minute window), then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        /// See https://aka.ms/msal-net-acuiretokensilent for more details
        /// </remarks>
        [Obsolete("Use AcquireTokenSilent instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<AuthenticationResult> AcquireTokenSilentAsync(
            IEnumerable<string> scopes,
            IAccount account);

        /// <summary>
        /// Attempts to acquire and access token for the <paramref name="account"/> from the user token cache, with advanced parameters making a network call.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account for which the token is requested. <see cref="IAccount"/></param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured in the application constructor
        /// narrows down the selection of tenants for which to get a tenant, but does not change the configured value</param>
        /// <param name="forceRefresh">If <c>true</c>, the will ignore the access token in the cache and attempt to acquire new access token
        /// using the refresh token for the account if this one is available. This can be useful in the case when the application developer wants to make
        /// sure that conditional access policies are applies immediately, rather than after the expiration of the access token</param>
        /// <returns>An <see cref="AuthenticationResult"/> containing the requested token</returns>
        /// <exception cref="MsalUiRequiredException">can be thrown in the case where an interaction is required with the end user of the application,
        /// for instance, if no refresh token was in the cache, or the user needs to consents, or re-sign-in (for instance if the password expired),
        /// or performs two factor authentication</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes. This means that an access token with more scopes than
        /// requested could be returned as well. If the access token is expired or close to expiration (within 5 minute window),
        /// then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        /// See https://aka.ms/msal-net-acquiretokensilent for more details
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenSilent instead." + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenSilentAsync(
            IEnumerable<string> scopes,
            IAccount account,
            string authority,
            bool forceRefresh);

        /// <summary>
        /// Gets the Client ID (also known as Application ID) of the application as registered in the application registration portal (https://aka.ms/msal-net-register-app)
        /// and as passed in the constructor of the application.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AppConfig.ClientId instead." + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        string ClientId { get; }

        #endregion MSAL3X deprecations
    }

    /// <inheritdoc/>
    public partial class ClientApplicationBase
    {
        /// <summary>
        /// In MSAL 1.x returned an enumeration of <see cref="IUser"/>. From MSAL 2.x, use <see cref="GetAccountsAsync()"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use GetAccountsAsync instead (See https://aka.ms/msal-net-2-released)", true)]
        public IEnumerable<IUser> Users { get { throw new NotImplementedException(); } }

        /// <summary>
        /// In MSAL 1.x, return a user from its identifier. From MSAL 2.x, use <see cref="GetAccountsAsync()"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        /// <param name="identifier">Identifier of the user to retrieve</param>
        /// <returns>the user in the cache with the identifier passed as an argument</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use GetAccountAsync instead and pass IAccount.HomeAccountId.Identifier (See https://aka.ms/msal-net-2-released)", true)]
        public IUser GetUser(string identifier) { throw new NotImplementedException(); }

        /// <summary>
        /// In MSAL 1.x removed a user from the cache. From MSAL 2.x, use <see cref="RemoveAsync(IAccount)"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        /// <param name="user">User to remove from the cache</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use RemoveAccountAsync instead (See https://aka.ms/msal-net-2-released)", true)]
        public void Remove(IUser user) { throw new NotImplementedException(); }

        /// <summary>
        /// Identifier of the component (libraries/SDK) consuming MSAL.NET.
        /// This will allow for disambiguation between MSAL usage by the app vs MSAL usage by component libraries.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use WithComponent on AbstractApplicationBuilder<T> to configure this instead." + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public string Component { get; set; }

        /// <summary>
        /// Sets or Gets a custom query parameters that may be sent to the STS for dogfood testing or debugging. This is a string of segments
        /// of the form <c>key=value</c> separated by an ampersand character.
        /// Unless requested otherwise, this parameter should not be set by application developers as it may have adverse effect on the application.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use ExtraQueryParameters on each call instead." + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public string SliceParameters { get; set; }

        /// <summary>
        /// Gets/sets a boolean value telling the application if the authority needs to be verified against a list of known authorities. The default
        /// value is <c>true</c>. It should currently be set to <c>false</c> for Azure AD B2C authorities as those are customer specific
        /// (a list of known B2C authorities cannot be maintained by MSAL.NET). This property can be set just after the construction of the application
        /// and before an operation acquiring a token or interacting with the STS.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Can be set on AbstractApplicationBuilder<T>.WithAuthority as needed." + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public bool ValidateAuthority { get; set; }

        /// <summary>
        /// The redirect URI (also known as Reply URI or Reply URL), is the URI at which Azure AD will contact back the application with the tokens.
        /// This redirect URI needs to be registered in the app registration (https://aka.ms/msal-net-register-app).
        /// In MSAL.NET, <see cref="T:PublicClientApplication"/> define the following default RedirectUri values:
        /// <list type="bullet">
        /// <item><description><c>https://login.microsoftonline.com/common/oauth2/nativeclient</c> for desktop (.NET Framework and .NET Core) applications</description></item>
        /// <item><description><c>msal{ClientId}</c> for Xamarin iOS and Xamarin Android (as this will be used by the system web browser by default on these
        /// platforms to call back the application)
        /// </description></item>
        /// </list>
        /// These default URIs could change in the future.
        /// In Microsoft.Identity.Client.ConfidentialClientApplication, this can be the URL of the web application / web API.
        /// </summary>
        /// <remarks>This is especially important when you deploy an application that you have initially tested locally;
        /// you then need to add the reply URL of the deployed application in the application registration portal</remarks>
        [Obsolete("Should be set using AbstractApplicationBuilder<T>.WithRedirectUri and can be viewed with ClientApplicationBase.AppConfig.RedirectUri." + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string RedirectUri { get; set; }

        #region MSAL3X deprecations

        /// <summary>
        /// Gets the Client ID (also known as <i>Application ID</i>) of the application as registered in the application registration portal (https://aka.ms/msal-net-register-app)
        /// and as passed in the constructor of the application
        /// </summary>
        [Obsolete("Use AppConfig.ClientId instead." + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string ClientId => AppConfig.ClientId;

        /// <summary>
        /// [V2 API] Attempts to acquire an access token for the <paramref name="account"/> from the user token cache, with advanced parameters controlling network call.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account for which the token is requested. <see cref="IAccount"/></param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured in the application constructor
        /// narrows down the selection to a specific tenant. This does not change the configured value in the application. This is specific
        /// to applications managing several accounts (like a mail client with several mailboxes)</param>
        /// <param name="forceRefresh">If <c>true</c>, ignore any access token in the cache and attempt to acquire new access token
        /// using the refresh token for the account if this one is available. This can be useful in the case when the application developer wants to make
        /// sure that conditional access policies are applied immediately, rather than after the expiration of the access token</param>
        /// <returns>An <see cref="AuthenticationResult"/> containing the requested access token</returns>
        /// <exception cref="MsalUiRequiredException">can be thrown in the case where an interaction is required with the end user of the application,
        /// for instance, if no refresh token was in the cache,a or the user needs to consent, or re-sign-in (for instance if the password expired),
        /// or performs two factor authentication</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes. This means that an access token with more scopes than
        /// requested could be returned as well. If the access token is expired or close to expiration (within a 5 minute window),
        /// then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        ///
        /// See https://aka.ms/msal-net-acquiretokensilent for more details
        /// </remarks>
        [Obsolete("Use AcquireTokenSilent instead." + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<AuthenticationResult> AcquireTokenSilentAsync(
            IEnumerable<string> scopes,
            IAccount account,
            string authority, bool forceRefresh)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// [V2 API] Attempts to acquire an access token for the <paramref name="account"/> from the user token cache.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account for which the token is requested. <see cref="IAccount"/></param>
        /// <returns>An <see cref="AuthenticationResult"/> containing the requested token</returns>
        /// <exception cref="MsalUiRequiredException">can be thrown in the case where an interaction is required with the end user of the application,
        /// for instance so that the user consents, or re-signs-in (for instance if the password expired), or performs two factor authentication</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If the access token is expired or
        /// close to expiration (within a 5 minute window), then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        ///
        /// See https://aka.ms/msal-net-acquiretokensilent for more details
        /// </remarks>
        [Obsolete("Use AcquireTokenSilent instead." + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<AuthenticationResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, IAccount account)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }
        #endregion MSAL3X deprecations
    }

    public partial class AuthenticationResult
    {
        /// <summary>
        /// In MSAL.NET 1.x, returned the user who signed in to get the authentication result. From MSAL 2.x
        /// rather use <see cref="Account"/> instead. See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use Account instead (See https://aka.ms/msal-net-2-released)", true)]
        public IUser User { get { throw new NotImplementedException(); } }
    }

    public partial class TokenCacheNotificationArgs
    {
        /// <summary>
        /// In MSAL.NET 1.x, returned the user who signed in to get the authentication result. From MSAL 2.x
        /// rather use <see cref="Account"/> instead. See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use Account instead (See https://aka.ms/msal-net-2-released)", true)]
        public IUser User { get { throw new NotImplementedException(); } }
    }

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

        /// <inheritdoc/>
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
                .WithRedirectUri(PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(clientId))
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

#if SUPPORTS_CONFIDENTIAL_CLIENT
     public partial interface IConfidentialClientApplication
    {
    #region MSAL3X deprecations

        /// <summary>
        /// [V3 API] Acquires token using On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of)
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="userAssertion">Instance of UserAssertion containing user's token.</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        [Obsolete("Use AcquireTokenOnBehalfOf instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(
            IEnumerable<string> scopes,
            UserAssertion userAssertion);

        /// <summary>
        /// [V3 API] Acquires token using On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of)
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="userAssertion">Instance of UserAssertion containing user's token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        [Obsolete("Use AcquireTokenOnBehalfOf instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(
            IEnumerable<string> scopes,
            UserAssertion userAssertion,
            string authority);

        /// <summary>
        /// [V2 API] Acquires security token from the authority using authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="IClientApplicationBase.AcquireTokenSilentAsync(System.Collections.Generic.IEnumerable{string}, IAccount)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        [Obsolete("Use AcquireTokenByAuthorizationCode instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(
            string authorizationCode,
            IEnumerable<string> scopes);

        /// <summary>
        /// [V2 API] Acquires token from the service for the confidential client. This method attempts to look up valid access token in the cache.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenForClient instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenForClientAsync(
            IEnumerable<string> scopes);

        /// <summary>
        /// [V2 API] Acquires token from the service for the confidential client. This method attempts to look up valid access token in the cache.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="forceRefresh">If TRUE, API will ignore the access token in the cache and attempt to acquire new access token using client credentials</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenForClient instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenForClientAsync(
            IEnumerable<string> scopes,
            bool forceRefresh);

        /// <summary>
        /// [V2 API] URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use GetAuthorizationRequestUrl instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<Uri> GetAuthorizationRequestUrlAsync(
            IEnumerable<string> scopes,
            string loginHint,
            string extraQueryParameters);

        /// <summary>
        /// [V2 API] Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use GetAuthorizationRequestUrl instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<Uri> GetAuthorizationRequestUrlAsync(
            IEnumerable<string> scopes,
            string redirectUri,
            string loginHint,
            string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority);

    #endregion MSAL3X deprecations
    }
#endif

    /// <summary>
    /// Component to be used with confidential client applications like web apps/APIs.
    /// This component supports Subject Name + Issuer authentication in order to help, in the future,
    /// Azure AD certificates rollover.
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [EditorBrowsable(EditorBrowsableState.Never)]
#endif
    public interface IConfidentialClientApplicationWithCertificate
    {
        /// <summary>
        /// [V2 API] Acquires token from the service for the confidential client using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// This method enables application developers to achieve easy certificates roll-over
        /// in Azure AD: this method will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or PowerShell/CLI operation).
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenForClient instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenForClientWithCertificateAsync(IEnumerable<string> scopes);

        /// <summary>
        /// [V2 API] Acquires token from the service for the confidential client using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// This method attempts to look up valid access token in the cache unless<paramref name="forceRefresh"/> is true
        /// This method enables application developers to achieve easy certificates roll-over
        /// in Azure AD: this method will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or PowerShell/CLI operation)
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="forceRefresh">If TRUE, API will ignore the access token in the cache and attempt to acquire new access token using client credentials</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenForClient instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenForClientWithCertificateAsync(IEnumerable<string> scopes, bool forceRefresh);

        /// <summary>
        ///[V2 API] Acquires token using On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of)
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="userAssertion">Instance of UserAssertion containing user's token.</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenForClient instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenOnBehalfOfWithCertificateAsync(IEnumerable<string> scopes, UserAssertion userAssertion);

        /// <summary>
        /// [V2 API] Acquires token using On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of)
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="userAssertion">Instance of UserAssertion containing user's token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenForClient instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenOnBehalfOfWithCertificateAsync(IEnumerable<string> scopes, UserAssertion userAssertion, string authority);
    }

    public sealed partial class ConfidentialClientApplication
    {
#if SUPPORTS_CONFIDENTIAL_CLIENT
        /// <summary>
        /// [V2 API] Constructor for a confidential client application requesting tokens with the default authority (<see cref="ApplicationBase.DefaultAuthority"/>)
        /// </summary>
        /// <param name="clientId">Client ID (also known as App ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)/. REQUIRED</param>
        /// <param name="redirectUri">URL where the STS will call back the application with the security token. REQUIRED</param>
        /// <param name="clientCredential">Credential, previously shared with Azure AD during the application registration and proving the identity
        /// of the application. An instance of ClientCredential can be created either from an application secret, or a certificate. REQUIRED.</param>
        /// <param name="userTokenCache">Token cache for saving user tokens. Can be set to null if the confidential client
        /// application only uses the Client Credentials grants (that is requests token in its own name and not in the name of users).
        /// Otherwise should be provided. REQUIRED</param>
        /// <param name="appTokenCache">Token cache for saving application (that is client token). Can be set to <c>null</c> except if the application
        /// uses the client credentials grants</param>
        /// <remarks>
        /// See https://aka.ms/msal-net-client-applications for a description of confidential client applications (and public client applications)
        /// Client credential grants are overrides of <see cref="ConfidentialClientApplication.AcquireTokenForClientAsync(IEnumerable{string})"/>
        ///
        /// See also <see cref="T:ConfidentialClientApplicationBuilder"/> for the V3 API way of building a confidential client application
        /// with a builder pattern. It offers building the application from configuration options, and a more fluid way of providing parameters.
        /// </remarks>
        /// <seealso cref="ConfidentialClientApplication"/> which
        /// enables app developers to specify the authority
        [Obsolete("Use ConfidentialClientApplicationBuilder instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ConfidentialClientApplication(string clientId, string redirectUri,
            ClientCredential clientCredential, TokenCache userTokenCache, TokenCache appTokenCache)
            : this(ConfidentialClientApplicationBuilder
                .Create(clientId)
                .BuildConfiguration())
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// [V2 API] Constructor for a confidential client application requesting tokens with a specified authority
        /// </summary>
        /// <param name="clientId">Client ID (also named Application ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)/. REQUIRED</param>
        /// <param name="authority">Authority of the security token service (STS) from which MSAL.NET will acquire the tokens.
        /// Usual authorities are:
        /// <list type="bullet">
        /// <item><description><c>https://login.microsoftonline.com/tenant/</c>, where <c>tenant</c> is the tenant ID of the Azure AD tenant
        /// or a domain associated with this Azure AD tenant, in order to sign-in users of a specific organization only</description></item>
        /// <item><description><c>https://login.microsoftonline.com/common/</c> to sign-in users with any work and school accounts or Microsoft personal accounts</description></item>
        /// <item><description><c>https://login.microsoftonline.com/organizations/</c> to sign-in users with any work and school accounts</description></item>
        /// <item><description><c>https://login.microsoftonline.com/consumers/</c> to sign-in users with only personal Microsoft accounts(live)</description></item>
        /// </list>
        /// Note that this setting needs to be consistent with what is declared in the application registration portal
        /// </param>
        /// <param name="redirectUri">URL where the STS will call back the application with the security token. REQUIRED</param>
        /// <param name="clientCredential">Credential, previously shared with Azure AD during the application registration and proving the identity
        /// of the application. An instance of <see cref="ClientCredential"/> can be created either from an application secret, or a certificate. REQUIRED.</param>
        /// <param name="userTokenCache">Token cache for saving user tokens. Can be set to null if the confidential client
        /// application only uses the Client Credentials grants (that is requests token in its own name and not in the name of users).
        /// Otherwise should be provided. REQUIRED</param>
        /// <param name="appTokenCache">Token cache for saving application (that is client token). Can be set to <c>null</c> except if the application
        /// uses the client credentials grants</param>
        /// <remarks>
        /// See https://aka.ms/msal-net-client-applications for a description of confidential client applications (and public client applications)
        /// Client credential grants are overrides of <see cref="ConfidentialClientApplication.AcquireTokenForClientAsync(IEnumerable{string})"/>
        ///
        /// See also <see cref="T:ConfidentialClientApplicationBuilder"/> for the V3 API way of building a confidential client application
        /// with a builder pattern. It offers building the application from configuration options, and a more fluid way of providing parameters.
        /// </remarks>
        /// <seealso cref="ConfidentialClientApplication"/> which
        /// enables app developers to create a confidential client application requesting tokens with the default authority.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use ConfidentialClientApplicationBuilder instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public ConfidentialClientApplication(string clientId, string authority, string redirectUri,
            ClientCredential clientCredential, TokenCache userTokenCache, TokenCache appTokenCache)
            : this(ConfidentialClientApplicationBuilder
                .Create(clientId)
                .BuildConfiguration())
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }
#endif

        /// <summary>
        /// [V2 API] Acquires an access token for this application (usually a web API) from the authority configured in the application, in order to access
        /// another downstream protected web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of).
        /// This confidential client application was itself called with a token which will be provided in the
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <seealso cref="AcquireTokenOnBehalfOfAsync(IEnumerable{string}, UserAssertion, string)"/> for the on-behalf-of flow when specifying the authority
        /// <seealso cref="AcquireTokenOnBehalfOf(IEnumerable{string}, UserAssertion)"/> which is the corresponding V3 API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenOnBehalfOf instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(IEnumerable<string> scopes, UserAssertion userAssertion)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// [V2 API] Acquires an access token for this application (usually a web API) from a specific authority, in order to access
        /// another downstream protected web API on behalf of a user (See https://aka.ms/msal-net-on-behalf-of).
        /// This confidential client application was itself called with a token which will be provided in the
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <seealso cref="AcquireTokenOnBehalfOfAsync(IEnumerable{string}, UserAssertion)"/> for the on-behalf-of flow without specifying the authority
        /// <seealso cref="AcquireTokenOnBehalfOf(IEnumerable{string}, UserAssertion)"/> which is the corresponding V3 API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenOnBehalfOf instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(
            IEnumerable<string> scopes,
            UserAssertion userAssertion,
            string authority)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// [V2 API] Acquires an access token for this application (usually a web API) from the authority configured in the application, in order to access
        /// another downstream protected web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of).
        /// This confidential client application was itself called with a token which will be provided in the
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// This override sends the certificate, which helps certificate rotation in Azure AD
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <seealso cref="AcquireTokenOnBehalfOf(IEnumerable{string}, UserAssertion)"/> which is the corresponding V3 API
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenOnBehalfOf instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenOnBehalfOfWithCertificateAsync(IEnumerable<string> scopes, UserAssertion userAssertion)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// [V2 API] Acquires an access token for this application (usually a web API) from a specific authority, in order to access
        /// another downstream protected web API on behalf of a user (See https://aka.ms/msal-net-on-behalf-of).
        /// This confidential client application was itself called with a token which will be provided in the
        /// This override sends the certificate, which helps certificate rotation in Azure AD
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <seealso cref="AcquireTokenOnBehalfOf(IEnumerable{string}, UserAssertion)"/> which is the corresponding V3 API
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenOnBehalfOf instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenOnBehalfOfWithCertificateAsync(IEnumerable<string> scopes, UserAssertion userAssertion,
            string authority)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// [V2 API] Acquires a security token from the authority configured in the app using the authorization code previously received from the STS. It uses
        /// the OAuth 2.0 authorization code flow (See https://aka.ms/msal-net-authorization-code).
        /// It's usually used in web apps (for instance ASP.NET / ASP.NET Core web apps) which sign-in users, and therefore receive an authorization code.
        /// This method does not lookup the token cache, but stores the result in it, so it can be looked up using other methods
        /// such as <see cref="IClientApplicationBase.AcquireTokenSilentAsync(IEnumerable{string}, IAccount)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        /// <seealso cref="AcquireTokenByAuthorizationCode(IEnumerable{string}, string)"/> which is the corresponding V2 API
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenByAuthorizationCode instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, IEnumerable<string> scopes)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// [V3 API] Acquires a token from the authority configured in the app, for the confidential client itself (in the name of no user)
        /// using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// </summary>
        /// <param name="scopes">scopes requested to access a protected API. For this flow (client credentials), the scopes
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are really defined statically at application registration
        /// in the portal, and cannot be overridden in the application. See also </param>
        /// <returns>Authentication result containing the token of the user for the requested scopes</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenForClient instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenForClientAsync(IEnumerable<string> scopes)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// [V2 API] Acquires a token from the authority configured in the app, for the confidential client itself (in the name of no user)
        /// using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API. For this flow (client credentials), the scopes
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are really defined statically at application registration
        /// in the portal, and cannot be overridden in the application</param>
        /// <param name="forceRefresh">If <c>true</c>, API will ignore the access token in the cache and attempt to acquire new access token using client credentials.
        /// This override can be used in case the application knows that conditional access policies changed</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        /// <seealso cref="AcquireTokenForClient(IEnumerable{string})"/> which is the corresponding V3 API
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenForClient instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<AuthenticationResult> AcquireTokenForClientAsync(IEnumerable<string> scopes, bool forceRefresh)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// [V2 API] Acquires token from the service for the confidential client using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// This method enables application developers to achieve easy certificate roll-over
        /// in Azure AD: this method will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or PowerShell/CLI operation)
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        /// <seealso cref="AcquireTokenForClient(IEnumerable{string})"/> which is the corresponding V3 API
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenForClient instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenForClientWithCertificateAsync(IEnumerable<string> scopes)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// [V2 API] Acquires token from the service for the confidential client using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// This method attempts to look up valid access token in the cache unless<paramref name="forceRefresh"/> is true
        /// This method enables application developers to achieve easy certificate roll-over
        /// in Azure AD: this method will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or PowerShell/CLI operation)
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="forceRefresh">If TRUE, API will ignore the access token in the cache and attempt to acquire new access token using client credentials</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        /// <seealso cref="AcquireTokenForClient(IEnumerable{string})"/> which is the corresponding V3 API
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenForClient instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenForClientWithCertificateAsync(IEnumerable<string> scopes, bool forceRefresh)
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
        /// [V2 API] Computes the URL of the authorization request letting the user sign-in and consent to the application accessing specific scopes in
        /// the user's name. The URL targets the /authorize endpoint of the authority configured in the application.
        /// This override enables you to specify a login hint and extra query parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN. This can be empty</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        /// <seealso cref="GetAuthorizationRequestUrl(IEnumerable{string})"/> which is the corresponding V3 API
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use GetAuthorizationRequestUrl instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<Uri> GetAuthorizationRequestUrlAsync(
            IEnumerable<string> scopes,
            string loginHint,
            string extraQueryParameters)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// [V2 API] Computes the URL of the authorization request letting the user sign-in and consent to the application accessing specific scopes in
        /// the user's name. The URL targets the /authorize endpoint of the authority specified as the <paramref name="authority"/> parameter.
        /// This override enables you to specify a redirectUri, login hint extra query parameters, extra scope to consent (which are not for the
        /// same resource as the <paramref name="scopes"/>), and an authority.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API (a resource)</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Scopes for additional resources (other than the resource for which <paramref name="scopes"/> are requested),
        /// which a developer can request the user to consent to upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        /// <seealso cref="GetAuthorizationRequestUrl(IEnumerable{string})"/> which is the corresponding V3 API
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use GetAuthorizationRequestUrl instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        public Task<Uri> GetAuthorizationRequestUrlAsync(
            IEnumerable<string> scopes,
            string redirectUri,
            string loginHint,
            string extraQueryParameters,
            IEnumerable<string> extraScopesToConsent,
            string authority)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }
    }

        public partial interface IByRefreshToken
    {
        /// <summary>
        /// Acquires an access token from an existing refresh token and stores it and the refresh token into
        /// the user token cache, where it will be available for further AcquireTokenSilentAsync calls.
        /// This method can be used in migration to MSAL from ADAL v2 and in various integration
        /// scenarios where you have a RefreshToken available.
        /// (see https://aka.ms/msal-net-migration-adal2-msal2)
        /// </summary>
        /// <param name="scopes">Scope to request from the token endpoint.
        /// Setting this to null or empty will request an access token, refresh token and ID token with default scopes</param>
        /// <param name="refreshToken">The refresh token from ADAL 2.x</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use AcquireTokenByRefreshToken instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(IEnumerable<string> scopes, string refreshToken);
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
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(MsalErrorMessage.LoggingClassIsObsolete, true)]
    public sealed class Logger
    {
        /// <summary>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.LoggingClassIsObsolete, true)]
        public static LogCallback LogCallback
        {
            set => throw new NotImplementedException(MsalErrorMessage.LoggingClassIsObsolete);
        }

        /// <summary>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.LoggingClassIsObsolete, true)]
        public static LogLevel Level
        {
            get => throw new NotImplementedException(MsalErrorMessage.LoggingClassIsObsolete);
            set => throw new NotImplementedException(MsalErrorMessage.LoggingClassIsObsolete);
        }

        /// <summary>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.LoggingClassIsObsolete, true)]
        public static bool PiiLoggingEnabled { get; set; } = false;

        /// <summary>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.LoggingClassIsObsolete, true)]
        public static bool DefaultLoggingEnabled { get; set; } = false;
    }

    /// <summary>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(MsalErrorMessage.TelemetryClassIsObsolete, true)]
    public class Telemetry : ITelemetryReceiver
    {
        /// <summary>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.TelemetryClassIsObsolete, true)]
        public delegate void Receiver(List<Dictionary<string, string>> events);

        /// <summary>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.TelemetryClassIsObsolete, true)]
        public static Telemetry GetInstance()
        {
            throw new NotImplementedException(MsalErrorMessage.TelemetryClassIsObsolete);
        }

        /// <summary>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.TelemetryClassIsObsolete, true)]
        public bool TelemetryOnFailureOnly
        {
            get => throw new NotImplementedException(MsalErrorMessage.TelemetryClassIsObsolete);
            set => throw new NotImplementedException(MsalErrorMessage.TelemetryClassIsObsolete);
        }

        /// <summary>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.TelemetryClassIsObsolete, true)]
        public void RegisterReceiver(Receiver r)
        {
            throw new NotImplementedException(MsalErrorMessage.TelemetryClassIsObsolete);
        }

        /// <summary>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.TelemetryClassIsObsolete, true)]
        public bool HasRegisteredReceiver()
        {
            throw new NotImplementedException(MsalErrorMessage.TelemetryClassIsObsolete);
        }

        /// <summary>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(MsalErrorMessage.TelemetryClassIsObsolete, true)]
        void ITelemetryReceiver.HandleTelemetryEvents(List<Dictionary<string, string>> events)
        {
            throw new NotImplementedException(MsalErrorMessage.TelemetryClassIsObsolete);
        }
    }

#if SUPPORTS_CONFIDENTIAL_CLIENT

    /// <summary>
    /// Certificate for a client assertion. This class is used in one of the constructors of <see cref="ClientCredential"/>. ClientCredential
    /// is itself used in the constructor of <see cref="ConfidentialClientApplication"/> to pass to Azure AD a shared secret (registered in the
    /// Azure AD application)
    /// </summary>
    /// <seealso cref="ClientCredential"/> for the constructor of <seealso cref="ClientCredential"/>
    /// with a certificate, and <seealso cref="ConfidentialClientApplication"/>
    /// <remarks>To understand the difference between public client applications and confidential client applications, see https://aka.ms/msal-net-client-applications</remarks>
    [Obsolete("Use ConfidentialClientApplicationBuilder.WithCertificate instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ClientAssertionCertificate
    {
        /// <summary>
        /// Constructor to create certificate information used in <see cref="ClientCredential"/>
        /// to instantiate a <see cref="ClientCredential"/> used in the constructors of <see cref="ConfidentialClientApplication"/>
        /// </summary>
        /// <param name="certificate">The X509 certificate used as credentials to prove the identity of the application to Azure AD.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]        
        public ClientAssertionCertificate(X509Certificate2 certificate)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        /// <summary>
        /// Gets minimum X509 certificate key size in bits
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static int MinKeySizeInBits => 2048;

        /// <summary>
        /// Gets the X509 certificate used as credentials to prove the identity of the application to Azure AD.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public X509Certificate2 Certificate => throw MigrationHelper.CreateMsalNet3BreakingChangesException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal byte[] Sign(ICryptographyManager cryptographyManager, string message)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        // Thumbprint should be url encoded
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal string Thumbprint => throw MigrationHelper.CreateMsalNet3BreakingChangesException();
    }
#endif

#if SUPPORTS_CONFIDENTIAL_CLIENT

    /// <summary>
    /// Meant to be used in confidential client applications, an instance of <c>ClientCredential</c> is passed
    /// to the constructors of (<see cref="ConfidentialClientApplication"/>)
    /// as credentials proving that the application (the client) is what it claims it is. These credentials can be
    /// either a client secret (an application password) or a certificate.
    /// This class has one constructor for each case.
    /// These credentials are added in the application registration portal (in the secret section).
    /// </summary>
    [Obsolete("Use ConfidentialClientApplicationBuilder.WithCertificate or WithClientSecret instead. " + MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ClientCredential
    {
        /// <summary>
        /// Constructor of client (application) credentials from a <see cref="ClientAssertionCertificate"/>
        /// </summary>
        /// <param name="certificate">contains information about the certificate previously shared with AAD at application
        /// registration to prove the identity of the application (the client) requesting the tokens.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ClientCredential(ClientAssertionCertificate certificate)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal ClientAssertionCertificate Certificate => throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal string Assertion
        {
            get { throw MigrationHelper.CreateMsalNet3BreakingChangesException(); }
            set { throw MigrationHelper.CreateMsalNet3BreakingChangesException(); }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal long ValidTo
        {
            get { throw MigrationHelper.CreateMsalNet3BreakingChangesException(); }
            set { throw MigrationHelper.CreateMsalNet3BreakingChangesException(); }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal bool ContainsX5C
        {
            get { throw MigrationHelper.CreateMsalNet3BreakingChangesException(); }
            set { throw MigrationHelper.CreateMsalNet3BreakingChangesException(); }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal string Audience
        {
            get { throw MigrationHelper.CreateMsalNet3BreakingChangesException(); }
            set { throw MigrationHelper.CreateMsalNet3BreakingChangesException(); }
        }

        /// <summary>
        /// Constructor of client (application) credentials from a client secret, also known as the application password.
        /// </summary>
        /// <param name="secret">Secret string previously shared with AAD at application registration to prove the identity
        /// of the application (the client) requesting the tokens.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ClientCredential(string secret)
        {
            throw MigrationHelper.CreateMsalNet3BreakingChangesException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal string Secret => throw MigrationHelper.CreateMsalNet3BreakingChangesException();
    }
#endif

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

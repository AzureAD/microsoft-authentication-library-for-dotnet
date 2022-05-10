// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Broker
{
    // TODO: wpf crashes on the second ATI
    // TODO: need to map exceptions 
    //   - WAM's retrayble exception?
    //   - cancellation exception for when the user closes the browser
    // TODO: remove account is not implemented (Completed)
    // TODO: bug around double interactive auth https://identitydivision.visualstudio.com/Engineering/_workitems/edit/1858419 - block users from calling ATI twice with a semaphore (Fixed)
    // TODO: call start-up only once (i.e. initialize core object only once) 
    // TODO: pass in claims - try {"access_token":{"deviceid":{"essential":true}}} (Completed)
    // TODO: pass is other "extra query params" (Completed)
    // TODO: multi-cloud support (blocked by WAM bug)
    // TODO: add logging (Blocked - a C++ API exists, no C# API yet as it's pretty complex, waiting for msalruntime to expose it)

    internal class RuntimeBroker : IBroker
    {
        private readonly ICoreLogger _logger;
        private readonly IntPtr _parentHandle = IntPtr.Zero;
        internal const string ErrorMessageSuffix = " For more details see https://aka.ms/msal-net-wam";
        private readonly WindowsBrokerOptions _wamOptions;
        private const string WamErrorPrefix = "WAM Error ";

        //MSA-PT
        private const string NativeInteropMsalRequestType = "msal_request_type"; 
        private const string ConsumersPassthroughRequest = "consumer_passthrough";
        
        //Only one broker session can exist at a time
        public static SemaphoreSlim s_interactiveSlimLock { get; set; } = new SemaphoreSlim(1);

        //Error Response 
        public enum ResponseStatus : int
        {
            Unexpected = 0,
            Reserved = 1,
            InteractionRequired = 2,
            NoNetwork = 3,
            NetworkTemporarilyUnavailable = 4,
            ServerTemporarilyUnavailable = 5,
            ApiContractViolation = 6,
            UserCanceled = 7,
            ApplicationCanceled = 8,
            IncorrectConfiguration = 9,
            InsufficientBuffer = 10,
            AuthorityUntrusted = 11,
            UserSwitch = 12,
            AccountUnusable = 13,
            UserDataRemovalRequired = 14
        };

        /// <summary>
        /// Ctor. Only call if on Win10, otherwise a TypeLoadException occurs. See DesktopOsHelper.IsWin10
        /// </summary>
        public RuntimeBroker(
            CoreUIParent uiParent,
            ApplicationConfiguration appConfig,
            ICoreLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _parentHandle = GetParentWindow(uiParent);

            _wamOptions = appConfig.WindowsBrokerOptions ??
                WindowsBrokerOptions.CreateDefault();

            if (_wamOptions.ListWindowsWorkAndSchoolAccounts)
            {
                throw new NotImplementedException("The new broker implementation does not yet support Windows account discovery (ListWindowsWorkAndSchoolAccounts option)");
            }
        }

        /// <summary>
        /// Acquire Token Interactively 
        /// </summary>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="acquireTokenInteractiveParameters"></param>
        /// <returns></returns>
        /// <exception cref="MsalServiceException"></exception>
        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            MsalTokenResponse msalTokenResponse = null;

            try
            {
                //need to provide a handle
                if (_parentHandle == IntPtr.Zero)
                {
                    throw new MsalClientException(
                        "window_handle_required",
                        "Public Client applications wanting to use WAM need to provide their window handle. Console applications can use GetConsoleWindow Windows API for this.");
                }

                //if OperatingSystemAccount is passed then we use the user signed-in on the machine
                if (PublicClientApplication.IsOperatingSystemAccount(authenticationRequestParameters.Account))
                {
                    return await AcquireTokenInteractiveDefaultUserAsync(authenticationRequestParameters, acquireTokenInteractiveParameters).ConfigureAwait(false);
                }

                await s_interactiveSlimLock.WaitAsync().ConfigureAwait(false);
                var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
                
                _logger.Verbose("[WamBroker] Using Windows account picker.");

                using (var core = new NativeInterop.Core())
                using (var authParams = GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
                {
                    //Login Hint
                    string loginHint = authenticationRequestParameters.LoginHint ?? authenticationRequestParameters?.Account?.Username;

                    if (!string.IsNullOrEmpty(loginHint))
                    {
                        _logger.Verbose("[WamBroker] AcquireTokenInteractive - account information provided. Trying to find a Windows account that matches.");
                    }
                    else
                    {
                        _logger.Verbose("[WamBroker] Account information was not provided. Using an account picker.");
                    }

                    using (var result = await core.SignInInteractivelyAsync(
                        _parentHandle,
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        loginHint,
                        cancellationToken).ConfigureAwait(false))
                    {
                        if (result.IsSuccess)
                        {
                            msalTokenResponse = ParseRuntimeResponse(result, authenticationRequestParameters);
                            _logger.Verbose("[WamBroker] Successfully retrieved token.");

                        }
                        else
                        {
                            _logger.Error($"[WamBroker] Could not login interactively. {result.Error}");
                            //throw new MsalServiceException("wam_interactive_failed", $"Could not get the account provider - account picker. {result.Error}");
                            CreateWamErrorResponse(result, authenticationRequestParameters, _logger);
                        }
                    }
                }
            }
            finally
            {
                s_interactiveSlimLock.Release();
            }
            
            return msalTokenResponse;
        }

        /// <summary>
        /// AcquireToken Interactively for the default user using WAM
        /// </summary>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="acquireTokenInteractiveParameters"></param>
        /// <returns></returns>
        /// <exception cref="MsalServiceException"></exception>
        public async Task<MsalTokenResponse> AcquireTokenInteractiveDefaultUserAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            MsalTokenResponse msalTokenResponse = null;

            try
            {
                await s_interactiveSlimLock.WaitAsync().ConfigureAwait(false);
                var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;

                _logger.Verbose("[WamBroker] Signing in with the default user account.");

                using (var core = new NativeInterop.Core())
                using (var authParams = GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
                {
                    using (NativeInterop.AuthResult result = await core.SignInAsync(
                            _parentHandle,
                            authParams,
                            authenticationRequestParameters.CorrelationId.ToString("D"),
                            cancellationToken).ConfigureAwait(false))
                    {
                        if (result.IsSuccess)
                        {
                            msalTokenResponse = ParseRuntimeResponse(result, authenticationRequestParameters);
                        }
                        else
                        {
                            _logger.Error($"[WamBroker] Could not login interactively with the Default OS Account. {result.Error}");
                            //throw new MsalServiceException("wam_interactive_failed", $"Could not get the account provider for the default OS Account. {result.Error}");
                            CreateWamErrorResponse(result, authenticationRequestParameters, _logger);
                        }
                    }
                }
            }
            finally
            {
                s_interactiveSlimLock.Release();
            }
            
            return msalTokenResponse;

        }

        /// <summary>
        /// Parse Native Interop AuthResult Response to MSAL Token Response
        /// </summary>
        /// <param name="authResult"></param>
        /// <param name="authenticationRequestParameters"></param>
        /// <returns></returns>
        /// <exception cref="MsalServiceException"></exception>
        private MsalTokenResponse ParseRuntimeResponse(
                NativeInterop.AuthResult authResult, AuthenticationRequestParameters authenticationRequestParameters)
        {
            try
            {
                string expiresOn = authResult.ExpiresOn.ToString();
                string correlationId = authenticationRequestParameters.CorrelationId.ToString("D");

                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    _logger.Warning("No correlation ID in response");
                    correlationId = null;
                }

                MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
                {
                    AccessToken = authResult.AccessToken,
                    IdToken = authResult.RawIdToken,
                    CorrelationId = correlationId,
                    Scope = authResult.GrantedScopes,
                    ExpiresIn = DateTimeHelpers.GetDurationFromWindowsTimestamp(expiresOn, _logger),
                    ClientInfo = authResult.Account.ClientInfo.ToString(),
                    TokenType = "Bearer",
                    WamAccountId = authResult.Account.Id,
                    TokenSource = TokenSource.Broker
                };

                _logger.Info("WAM response status success");

                return msalTokenResponse;
            }
            catch (NativeInterop.MsalRuntimeException ex)
            {
                throw new MsalServiceException("wam_failed", $"Could not acquire token using WAM. {ex.Message}");
            }

        }

        /// <summary>
        /// Gets the Common Auth Parameters to be passed to Native Interop
        /// </summary>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="isMsaPassthrough"></param>
        /// <returns></returns>
        private NativeInterop.AuthParameters GetCommonAuthParameters(AuthenticationRequestParameters authenticationRequestParameters, bool isMsaPassthrough)
        {
            _logger.Verbose("[WAM Broker] Getting rumtime common auth parameters.");

            var authParams = new NativeInterop.AuthParameters
                (authenticationRequestParameters.AppConfig.ClientId, 
                authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority);
            
            //scopes
            authParams.RequestedScopes = string.Join(" ", authenticationRequestParameters.Scope);
            
            //redirect URI
            authParams.RedirectUri = authenticationRequestParameters.RedirectUri.ToString();

            //MSA-PT
            if (isMsaPassthrough)
                authParams.Properties[NativeInteropMsalRequestType] = ConsumersPassthroughRequest;

            //Client Claims
            if (!string.IsNullOrWhiteSpace(authenticationRequestParameters.ClaimsAndClientCapabilities))
            {
                authParams.DecodedClaims = authenticationRequestParameters.ClaimsAndClientCapabilities;
            }

            //pass extra query parameters if there are any
            if (authenticationRequestParameters.ExtraQueryParameters != null)
            {
                foreach (KeyValuePair<string, string> kvp in authenticationRequestParameters.ExtraQueryParameters)
                {
                    authParams.Properties[kvp.Key] = kvp.Value;
                }
            }

            _logger.Verbose("[WAM Broker] Finished getting rumtime common auth parameters.");

            return authParams;
        }

        /// <summary>
        /// Gets the window handle
        /// </summary>
        /// <param name="uiParent"></param>
        /// <returns></returns>
        private IntPtr GetParentWindow(CoreUIParent uiParent)
        {
            if (uiParent?.OwnerWindow is IntPtr ptr)
            {
                return ptr;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Auth Broker Installation URL
        /// </summary>
        /// <param name="appLink"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void HandleInstallUrl(string appLink)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Check to see if broker is installed and invokable
        /// </summary>
        /// <param name="authorityType"></param>
        /// <returns></returns>
        public bool IsBrokerInstalledAndInvokable(AuthorityType authorityType)
        {
            if (!DesktopOsHelper.IsWin10OrServerEquivalent())
            {
                _logger.Warning("[WAM Broker] Not a supported operating system. WAM broker is not available. ");
                return false;
            }

            // WAM does not work on pure ADFS environments
            if (authorityType == AuthorityType.Adfs)
            {
                _logger.Warning("[WAM Broker] WAM does not work in pure ADFS environments. Falling back to browser for an ADFS authority. ");
                return false;
            }

            _logger.Verbose("[WAM Broker] IsBrokerInstalledAndInvokable true");
            return true;
        }

        /// <summary>
        /// AcquireTokenSilentAsync
        /// </summary>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="acquireTokenSilentParameters"></param>
        /// <returns></returns>
        /// <exception cref="MsalUiRequiredException"></exception>
        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
            MsalTokenResponse msalTokenResponse = null;

            _logger.Verbose("[WamBroker] Acquiring token silently.");

            using (var core = new NativeInterop.Core())
            using (var authParams = GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
            {
                using (var account = await core.ReadAccountByIdAsync(
                    acquireTokenSilentParameters.Account.HomeAccountId.ObjectId,
                    authenticationRequestParameters.CorrelationId.ToString("D"),
                    cancellationToken).ConfigureAwait(false))
                {
                    if (account == null)
                    {
                        _logger.WarningPii(
                            $"Could not find a WAM account for the selected user {acquireTokenSilentParameters.Account.Username}",
                            "Could not find a WAM account for the selected user");

                        throw new MsalUiRequiredException(
                            "wam_no_account_for_id",
                            $"Could not find a WAM account for the selected user {acquireTokenSilentParameters.Account.Username}");
                    }

                    using (NativeInterop.AuthResult result = await core.AcquireTokenSilentlyAsync(
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        account,
                        cancellationToken).ConfigureAwait(false))
                    {
                        if (result.IsSuccess)
                        {
                            msalTokenResponse = ParseRuntimeResponse(result, authenticationRequestParameters);
                        }
                        else
                        {
                            //throw new MsalUiRequiredException(MsalError.FailedToAcquireTokenSilentlyFromBroker, $"Failed to acquire token silently. {result.Error}");
                            CreateWamErrorResponse(result, authenticationRequestParameters, _logger);
                        }
                    }
                }
            }

            return msalTokenResponse;
        }

        /// <summary>
        /// Acquire Token Silent with Default User
        /// </summary>ter
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="acquireTokenSilentParameters"></param>
        /// <returns></returns>
        /// <exception cref="MsalUiRequiredException"></exception>
        public async Task<MsalTokenResponse> AcquireTokenSilentDefaultUserAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
            MsalTokenResponse msalTokenResponse = null;

            _logger.Verbose("[WamBroker] Acquiring token silently for default account.");

            using (var core = new NativeInterop.Core())
            using (var authParams = GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
            {
                using (NativeInterop.AuthResult result = await core.SignInSilentlyAsync(
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        cancellationToken).ConfigureAwait(false))
                {
                    if (result.IsSuccess)
                    {
                        msalTokenResponse = ParseRuntimeResponse(result, authenticationRequestParameters);
                    }
                    else
                    {
                        //throw new MsalUiRequiredException(MsalError.FailedToAcquireTokenSilentlyFromBroker, $"Failed to acquire token silently. {result.Error}");
                        CreateWamErrorResponse(result, authenticationRequestParameters, _logger);
                    }
                }
            }

            return msalTokenResponse;
        }

        /// <summary>
        /// GetAccountsAsync
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="redirectUri"></param>
        /// <param name="authorityInfo"></param>
        /// <param name="cacheSessionManager"></param>
        /// <param name="instanceDiscoveryManager"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<IReadOnlyList<IAccount>> GetAccountsAsync(
            string clientID,
            string redirectUri,
            AuthorityInfo authorityInfo,
            ICacheSessionManager cacheSessionManager,
            IInstanceDiscoveryManager instanceDiscoveryManager)
        {
            // runtime does not yet support account discovery

            return Task.FromResult<IReadOnlyList<IAccount>>(Array.Empty<IAccount>());
        }

        /// <summary>
        /// RemoveAccountAsync
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task RemoveAccountAsync(ApplicationConfiguration appConfig, IAccount account)
        {
            string correlationId = Guid.NewGuid().ToString();

            _logger.Info($"Removing WAM Account. Correlation ID : {correlationId} ");

            using (var core = new NativeInterop.Core())
            {
                using (var wamAccount = await core.ReadAccountByIdAsync(
                    account.HomeAccountId.ObjectId,
                    correlationId).ConfigureAwait(false))
                {
                    if (wamAccount == null)
                    {
                        _logger.WarningPii(
                            $"Could not find a WAM account for the selected user {account.Username}",
                            "Could not find a WAM account for the selected user");
                    }
                    
                    using (NativeInterop.SignOutResult result = await core.SignOutSilentlyAsync(
                        appConfig.ClientId,
                        correlationId,
                        wamAccount).ConfigureAwait(false))
                    {
                        if (result.IsSuccess)
                        {
                            _logger.Verbose("[WamBroker] Account signed out successfully. ");
                        }
                        else
                        {
                            throw new MsalServiceException("wam_failed_to_signout", $"Failed to sign out account. {result.Error}");
                        }
                    }
                }
            }
        }

        internal MsalException CreateWamErrorResponse(
            NativeInterop.AuthResult authResult,
            AuthenticationRequestParameters authenticationRequestParameters,
            ICoreLogger logger)
        {
            MsalServiceException serviceException = null;
            string internalErrorCode = null;
            string errorMessage;
            int errorCode;

            switch ((int)authResult.Error.Status)
            {
                case (int)ResponseStatus.UserCanceled:
                    throw new MsalClientException(MsalError.AuthenticationCanceledError, MsalErrorMessage.AuthenticationCanceled);

                // Account Switch occurs when a login hint is passed to WAM but the user chooses a different account for login.
                // MSAL treats this as a success scenario
                //case (int)ResponseStatus.UserSwitch:
                //    logger.Info("WAM response status account switch. Treating as success");
                //    ParseRuntimeResponse(authResult, authenticationRequestParameters);

                case (int)ResponseStatus.InteractionRequired:
                case (int)ResponseStatus.AccountUnusable:
                    errorCode = authResult.Error.ErrorCode;
                    internalErrorCode = authResult.Error.Tag.ToString();
                    errorMessage = WamErrorPrefix +
                        $" Error Code: {errorCode}" +
                        $" Error Message: {authResult.Error.Status}" +
                        $" Internal Error Code: {internalErrorCode}";
                    throw new MsalUiRequiredException(errorCode.ToString(), errorMessage);

                case (int)ResponseStatus.IncorrectConfiguration:
                case (int)ResponseStatus.ApiContractViolation:
                    errorCode = authResult.Error.ErrorCode;
                    internalErrorCode = (authResult.Error.Tag).ToString(CultureInfo.InvariantCulture);
                    errorMessage =
                        $"{WamErrorPrefix} \n" +
                        $" Error Code: {errorCode} \n" +
                        $" Error Message: {authResult.Error.Status} \n" +
                        $" WAM Error Message: {authResult.Error.Context} \n" +
                        $" Internal Error Code: {internalErrorCode} \n" +
                        $" Is Retryable: false \n" +
                        $" Possible causes: \n " +
                        $"- Invalid redirect uri - ensure you have configured the following url in the AAD portal App Registration: {GetExpectedRedirectUri(authenticationRequestParameters.AppConfig.ClientId)} \n" +
                        $"- No Internet connection \n" +
                        $"Please see https://aka.ms/msal-net-wam for details about Windows Broker integration";

                    serviceException = new MsalServiceException(errorCode.ToString(), errorMessage);
                    serviceException.IsRetryable = false;
                    throw serviceException;

                case (int)ResponseStatus.NetworkTemporarilyUnavailable:
                case (int)ResponseStatus.NoNetwork:
                case (int)ResponseStatus.ServerTemporarilyUnavailable:
                    errorCode = authResult.Error.ErrorCode;
                    internalErrorCode = (authResult.Error.Tag).ToString(CultureInfo.InvariantCulture);
                    errorMessage =
                        $"{WamErrorPrefix} \n" +
                        $" Error Code: {errorCode} \n" +
                        $" Error Message: {authResult.Error.Status} \n" +
                        $" WAM Error Message: {authResult.Error.Context} \n" +
                        $" Internal Error Code: {internalErrorCode} \n" +
                        $" Is Retryable: true \n" +
                        $" Possible causes: \n " +
                        $"- Invalid redirect uri - ensure you have configured the following url in the AAD portal App Registration: {GetExpectedRedirectUri(authenticationRequestParameters.AppConfig.ClientId)} \n" +
                        $"- No Internet connection \n" +
                        $"Please see https://aka.ms/msal-net-wam for details about Windows Broker integration";

                    serviceException = new MsalServiceException(errorCode.ToString(), errorMessage);
                    serviceException.IsRetryable = false;
                    throw serviceException;

                default:
                    errorCode = authResult.Error.ErrorCode;
                    internalErrorCode = (authResult.Error.ErrorCode).ToString(CultureInfo.InvariantCulture);
                    errorMessage = $"Unknown {authResult.Error} (internal error code {errorCode}) (internal error code {internalErrorCode})";

                    throw new MsalServiceException(MsalError.UnknownBrokerError, errorMessage);
            }
        }

        private static string GetExpectedRedirectUri(string clientId)
        {
#if WINDOWS_APP
            string sid = Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper();            
            return $"ms-appx-web://microsoft.aad.brokerplugin/{sid}";
#else

            return $"ms-appx-web://microsoft.aad.brokerplugin/{clientId}";
#endif
        }
    }
}

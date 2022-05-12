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
using Microsoft.Identity.Client.NativeInterop;
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

    // TODO: bug around double interactive auth https://identitydivision.visualstudio.com/Engineering/_workitems/edit/1858419 - block users from calling ATI twice with a semaphore (Undo the fix)
    // TODO: call start-up only once (i.e. initialize core object only once) 
    // TODO: pass in claims - try {"access_token":{"deviceid":{"essential":true}}} (Clarify)

    // TODO: multi-cloud support (blocked by WAM bug)
    // TODO: add logging (Blocked - a C++ API exists, no C# API yet as it's pretty complex, waiting for msalruntime to expose it)

    internal class RuntimeBroker : IBroker
    {
        private readonly ICoreLogger _logger;
        private readonly IntPtr _parentHandle = IntPtr.Zero;
        internal const string ErrorMessageSuffix = " For more details see https://aka.ms/msal-net-wam";
        private readonly WindowsBrokerOptions _wamOptions;
        private const string WamErrorPrefix = "WAM Error ";

        public bool IsPopSupported => true;

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

            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
                
            _logger.Verbose("[WamBroker] Using Windows account picker.");

            using (var core = new NativeInterop.Core())
            using (var authParams = WamAdapters.GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
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
                        msalTokenResponse = WamAdapters.ParseRuntimeResponse(result, authenticationRequestParameters, _logger);
                        _logger.Verbose("[WamBroker] Successfully retrieved token.");
                    }
                    else
                    {
                        _logger.Error($"[WamBroker] Could not login interactively. {result.Error}");
                        WamAdapters.CreateWamErrorResponse(result, authenticationRequestParameters, _logger);
                    }
                }
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


            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;

            _logger.Verbose("[WamBroker] Signing in with the default user account.");

            using (var core = new NativeInterop.Core())
            using (var authParams = WamAdapters.GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
            {

                using (NativeInterop.AuthResult result = await core.SignInAsync(
                        _parentHandle,
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        cancellationToken).ConfigureAwait(false))
                {
                    if (result.IsSuccess)
                    {
                        msalTokenResponse = WamAdapters.ParseRuntimeResponse(result, authenticationRequestParameters, _logger);
                    }
                    else
                    {
                        _logger.Error($"[WamBroker] Could not login interactively with the Default OS Account. {result.Error}");
                        WamAdapters.CreateWamErrorResponse(result, authenticationRequestParameters, _logger);
                    }
                }
            }

            return msalTokenResponse;
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
            using (var authParams = WamAdapters.GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
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
                            msalTokenResponse = WamAdapters.ParseRuntimeResponse(result, authenticationRequestParameters, _logger);
                        }
                        else
                        {
                            WamAdapters.CreateWamErrorResponse(result, authenticationRequestParameters, _logger);
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
            using (var authParams = WamAdapters.GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
            {

                using (NativeInterop.AuthResult result = await core.SignInSilentlyAsync(
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        cancellationToken).ConfigureAwait(false))
                {
                    if (result.IsSuccess)
                    {
                        msalTokenResponse = WamAdapters.ParseRuntimeResponse(result, authenticationRequestParameters, _logger);
                    }
                    else
                    {
                        WamAdapters.CreateWamErrorResponse(result, authenticationRequestParameters, _logger);
                    }
                }
            }

            return msalTokenResponse;
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

            //if OperatingSystemAccount is passed then we use the user signed-in on the machine
            if (PublicClientApplication.IsOperatingSystemAccount(account))
            {
                throw new MsalException("wam_remove_account_failed", "Could not remove the default os account");
            }

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
    }
}

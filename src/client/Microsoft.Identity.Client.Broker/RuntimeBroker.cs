// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.NativeInterop;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client.Broker
{
    internal class RuntimeBroker : IBroker
    {
        private readonly ILoggerAdapter _logger;
        private readonly IntPtr _parentHandle = IntPtr.Zero;
        internal const string ErrorMessageSuffix = " For more details see https://aka.ms/msal-net-wam";
        private readonly WindowsBrokerOptions _wamOptions;

        public bool IsPopSupported => true;

        /// <summary>
        /// Ctor. Only call if on Win10, otherwise a TypeLoadException occurs. See DesktopOsHelper.IsWin10
        /// </summary>
        public RuntimeBroker(
            CoreUIParent uiParent,
            ApplicationConfiguration appConfig,
            ILoggerAdapter logger)
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

                _logger.Verbose("[WamBroker] AcquireTokenInteractive - login hint provided? " + string.IsNullOrEmpty(loginHint));

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
                        WamAdapters.ThrowExceptionFromWamError(result, authenticationRequestParameters, _logger);
                    }
                }
            }
            
            return msalTokenResponse;
        }

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
                        WamAdapters.ThrowExceptionFromWamError(result, authenticationRequestParameters, _logger);
                    }
                }
            }

            return msalTokenResponse;
        }

        private IntPtr GetParentWindow(CoreUIParent uiParent)
        {
            if (uiParent?.OwnerWindow is IntPtr ptr)
            {
                return ptr;
            }

            return IntPtr.Zero;
        }

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
                using (var readAccountResult = await core.ReadAccountByIdAsync(
                    acquireTokenSilentParameters.Account.HomeAccountId.ObjectId,
                    authenticationRequestParameters.CorrelationId.ToString("D"),
                    cancellationToken).ConfigureAwait(false))
                {
                    if (!readAccountResult.IsSuccess)
                    {
                        _logger.WarningPii(
                            $"[WamBroker] Could not find a WAM account for the selected user {acquireTokenSilentParameters.Account.Username}",
                            $"[WamBroker] Could not find a WAM account for the selected user {readAccountResult.Error}");

                        throw new MsalUiRequiredException(
                            "wam_no_account_for_id",
                            $"Could not find a WAM account for the selected user {acquireTokenSilentParameters.Account.Username}. {readAccountResult.Error}");
                    }

                    using (NativeInterop.AuthResult result = await core.AcquireTokenSilentlyAsync(
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        readAccountResult.Account,
                        cancellationToken).ConfigureAwait(false))
                    {
                        if (result.IsSuccess)
                        {
                            msalTokenResponse = WamAdapters.ParseRuntimeResponse(result, authenticationRequestParameters, _logger);
                        }
                        else
                        {
                            WamAdapters.ThrowExceptionFromWamError(result, authenticationRequestParameters, _logger);
                        }
                    }
                }
            }

            return msalTokenResponse;
        }

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
                        WamAdapters.ThrowExceptionFromWamError(result, authenticationRequestParameters, _logger);
                    }
                }
            }

            return msalTokenResponse;
        }

        public async Task<MsalTokenResponse> AcquireTokenByUsernamePasswordAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenByUsernamePasswordParameters acquireTokenByUsernamePasswordParameters)
        {
            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
            MsalTokenResponse msalTokenResponse = null;

            _logger.Verbose("[WamBroker] Acquiring token with Username Password flow.");

            using (var core = new NativeInterop.Core())
            using (AuthParameters authParams = WamAdapters.GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
            {
                authParams.Properties["MSALRuntime_Username"] = acquireTokenByUsernamePasswordParameters.Username;
                authParams.Properties["MSALRuntime_Password"] = acquireTokenByUsernamePasswordParameters.Password;

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
                        WamAdapters.ThrowExceptionFromWamError(result, authenticationRequestParameters, _logger);
                    }
                }
            }

            return msalTokenResponse;
        }

        public async Task RemoveAccountAsync(ApplicationConfiguration appConfig, IAccount account)
        {
            string correlationId = Guid.NewGuid().ToString();

            //if OperatingSystemAccount is passed then we use the user signed -in on the machine
            if (PublicClientApplication.IsOperatingSystemAccount(account))
            {
                _logger.Verbose("[WamBroker] Default Operating System Account cannot be removed. ");
                throw new MsalClientException("wam_remove_account_failed", "Default Operating System account cannot be removed.");
            }

            _logger.Info($"Removing WAM Account. Correlation ID : {correlationId} ");

            using (var core = new NativeInterop.Core())
            {
                using (var readAccountResult = await core.ReadAccountByIdAsync(
                    account.HomeAccountId.ObjectId,
                    correlationId).ConfigureAwait(false))
                {
                    if (readAccountResult.IsSuccess)
                    {
                        _logger.Verbose("[WamBroker] WAM Account exist and can be removed.");

                    }
                    else
                    {
                        _logger.WarningPii(
                            $"[WamBroker] Could not find a WAM account for the selected user {account.Username}",
                            $"[WamBroker] Could not find a WAM account for the selected user {readAccountResult.Error}");

                        string errorMessage = $"{readAccountResult.Error} (error code : {readAccountResult.Error.ErrorCode})";
                        throw new MsalServiceException("wam_no_account_found", errorMessage);
                    }
                    
                    using (NativeInterop.SignOutResult result = await core.SignOutSilentlyAsync(
                        appConfig.ClientId,
                        correlationId,
                        readAccountResult.Account).ConfigureAwait(false))
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

        public void HandleInstallUrl(string appLink)
        {
            throw new NotImplementedException();
        }

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

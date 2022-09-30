// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Globalization;

namespace Microsoft.Identity.Client.Broker
{
    internal class RuntimeBroker : IBroker
    {
        private readonly ILoggerAdapter _logger;
        private readonly IntPtr _parentHandle = IntPtr.Zero;
        internal const string ErrorMessageSuffix = " For more details see https://aka.ms/msal-net-wam";
        private readonly WindowsBrokerOptions _wamOptions;
        private static Exception s_initException;

        public bool IsPopSupported => true;

        /// <summary>
        /// Being a C API, MSAL runtime uses a "global init" and "global shutdown" approach. 
        /// It is recommended to initialize the runtime once and to clean it up only once. 
        /// </summary>
        private static Lazy<NativeInterop.Core> s_lazyCore = new Lazy<NativeInterop.Core>(() =>
        {
            try
            {
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

                return new NativeInterop.Core();
            }
            catch (MsalRuntimeException ex) when (ex.Status == ResponseStatus.ApiContractViolation)
            {
                // failed to initialize msal runtime - can happen on older versions of Windows. Means broker is not available.
                // We will never get here with our current OS version check. Instead in this scenario we will fallback to the browser
                // but MSALRuntime does it's internal check for OS compatibility and throws an ApiContractViolation MsalRuntimeException.
                // For any reason, if our OS check fails then this will catch the MsalRuntimeException and 
                // log but we will not fallback to the browser in this case. 
                s_initException = ex;

                // ignored
                return null;
            }
            catch (Exception ex)
            {
                // When MSAL Runtime dlls fails to load then we catch the exception and throw with a meaningful
                // message with information on how to troubleshoot
                throw new MsalClientException(
                    "wam_runtime_init_failed", ex.Message + " See https://aka.ms/msal-net-wam#troubleshooting", ex);
            }
        });

        /// <summary>
        /// Do not execute too much logic here. All "on process" handlers should execute in under 2s on Windows.
        /// </summary>
        private static void OnProcessExit(object sender, EventArgs e)
        {
            if (s_lazyCore.IsValueCreated)
            {
                s_lazyCore.Value?.Dispose();
            }
        }

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
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if msal runtime init failed");
            MsalTokenResponse msalTokenResponse = null;

            //need to provide a handle
            if (_parentHandle == IntPtr.Zero)
            {
                throw new MsalClientException(
                    "window_handle_required",
                    "Desktop applications wanting to use the broker need to provide their window handle. See https://aka.ms/msal-net-wam#parent-window-handles");
            }

            //if OperatingSystemAccount is passed then we use the user signed-in on the machine
            if (PublicClientApplication.IsOperatingSystemAccount(authenticationRequestParameters.Account))
            {
                return await AcquireTokenInteractiveDefaultUserAsync(authenticationRequestParameters, acquireTokenInteractiveParameters).ConfigureAwait(false);
            }

            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;

            _logger.Verbose("[WamBroker] Using Windows account picker.");

            if (authenticationRequestParameters?.Account?.HomeAccountId?.ObjectId != null)
            {
                using (var authParams = WamAdapters.GetCommonAuthParameters(
                    authenticationRequestParameters, 
                    _wamOptions.MsaPassthrough, 
                    _logger))
                {
                    using (var readAccountResult = await s_lazyCore.Value.ReadAccountByIdAsync(
                    authenticationRequestParameters.Account.HomeAccountId.ObjectId,
                    authenticationRequestParameters.CorrelationId.ToString("D"),
                    cancellationToken).ConfigureAwait(false))
                    {
                        if (readAccountResult.IsSuccess)
                        {
                            using (var result = await s_lazyCore.Value.AcquireTokenInteractivelyAsync(
                            _parentHandle,
                            authParams,
                            authenticationRequestParameters.CorrelationId.ToString("D"),
                            readAccountResult.Account,
                            cancellationToken).ConfigureAwait(false))
                            {
                                var errorMessage = "Could not login interactively.";
                                msalTokenResponse = WamAdapters.HandleResponse(result, authenticationRequestParameters, _logger, errorMessage);
                            }
                        }
                        else
                        {
                            _logger.Warning(
                                $"[WamBroker] Could not find a WAM account for the selected user {authenticationRequestParameters.Account.Username}");
                            _logger.Info(
                                $"[WamBroker] Calling SignInInteractivelyAsync this will show the account picker.");

                            msalTokenResponse = await SignInInteractivelyAsync(
                                authenticationRequestParameters, acquireTokenInteractiveParameters)
                                 .ConfigureAwait(false);
                        }
                    }
                }
            }
            else
            {
                msalTokenResponse = await SignInInteractivelyAsync(
                    authenticationRequestParameters, acquireTokenInteractiveParameters)
                    .ConfigureAwait(false);
            }

            return msalTokenResponse;
        }

        public async Task<MsalTokenResponse> SignInInteractivelyAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            MsalTokenResponse msalTokenResponse = null;
            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if msal runtime init failed");

            using (var authParams = WamAdapters.GetCommonAuthParameters(
                authenticationRequestParameters, 
                _wamOptions.MsaPassthrough,
                _logger))
            {
                //Login Hint
                string loginHint = authenticationRequestParameters.LoginHint ?? authenticationRequestParameters?.Account?.Username;
                _logger.Verbose("[WamBroker] AcquireTokenInteractive - login hint provided? " + !string.IsNullOrEmpty(loginHint));

                using (var result = await s_lazyCore.Value.SignInInteractivelyAsync(
                    _parentHandle,
                    authParams,
                    authenticationRequestParameters.CorrelationId.ToString("D"),
                    loginHint,
                    cancellationToken).ConfigureAwait(false))
                {
                    var errorMessage = "Could not login interactively.";
                    msalTokenResponse = WamAdapters.HandleResponse(result, authenticationRequestParameters, _logger, errorMessage);
                }
            }

            return msalTokenResponse;
        }

        public async Task<MsalTokenResponse> AcquireTokenInteractiveDefaultUserAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if msal runtime init failed");

            MsalTokenResponse msalTokenResponse = null;
            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;

            _logger.Verbose("[WamBroker] Signing in with the default user account.");

            using (var authParams = WamAdapters.GetCommonAuthParameters(
                authenticationRequestParameters, 
                _wamOptions.MsaPassthrough,
                _logger))
            {
                using (NativeInterop.AuthResult result = await s_lazyCore.Value.SignInAsync(
                        _parentHandle,
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        cancellationToken).ConfigureAwait(false))
                {
                    var errorMessage = "Could not login interactively with the Default OS Account.";
                    msalTokenResponse = WamAdapters.HandleResponse(result, authenticationRequestParameters, _logger, errorMessage);
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
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if msal runtime init failed");

            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
            MsalTokenResponse msalTokenResponse = null;

            _logger.Verbose("[WamBroker] Acquiring token silently.");

            using (var authParams = WamAdapters.GetCommonAuthParameters(
                authenticationRequestParameters, 
                _wamOptions.MsaPassthrough,
                _logger))
            {
                using (var readAccountResult = await s_lazyCore.Value.ReadAccountByIdAsync(
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

                    using (NativeInterop.AuthResult result = await s_lazyCore.Value.AcquireTokenSilentlyAsync(
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        readAccountResult.Account,
                        cancellationToken).ConfigureAwait(false))
                    {
                        var errorMessage = "Could not acquire token silently.";
                        msalTokenResponse = WamAdapters.HandleResponse(result, authenticationRequestParameters, _logger, errorMessage);
                    }
                }
            }

            return msalTokenResponse;
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentDefaultUserAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if msal runtime init failed");

            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
            MsalTokenResponse msalTokenResponse = null;

            _logger.Verbose("[WamBroker] Acquiring token silently for default account.");

            using (var authParams = WamAdapters.GetCommonAuthParameters(
                authenticationRequestParameters, 
                _wamOptions.MsaPassthrough,
                _logger))
            {
                using (NativeInterop.AuthResult result = await s_lazyCore.Value.SignInSilentlyAsync(
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        cancellationToken).ConfigureAwait(false))
                {
                    var errorMessage = "Could not acquire token silently for the default user.";
                    msalTokenResponse = WamAdapters.HandleResponse(result, authenticationRequestParameters, _logger, errorMessage);
                }
            }

            return msalTokenResponse;
        }

        public async Task<MsalTokenResponse> AcquireTokenByUsernamePasswordAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenByUsernamePasswordParameters acquireTokenByUsernamePasswordParameters)
        {
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if msal runtime init failed");

            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
            MsalTokenResponse msalTokenResponse = null;

            _logger.Verbose("[WamBroker] Acquiring token with Username Password flow.");

            using (AuthParameters authParams = WamAdapters.GetCommonAuthParameters(
                authenticationRequestParameters, 
                _wamOptions.MsaPassthrough,
                _logger))
            {
                authParams.Properties["MSALRuntime_Username"] = acquireTokenByUsernamePasswordParameters.Username;
                authParams.Properties["MSALRuntime_Password"] = acquireTokenByUsernamePasswordParameters.Password;

                using (NativeInterop.AuthResult result = await s_lazyCore.Value.SignInSilentlyAsync(
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
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if msal runtime init failed");
            
            if (account == null)
            {
                _logger.Verbose("[WamBroker] No valid account was passed to RemoveAccountAsync. ");
                throw new MsalClientException("wam_remove_account_failed", "No valid account was passed.");
            }

            string correlationId = Guid.NewGuid().ToString();

            //if OperatingSystemAccount is passed then we use the user signed -in on the machine
            if (PublicClientApplication.IsOperatingSystemAccount(account))
            {
                _logger.Verbose("[WamBroker] Default Operating System Account cannot be removed. ");
                throw new MsalClientException("wam_remove_account_failed", "Default Operating System account cannot be removed.");
            }

            _logger.Info($"Removing WAM Account. Correlation ID : {correlationId} ");

            {
                using (var readAccountResult = await s_lazyCore.Value.ReadAccountByIdAsync(
                    account.HomeAccountId.ObjectId,
                    correlationId,
                    default).ConfigureAwait(false))
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

                    using (NativeInterop.SignOutResult result = await s_lazyCore.Value.SignOutSilentlyAsync(
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
                            WamAdapters.ThrowExceptionFromWamError(result, _logger);
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
                _logger.Warning("[WAM Broker] WAM does not work in pure ADFS environments. Falling back to browser for an ADFS authority unless Proof-of-Possession is configured. ");
                return false;
            }

            if (s_lazyCore.Value == null)
            {
                _logger.Info("[WAM Broker] MsalRuntime initialization failed. See https://aka.ms/msal-net-wam#wam-limitations");
                _logger.InfoPii(s_initException);
                return false;
            }

            _logger.Verbose($"[WAM Broker] MsalRuntime initialization successful.");
            return true;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
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
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.WsTrust;

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
            catch (Exception ex) 
            {
                s_initException = ex;

                // ignored
                return null;
            }

        });
        
        /// <summary>
        /// Do not execute too much logic here. All "on process" handlers should execute in under 2s on Windows.
        /// </summary>
        private static void OnProcessExit(object sender, EventArgs e)
        {
            if (s_lazyCore.IsValueCreated )
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
                    "Public Client applications wanting to use WAM need to provide their window handle. Console applications can use GetConsoleWindow Windows API for this.");
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
                using (var authParams = WamAdapters.GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
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

            using (var authParams = WamAdapters.GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
            {
                //Login Hint
                string loginHint = authenticationRequestParameters.LoginHint ?? authenticationRequestParameters?.Account?.Username;
                _logger.Verbose("[WamBroker] AcquireTokenInteractive - login hint provided? " + string.IsNullOrEmpty(loginHint));
                
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

            
            using (var authParams = WamAdapters.GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
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

            
            using (var authParams = WamAdapters.GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
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

            
            using (var authParams = WamAdapters.GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
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

            
            using (AuthParameters authParams = WamAdapters.GetCommonAuthParameters(authenticationRequestParameters, _wamOptions.MsaPassthrough))
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
                            throw new MsalServiceException("wam_failed_to_signout", $"Failed to sign out account. {result.Error}");
                        }
                    }
                }
            }
        }

        public async Task<IReadOnlyList<IAccount>> GetAccountsAsync(
            string clientID,
            string redirectUri,
            AuthorityInfo authorityInfo,
            ICacheSessionManager cacheSessionManager,
            IInstanceDiscoveryManager instanceDiscoveryManager)
        {
            if (!_wamOptions.ListWindowsWorkAndSchoolAccounts)
            {
                _logger.Info("[WamBroker] Returning no accounts due to configuration option.");
                return Array.Empty<IAccount>();
            }

            string correlationId = Guid.NewGuid().ToString();
            List<IAccount> accounts = new List<IAccount>();
            var requestContext = cacheSessionManager.RequestContext;

            using (var core = new NativeInterop.Core())
            using (var discoverAccountsResult = await core.DiscoverAccountsAsync(
                clientID, 
                correlationId, 
                cacheSessionManager.RequestContext.UserCancellationToken).ConfigureAwait(false))
            {
                if (discoverAccountsResult.IsSuccess)
                {
                    IEnumerable<NativeInterop.Account> wamAccounts = discoverAccountsResult.Accounts;

                    _logger.Info($"[WamBroker] Broker returned {wamAccounts.Count()} account(s).");

                    //If "multi-cloud" is enabled, we do not have do instanceMetadata matching
                    if (!requestContext.ServiceBundle.Config.MultiCloudSupportEnabled)
                    {
                        var environment = discoverAccountsResult.Accounts.Select(acc => acc.Environment).ToList();

                        var instanceMetadata = await instanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                                authorityInfo,
                                environment,
                                requestContext).ConfigureAwait(false);

                        _logger.Info($"[WamBroker] Filtering WAM accounts based on Environment.");

                        wamAccounts = wamAccounts.Where(acc => instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(acc.Environment));

                        _logger.Info($"[WamBroker] {wamAccounts.Count()} account(s) returned after filtering.");
                    }
                    
                    foreach (var acc in wamAccounts)
                    {
                        accounts.Add(WamAdapters.ConvertToMsalAccount(acc, clientID, _logger));
                    }

                    _logger.Info($"[WamBroker] Converted {accounts.Count} WAM account(s) to MSAL IAccount.");
                }
                else
                {
                    string errorMessage =
                        $" [WamBroker] \n" +
                        $" Error Code: {discoverAccountsResult.Error.ErrorCode} \n" +
                        $" Error Message: {discoverAccountsResult.Error.Context} \n" +
                        $" Internal Error Code: {discoverAccountsResult.Error.Tag.ToString(CultureInfo.InvariantCulture)} \n" +
                        $" Telemetry Data: {discoverAccountsResult.TelemetryData } \n";
                    _logger.Error($"[WamBroker] {errorMessage}");

                    return Array.Empty<IAccount>();
                }
            }

            return accounts;
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

            if (s_lazyCore.Value == null)
            {
                _logger.Info("[WAM Broker] MsalRuntime init failed...");
                _logger.InfoPii(s_initException);

                return false;
            }

            _logger.Verbose($"[WAM Broker] MsalRuntime init succesful.");
            return true;
        }
    }
}

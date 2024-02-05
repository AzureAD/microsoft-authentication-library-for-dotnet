// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Features.RuntimeBroker
{
    internal class RuntimeBroker : IBroker
    {
        private readonly ILoggerAdapter _logger;
        private readonly IntPtr _parentHandle = IntPtr.Zero;
        internal const string ErrorMessageSuffix = " For more details see https://aka.ms/msal-net-wam";
        private readonly BrokerOptions _wamOptions;
        private static Exception s_initException;

        private static Dictionary<NativeInterop.LogLevel, LogLevel> LogLevelMap = new Dictionary<NativeInterop.LogLevel, LogLevel>()
        {
            { NativeInterop.LogLevel.Trace, LogLevel.Verbose },
            { NativeInterop.LogLevel.Debug, LogLevel.Verbose },
            { NativeInterop.LogLevel.Info, LogLevel.Info },
            { NativeInterop.LogLevel.Warning, LogLevel.Warning },
            { NativeInterop.LogLevel.Error, LogLevel.Error },
            { NativeInterop.LogLevel.Fatal, LogLevel.Error },
        };

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
                // failed to initialize MSAL runtime - can happen on older versions of Windows. Means broker is not available.
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
                // When the MSAL Runtime DLL fails to load then we catch the exception and throw with a meaningful
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

            if (_logger.PiiLoggingEnabled)
            {
                s_lazyCore.Value.EnablePii(_logger.PiiLoggingEnabled);
            }

            _parentHandle = GetParentWindow(uiParent);

            // Broker options cannot be null
            _wamOptions = appConfig.BrokerOptions;
        }

        private void LogEventRaised(NativeInterop.Core sender, LogEventArgs args)
        {
            LogLevel msalLogLevel = LogLevelMap[args.LogLevel];
            if (_logger.IsLoggingEnabled(msalLogLevel))
            {
                if (_logger.PiiLoggingEnabled)
                {
                    _logger.Log(msalLogLevel, args.Message, string.Empty);
                }
                else
                {
                    _logger.Log(msalLogLevel, string.Empty, args.Message);
                }
            }
        }

        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            using LogEventWrapper logEventWrapper = new LogEventWrapper(this);
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if MSAL runtime init failed");

            //need to provide a handle
            if (_parentHandle == IntPtr.Zero)
            {
                throw new MsalClientException(
                    "window_handle_required",
                    "A window handle must be configured. See https://aka.ms/msal-net-wam#parent-window-handles");
            }

            //if OperatingSystemAccount is passed then we use the user signed-in on the machine
            if (PublicClientApplication.IsOperatingSystemAccount(authenticationRequestParameters.Account))
            {
                return await AcquireTokenInteractiveDefaultUserAsync(authenticationRequestParameters).ConfigureAwait(false);
            }

            MsalTokenResponse msalTokenResponse;

            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;

            _logger?.Verbose(() => "[RuntimeBroker] Using Windows account picker.");

            if (authenticationRequestParameters?.Account?.HomeAccountId?.ObjectId != null)
            {
                using (var authParams = WamAdapters.GetCommonAuthParameters(
                    authenticationRequestParameters,
                    _wamOptions,
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
                                var errorMessage = "Could not acquire token interactively.";
                                msalTokenResponse = WamAdapters.HandleResponse(result, authenticationRequestParameters, _logger, errorMessage);
                            }
                        }
                        else
                        {
                            _logger?.WarningPii(
                                $"[RuntimeBroker] Could not find a WAM account for the selected user {authenticationRequestParameters.Account.Username}, error: {readAccountResult.Error}",
                                $"[RuntimeBroker] Could not find a WAM account for the selected user. Error: {readAccountResult.Error}");

                            msalTokenResponse = await SignInInteractivelyAsync(
                                authenticationRequestParameters)
                                 .ConfigureAwait(false);
                        }
                    }
                }
            }
            else
            {
                msalTokenResponse = await SignInInteractivelyAsync(
                    authenticationRequestParameters)
                    .ConfigureAwait(false);
            }

            return msalTokenResponse;
        }

        private async Task<MsalTokenResponse> SignInInteractivelyAsync(
            AuthenticationRequestParameters authenticationRequestParameters)
        {
            MsalTokenResponse msalTokenResponse;
            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if MSAL runtime init failed");

            _logger?.Info($"[RuntimeBroker] Calling SignInInteractivelyAsync this will show the account picker.");

            using (var authParams = WamAdapters.GetCommonAuthParameters(
                authenticationRequestParameters,
                _wamOptions,
                _logger))
            {
                //Login Hint
                string loginHint = authenticationRequestParameters.LoginHint ?? authenticationRequestParameters?.Account?.Username;
                _logger?.Verbose(() => "[RuntimeBroker] AcquireTokenInteractive - login hint provided? " + !string.IsNullOrEmpty(loginHint));

                using (var result = await s_lazyCore.Value.SignInInteractivelyAsync(
                    _parentHandle,
                    authParams,
                    authenticationRequestParameters.CorrelationId.ToString("D"),
                    loginHint,
                    cancellationToken).ConfigureAwait(false))
                {
                    var errorMessage = "Could not sign in interactively.";
                    msalTokenResponse = WamAdapters.HandleResponse(result, authenticationRequestParameters, _logger, errorMessage);
                }
            }

            return msalTokenResponse;
        }

        private async Task<MsalTokenResponse> AcquireTokenInteractiveDefaultUserAsync(
            AuthenticationRequestParameters authenticationRequestParameters)
        {
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if MSAL runtime init failed");

            MsalTokenResponse msalTokenResponse;
            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;

            _logger?.Verbose(() => "[RuntimeBroker] Signing in with the default user account.");

            using (var authParams = WamAdapters.GetCommonAuthParameters(
                authenticationRequestParameters,
                _wamOptions,
                _logger))
            {
                using (NativeInterop.AuthResult result = await s_lazyCore.Value.SignInAsync(
                        _parentHandle,
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        cancellationToken).ConfigureAwait(false))
                {
                    var errorMessage = "Could not sign in interactively with the default OS account.";
                    msalTokenResponse = WamAdapters.HandleResponse(result, authenticationRequestParameters, _logger, errorMessage);
                }
            }

            return msalTokenResponse;
        }

        private static IntPtr GetParentWindow(CoreUIParent uiParent)
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
            using LogEventWrapper logEventWrapper = new LogEventWrapper(this);
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if MSAL runtime init failed");

            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
            MsalTokenResponse msalTokenResponse;

            _logger?.Verbose(() => "[RuntimeBroker] Acquiring token silently.");

            using (var authParams = WamAdapters.GetCommonAuthParameters(
                authenticationRequestParameters,
                _wamOptions,
                _logger))
            {
                using (var readAccountResult = await s_lazyCore.Value.ReadAccountByIdAsync(
                    authenticationRequestParameters.Account.HomeAccountId.ObjectId,
                    authenticationRequestParameters.CorrelationId.ToString("D"),
                    cancellationToken).ConfigureAwait(false))
                {
                    if (!readAccountResult.IsSuccess)
                    {
                        _logger?.WarningPii(
                            $"[RuntimeBroker] Could not find a WAM account for the selected user {acquireTokenSilentParameters.Account.Username}. Error: {readAccountResult.Error}",
                            $"[RuntimeBroker] Could not find a WAM account for the selected user. Error: {readAccountResult.Error}");

                        throw new MsalUiRequiredException(
                            "wam_no_account_for_id",
                            $"Could not find a WAM account for the selected user. Error: {readAccountResult.Error}");
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

                    if (acquireTokenSilentParameters.ForceRefresh && !string.IsNullOrEmpty(msalTokenResponse.AccessToken))
                    {
                        authParams.AccessTokenToRenew = msalTokenResponse.AccessToken;

                        using (NativeInterop.AuthResult result = await s_lazyCore.Value.AcquireTokenSilentlyAsync(
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        readAccountResult.Account,
                        cancellationToken).ConfigureAwait(false))
                        {
                            var errorMessage = "Could not acquire token silently with AccessTokenToRenew option.";
                            msalTokenResponse = WamAdapters.HandleResponse(result, authenticationRequestParameters, _logger, errorMessage);
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
            using LogEventWrapper logEventWrapper = new LogEventWrapper(this);
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if MSAL runtime init failed");

            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
            MsalTokenResponse msalTokenResponse;

            _logger?.Verbose(() => "[RuntimeBroker] Acquiring token silently for default account.");

            using (var authParams = WamAdapters.GetCommonAuthParameters(
                authenticationRequestParameters,
                _wamOptions,
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

                if (acquireTokenSilentParameters.ForceRefresh && !string.IsNullOrEmpty(msalTokenResponse.AccessToken))
                {
                    authParams.AccessTokenToRenew = msalTokenResponse.AccessToken;

                    using (NativeInterop.AuthResult result = await s_lazyCore.Value.SignInSilentlyAsync(
                       authParams,
                       authenticationRequestParameters.CorrelationId.ToString("D"),
                       cancellationToken).ConfigureAwait(false))
                    {
                        var errorMessage = "Could not acquire token silently for the default user.";
                        msalTokenResponse = WamAdapters.HandleResponse(result, authenticationRequestParameters, _logger, errorMessage);
                    }
                }

            }

            return msalTokenResponse;
        }

        public async Task<MsalTokenResponse> AcquireTokenByUsernamePasswordAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenByUsernamePasswordParameters acquireTokenByUsernamePasswordParameters)
        {
            using LogEventWrapper logEventWrapper = new LogEventWrapper(this);
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if msal runtime init failed");

            var cancellationToken = authenticationRequestParameters.RequestContext.UserCancellationToken;
            MsalTokenResponse msalTokenResponse;

            _logger?.Verbose(() => "[RuntimeBroker] Acquiring token with Username Password flow.");

            using (AuthParameters authParams = WamAdapters.GetCommonAuthParameters(
                authenticationRequestParameters,
                _wamOptions,
                _logger))
            {
                authParams.Properties["MSALRuntime_Username"] = acquireTokenByUsernamePasswordParameters.Username;
                authParams.Properties["MSALRuntime_Password"] = acquireTokenByUsernamePasswordParameters.Password;

                using (NativeInterop.AuthResult result = await s_lazyCore.Value.SignInSilentlyAsync(
                        authParams,
                        authenticationRequestParameters.CorrelationId.ToString("D"),
                        cancellationToken).ConfigureAwait(false))
                {
                    var errorMessage = "Could not acquire token with username and password.";
                    msalTokenResponse = WamAdapters.HandleResponse(result, authenticationRequestParameters, _logger, errorMessage);
                }
            }

            return msalTokenResponse;
        }

        public async Task RemoveAccountAsync(ApplicationConfiguration appConfig, IAccount account)
        {
            using LogEventWrapper logEventWrapper = new LogEventWrapper(this);
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if MSAL runtime init failed");

            if (account == null)
            {
                _logger?.Verbose(() => "[RuntimeBroker] No valid account was passed to RemoveAccountAsync. ");
                throw new MsalClientException("wam_remove_account_failed", "No valid account was passed.");
            }

            string correlationId = Guid.NewGuid().ToString();

            //if OperatingSystemAccount is passed then we use the user signed -in on the machine
            if (PublicClientApplication.IsOperatingSystemAccount(account))
            {
                _logger?.Verbose(() => "[RuntimeBroker] Default Operating System Account cannot be removed. ");
                throw new MsalClientException("wam_remove_account_failed", "Default Operating System account cannot be removed.");
            }

            _logger?.Info(() => $"Removing WAM Account. Correlation ID : {correlationId} ");

            {
                using (var readAccountResult = await s_lazyCore.Value.ReadAccountByIdAsync(
                    account.HomeAccountId.ObjectId,
                    correlationId,
                    default).ConfigureAwait(false))
                {
                    if (readAccountResult.IsSuccess)
                    {
                        _logger?.Verbose(() => "[RuntimeBroker] WAM Account exists and can be removed.");

                    }
                    else
                    {
                        _logger?.WarningPii(
                            $"[RuntimeBroker] Could not find a WAM account for the selected user {account.Username} - error: {readAccountResult.Error}",
                            $"[RuntimeBroker] Could not find a WAM account for the selected user, error: {readAccountResult.Error}");
                    }

                    using (NativeInterop.SignOutResult result = await s_lazyCore.Value.SignOutSilentlyAsync(
                        appConfig.ClientId,
                        correlationId,
                        readAccountResult.Account).ConfigureAwait(false))
                    {
                        if (result.IsSuccess)
                        {
                            _logger?.Verbose(() => "[RuntimeBroker] Account signed out successfully. ");
                        }
                        else
                        {
                            _logger?.WarningPii(
                            $"[RuntimeBroker] Could not sign out user {account.Username} - error: {result.Error}",
                            $"[RuntimeBroker] Could not sign out user, error: {result.Error}");
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
            if (!_wamOptions.ListOperatingSystemAccounts)
            {
                _logger.Info("[RuntimeBroker] ListWindowsWorkAndSchoolAccounts option was not enabled.");
                return Array.Empty<IAccount>();
            }
            using LogEventWrapper logEventWrapper = new LogEventWrapper(this);
            Debug.Assert(s_lazyCore.Value != null, "Should not call this API if MSAL runtime init failed");

            var requestContext = cacheSessionManager.RequestContext;

            using (var discoverAccountsResult = await s_lazyCore.Value.DiscoverAccountsAsync(
                clientID,
                cacheSessionManager.RequestContext.CorrelationId.ToString("D"),
                cacheSessionManager.RequestContext.UserCancellationToken).ConfigureAwait(false))
            {
                if (discoverAccountsResult.IsSuccess)
                {
                    List<NativeInterop.Account> wamAccounts = discoverAccountsResult.Accounts;

                    _logger.Info(() => $"[RuntimeBroker] Broker returned {wamAccounts.Count} account(s).");

                    if (wamAccounts.Count == 0)
                    {
                        return Array.Empty<IAccount>();
                    }

                    //If "multi-cloud" is enabled, we do not have to do instanceMetadata matching
                    if (!requestContext.ServiceBundle.Config.MultiCloudSupportEnabled)
                    {
                        var environmentList = discoverAccountsResult.Accounts.Select(acc => acc.Environment).Distinct().ToList();

                        var instanceMetadata = await instanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                                authorityInfo,
                                environmentList,
                                requestContext).ConfigureAwait(false);

                        _logger.Verbose(() => $"[RuntimeBroker] Filtering WAM accounts based on Environment.");

                        wamAccounts.RemoveAll(acc => !instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(acc.Environment));

                        _logger.Verbose(() => $"[RuntimeBroker] {wamAccounts.Count} account(s) returned after filtering.");
                    }

                    List<IAccount> msalAccounts = new List<IAccount>();

                    foreach (var acc in wamAccounts)
                    {
                        if (WamAdapters.TryConvertToMsalAccount(acc, clientID, _logger, out IAccount account))
                        {
                            msalAccounts.Add(account);
                        }
                    }

                    _logger.Verbose(() => $"[RuntimeBroker] Converted {msalAccounts.Count} WAM account(s) to MSAL Account(s).");

                    return msalAccounts;
                }
                else
                {
                    string errorMessagePii =
                        $" [RuntimeBroker] \n" +
                        $" Error Code: {discoverAccountsResult.Error.ErrorCode} \n" +
                        $" Error Message: {discoverAccountsResult.Error.Context} \n" +
                        $" Internal Error Code: {discoverAccountsResult.Error.Tag.ToString(CultureInfo.InvariantCulture)} \n" +
                        $" Telemetry Data: {discoverAccountsResult.TelemetryData} \n";

                    _logger.ErrorPii($"[RuntimeBroker] {errorMessagePii}",
                        $"[RuntimeBroker] DiscoverAccounts Error. " +
                        $"Error Code : {discoverAccountsResult.Error.ErrorCode}. " +
                        $"Internal Error Code: {discoverAccountsResult.Error.Tag.ToString(CultureInfo.InvariantCulture)}");

                    return Array.Empty<IAccount>();
                }
            }
        }

        public void HandleInstallUrl(string appLink)
        {
            throw new NotImplementedException();
        }

        public bool IsBrokerInstalledAndInvokable(AuthorityType authorityType)
        {
            if (!DesktopOsHelper.IsWin10OrServerEquivalent())
            {
                _logger?.Warning("[RuntimeBroker] Not a supported operating system. WAM broker is not available. ");
                return false;
            }

            // WAM only works with AAD
            if (authorityType != AuthorityType.Aad)
            {
                _logger?.Warning($"[RuntimeBroker] Authority is {authorityType}. WAM is not available");
                return false;
            }

            if (s_lazyCore.Value == null)
            {
                _logger?.Info(() => "[RuntimeBroker] MsalRuntime initialization failed. See https://aka.ms/msal-net-wam#wam-limitations");
                _logger?.InfoPii(s_initException);
                return false;
            }

            _logger?.Verbose(() => "[RuntimeBroker] MsalRuntime initialization successful.");
            return true;
        }

        internal class LogEventWrapper : IDisposable
        {
            private bool _disposedValue;
            RuntimeBroker _broker;

            public LogEventWrapper(RuntimeBroker broker)
            {
                _broker = broker;
                s_lazyCore.Value.LogEvent += _broker.LogEventRaised;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        // dispose managed state (managed objects)
                        s_lazyCore.Value.LogEvent -= _broker.LogEventRaised;
                    }

                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}

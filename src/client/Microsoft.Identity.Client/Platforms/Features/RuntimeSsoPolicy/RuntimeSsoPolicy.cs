// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.SsoPolicy;
using Microsoft.Identity.Client.NativeInterop;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Features.RuntimeSsoPolicy
{
    internal class RuntimeSsoPolicy : ISsoPolicy
    {
        private readonly ILoggerAdapter _logger;
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
        public RuntimeSsoPolicy(
            ApplicationConfiguration appConfig,
            ILoggerAdapter logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (_logger.PiiLoggingEnabled)
            {
                s_lazyCore.Value.EnablePii(_logger.PiiLoggingEnabled);
            }
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

        public async Task<IReadOnlyList<IAccount>> GetAccountsAsync(
            string clientID,
            string redirectUri,
            AuthorityInfo authorityInfo,
            ICacheSessionManager cacheSessionManager)
        {
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
                    List<IAccount> msalAccounts = new List<IAccount>();
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

        internal class LogEventWrapper : IDisposable
        {
            private bool _disposedValue;
            RuntimeSsoPolicy _runtimeSsoPolicy;

            public LogEventWrapper(RuntimeSsoPolicy runtimeSsoPolicy)
            {
                _runtimeSsoPolicy = runtimeSsoPolicy;
                s_lazyCore.Value.LogEvent += _runtimeSsoPolicy.LogEventRaised;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        // dispose managed state (managed objects)
                        s_lazyCore.Value.LogEvent -= _runtimeSsoPolicy.LogEventRaised;
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

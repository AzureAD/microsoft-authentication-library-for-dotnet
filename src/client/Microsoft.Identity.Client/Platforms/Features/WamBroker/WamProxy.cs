// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using System.Threading;

#if NET5_WIN
using Microsoft.Identity.Client.Platforms.net5win;
#elif DESKTOP || NET_CORE
using Microsoft.Identity.Client.Platforms;
#endif

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal class WamProxy : IWamProxy
    {
        private readonly ICoreLogger _logger;
        private readonly SynchronizationContext _synchronizationContext;

        public WamProxy(ICoreLogger logger, System.Threading.SynchronizationContext synchronizationContext)
        {
            _logger = logger;
            _synchronizationContext = synchronizationContext;
        }

        public async Task<IWebTokenRequestResultWrapper> GetTokenSilentlyAsync(WebAccount webAccount, WebTokenRequest webTokenRequest)
        {
            using (_logger.LogBlockDuration("WAM:GetTokenSilentlyAsync:webAccount"))
            {
                if (_logger.IsLoggingEnabled(LogLevel.Verbose))
                {
                    _logger.Verbose(webTokenRequest.ToLogString());
                    _logger.Verbose(webAccount.ToLogString());
                }

                var wamResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest, webAccount);
                return new WebTokenRequestResultWrapper(wamResult);
            }
        }

        public async Task<IWebTokenRequestResultWrapper> GetTokenSilentlyForDefaultAccountAsync(WebTokenRequest webTokenRequest)
        {
            using (_logger.LogBlockDuration("WAM:GetTokenSilentlyAsync:"))
            {
                if (_logger.IsLoggingEnabled(LogLevel.Verbose))
                {
                    _logger.Verbose(webTokenRequest.ToLogString());
                }

                var wamResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest);
                return new WebTokenRequestResultWrapper(wamResult);
            }
        }

        public async Task<IWebTokenRequestResultWrapper> RequestTokenForWindowAsync(
            IntPtr _parentHandle,
            WebTokenRequest webTokenRequest)
        {
            using (_logger.LogBlockDuration("WAM:RequestTokenForWindowAsync:"))
            {
                if (_logger.IsLoggingEnabled(LogLevel.Verbose))
                {
                    _logger.Verbose(webTokenRequest.ToLogString());
                }
#if WINDOWS_APP
            // UWP requires being on the UI thread
            await _synchronizationContext;

            WebTokenRequestResult wamResult = await WebAuthenticationCoreManager.RequestTokenAsync(webTokenRequest);
#else

                var wamResult = await WebAuthenticationCoreManagerInterop.RequestTokenForWindowAsync(
                    _parentHandle, webTokenRequest);
#endif
                return new WebTokenRequestResultWrapper(wamResult);
            }
        }

        public async Task<IWebTokenRequestResultWrapper> RequestTokenForWindowAsync(
           IntPtr _parentHandle,
           WebTokenRequest webTokenRequest,
           WebAccount wamAccount)
        {
            using (_logger.LogBlockDuration("WAM:RequestTokenForWindowAsync:"))
            {
                if (_logger.IsLoggingEnabled(LogLevel.Verbose))
                {
                    _logger.Verbose(webTokenRequest.ToLogString());
                    _logger.Verbose(wamAccount.ToLogString());
                }
#if WINDOWS_APP

            // UWP requires being on the UI thread
            await _synchronizationContext;

            WebTokenRequestResult wamResult = await WebAuthenticationCoreManager.RequestTokenAsync(
                webTokenRequest, 
                wamAccount);
#else

                var wamResult = await WebAuthenticationCoreManagerInterop.RequestTokenWithWebAccountForWindowAsync(
                _parentHandle, webTokenRequest, wamAccount);
#endif
                return new WebTokenRequestResultWrapper(wamResult);
            }
        }

        public async Task<WebAccount> FindAccountAsync(WebAccountProvider provider, string wamAccountId)
        {
            using (_logger.LogBlockDuration("WAM:FindAccountAsync:"))
            {
                if (_logger.IsLoggingEnabled(LogLevel.Verbose))
                {
                    _logger.Verbose(provider.ToLogString());
                }

                return await WebAuthenticationCoreManager.FindAccountAsync(provider, wamAccountId);
            }
        }

        public async Task<IReadOnlyList<WebAccount>> FindAllWebAccountsAsync(WebAccountProvider provider, string clientID)
        {
            using (_logger.LogBlockDuration("WAM:FindAllWebAccountsAsync:"))
            {
                if (_logger.IsLoggingEnabled(LogLevel.Verbose))
                {
                    _logger.Verbose(provider.ToLogString());
                }

                // Win 10 RS3 release and above
                if (!ApiInformation.IsMethodPresent(
                   "Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager",
                   "FindAllAccountsAsync"))
                {
                    _logger.Info("[WamProxy] FindAllAccountsAsync method does not exist (it was introduced in Win 10 RS3). " +
                        "Returning 0 broker accounts. ");
                    return Enumerable.Empty<WebAccount>().ToList();
                }

                FindAllAccountsResult findResult = await WebAuthenticationCoreManager.FindAllAccountsAsync(provider, clientID);

                // This is expected to happen with the MSA provider, which does not allow account listing
                if (findResult.Status != FindAllWebAccountsStatus.Success)
                {
                    var error = findResult.ProviderError;
                    _logger.Info($"[WAM Proxy] WebAuthenticationCoreManager.FindAllAccountsAsync failed " +
                        $" with error code {error.ErrorCode} error message {error.ErrorMessage} and status {findResult.Status}");

                    return Enumerable.Empty<WebAccount>().ToList();
                }

                _logger.Info($"[WAM Proxy] FindAllWebAccountsAsync returning {findResult.Accounts.Count()} WAM accounts");
                return findResult.Accounts;
            }
        }
    }
}

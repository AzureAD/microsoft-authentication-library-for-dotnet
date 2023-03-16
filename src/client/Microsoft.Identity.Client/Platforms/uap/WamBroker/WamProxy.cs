// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.uap.WamBroker
{
    internal class WamProxy : IWamProxy
    {
        private readonly ILoggerAdapter _logger;
        private readonly SynchronizationContext _synchronizationContext;

        public WamProxy(ILoggerAdapter logger, System.Threading.SynchronizationContext synchronizationContext)
        {
            _logger = logger;
            _synchronizationContext = synchronizationContext;
        }

        public async Task<IWebTokenRequestResultWrapper> GetTokenSilentlyAsync(WebAccount webAccount, WebTokenRequest webTokenRequest)
        {
            using (_logger.LogBlockDuration("WAM:GetTokenSilentlyAsync:webAccount"))
            {
                _logger.VerbosePii(() => webTokenRequest.ToLogString(true), () => webTokenRequest.ToLogString(false));
                _logger.VerbosePii(() => webAccount.ToLogString(true), () => webAccount.ToLogString(false));

                var wamResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest, webAccount);
                return new WebTokenRequestResultWrapper(wamResult);
            }
        }

        public async Task<IWebTokenRequestResultWrapper> GetTokenSilentlyForDefaultAccountAsync(WebTokenRequest webTokenRequest)
        {
            using (_logger.LogBlockDuration("WAM:GetTokenSilentlyAsync:"))
            {
                _logger.VerbosePii(() => webTokenRequest.ToLogString(true), () => webTokenRequest.ToLogString(false));

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
                _logger.VerbosePii(() => webTokenRequest.ToLogString(true), () => webTokenRequest.ToLogString(false));
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

                _logger.VerbosePii(() => webTokenRequest.ToLogString(true), () => webTokenRequest.ToLogString(false));
                _logger.VerbosePii(() => wamAccount.ToLogString(true), () => wamAccount.ToLogString(false));

                // UWP requires being on the UI thread
                await _synchronizationContext;

                WebTokenRequestResult wamResult = await WebAuthenticationCoreManager.RequestTokenAsync(
                    webTokenRequest,
                    wamAccount);

                return new WebTokenRequestResultWrapper(wamResult);
            }
        }

        public async Task<WebAccount> FindAccountAsync(WebAccountProvider provider, string wamAccountId)
        {
            using (_logger.LogBlockDuration("WAM:FindAccountAsync:"))
            {
                _logger.VerbosePii(() => provider.ToLogString(true), () => provider.ToLogString(false));
                return await WebAuthenticationCoreManager.FindAccountAsync(provider, wamAccountId);
            }
        }

        public async Task<IReadOnlyList<WebAccount>> FindAllWebAccountsAsync(WebAccountProvider provider, string clientID)
        {
            using (_logger.LogBlockDuration("WAM:FindAllWebAccountsAsync:"))
            {
                _logger.VerbosePii(() => provider.ToLogString(true), () => provider.ToLogString(false));

                // Win 10 RS3 release and above
                if (!ApiInformation.IsMethodPresent(
                   "Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager",
                   "FindAllAccountsAsync"))
                {
                    _logger.Info("[WamProxy] FindAllAccountsAsync method does not exist (it was introduced in Win 10 RS3). " +
                        "Returning 0 broker accounts. ");
                    return Enumerable.Empty<WebAccount>().ToList();
                }

                return await LegacyOsWamProxy.FindAllAccountsAsync(provider, clientID, _logger).ConfigureAwait(false);
            }
        }

        public bool TryGetAccountProperty(WebAccount webAccount, string propertyName, out string propertyValue)
        {
            return webAccount.Properties.TryGetValue("Authority", out propertyValue);
        }
    }
}

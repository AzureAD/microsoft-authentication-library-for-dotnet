// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Broker
{
    /// <summary>
    /// For platforms that do not support a broker
    /// </summary>
    internal class NullBroker : IBroker
    {
        private readonly ICoreLogger _logger;

        public NullBroker(ICoreLogger logger)
        {
            _logger = logger ?? new NullLogger();
        }

        public bool IsBrokerInstalledAndInvokable()
        {
            _logger.Info("NullLogger - acting as not installed.");
            return false;
        }      

        public Task<MsalTokenResponse> AcquireTokenInteractiveAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            throw new PlatformNotSupportedException();
        }

        public Task<MsalTokenResponse> AcquireTokenSilentAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            _logger.Info("NullLogger - returning null on silent request.");
            return null;
        }

        public void HandleInstallUrl(string appLink)
        {
            throw new PlatformNotSupportedException();
        }     

        public Task RemoveAccountAsync(ApplicationConfiguration appConfig, IAccount account)
        {
            _logger.Info("NullLogger::RemoveAccountAsync - NOP.");
            return Task.Delay(0); // nop
        }

        public Task<MsalTokenResponse> AcquireTokenSilentDefaultUserAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            _logger.Info("NullLogger - returning null on silent request.");
            return null;
        }

        Task<IReadOnlyList<IAccount>> IBroker.GetAccountsAsync(
            string clientID, 
            string redirectUri,             
            AuthorityInfo authorityInfo,
            ICacheSessionManager cacheSessionManager, 
            IInstanceDiscoveryManager instanceDiscoveryManager)
        {
            _logger.Info("NullLogger - returning empty list on GetAccounts request.");
            return Task.FromResult<IReadOnlyList<IAccount>>(new List<IAccount>()); // nop
        }
    }
}

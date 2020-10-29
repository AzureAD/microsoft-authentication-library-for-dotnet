// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
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
        public bool IsBrokerInstalledAndInvokable()
        {
            return false;
        }      

        public Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters, 
            BrokerAcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            throw new NotImplementedException();
        }

        public Task<MsalTokenResponse> AcquireTokenSilentAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            throw new NotImplementedException();
        }

        public void HandleInstallUrl(string appLink)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID, string redirectUri)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAccountAsync(IApplicationConfiguration appConfig, IAccount account)
        {
            throw new NotImplementedException();
        }
    }
}

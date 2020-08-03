// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Broker
{
    /// <summary>
    /// For platforms that do not support a broker (net desktop, net core, UWP, netstandard)
    /// </summary>
    internal class NullBroker : IBroker
    {
        public bool IsBrokerInstalledAndInvokable()
        {
            return false;
        }      

        public Task RemoveAccountAsync(string clientID, IAccount account)
        {
            throw new NotImplementedException(MsalErrorMessage.BrokerNotSupportedOnThisPlatform);
        }

        public Task<MsalTokenResponse> AcquireTokenInteractiveAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
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
    }
}

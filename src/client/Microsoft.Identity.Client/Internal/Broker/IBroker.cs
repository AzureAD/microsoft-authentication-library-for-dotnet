// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Broker
{
    internal interface IBroker
    {
        bool IsBrokerInstalledAndInvokable();
        
        Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters);

        Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters);

        Task<MsalTokenResponse> AcquireTokenSilentDefaultUserAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters);

        /// <summary>
        /// If device auth is required but the broker is not enabled, AAD will
        /// signal this by returning an URL pointing to the broker app that needs to be installed.
        /// </summary>
        void HandleInstallUrl(string appLink);

        //These methods are only available to brokers that have the BrokerSupportsSilentFlow flag enabled
        #region Silent Flow Methods
        Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID, string redirectUri);

        Task RemoveAccountAsync(IAppConfigInternal appConfig, IAccount account);
        #endregion Silent Flow Methods
    }
}

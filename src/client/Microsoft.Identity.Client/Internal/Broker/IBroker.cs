// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Broker
{
    internal interface IBroker
    {
        bool CanInvokeBroker();

        Task<MsalTokenResponse> AcquireTokenUsingBrokerAsync(Dictionary<string, string> brokerPayload);

        //These methods are only available to brokers that have the BrokerSupportsSilentFlow flag enabled
        #region Silent Flow Methods
        Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID);

        void RemoveAccountAsync(string clientID, IAccount account);
        #endregion Silent Flow Methods
    }
}

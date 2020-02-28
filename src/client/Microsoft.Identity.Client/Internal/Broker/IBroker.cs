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

        //This method is only available to brokers that have the BrokerSupportsSilentFlow flag enabled
        Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID);
    }
}

// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class BrokerInteractiveRequest : RequestBase
    {
        BrokerFactory brokerFactory = new BrokerFactory();
        IBroker BrokerHelper;
        AcquireTokenByBrokerParameters _brokerParameters;

        public BrokerInteractiveRequest(
           IServiceBundle serviceBundle,
           AuthenticationRequestParameters authenticationRequestParameters,
           AcquireTokenByBrokerParameters brokerParameters)
           : base(serviceBundle, authenticationRequestParameters, brokerParameters)
        {
            BrokerHelper = brokerFactory.CreateBrokerFacade(ServiceBundle.DefaultLogger);
            _brokerParameters = brokerParameters;

            _brokerParameters.BrokerPayload.Add(BrokerParameter.Authority, authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority);
            string scopes = ScopeHelper.ConvertSortedSetScopesToString(authenticationRequestParameters.Scope);

            _brokerParameters.BrokerPayload.Add(BrokerParameter.RequestScopes, scopes);
            _brokerParameters.BrokerPayload.Add(BrokerParameter.ClientId, authenticationRequestParameters.ClientId);
            _brokerParameters.BrokerPayload.Add(BrokerParameter.CorrelationId, ServiceBundle.DefaultLogger.CorrelationId.ToString());
            _brokerParameters.BrokerPayload.Add(BrokerParameter.ClientVersion, MsalIdHelper.GetMsalVersion());
            _brokerParameters.BrokerPayload.Add(BrokerParameter.Force, "NO");
            _brokerParameters.BrokerPayload.Add(BrokerParameter.RedirectUri, authenticationRequestParameters.RedirectUri.AbsoluteUri);

            //string extraQP = string.Join("&", authenticationRequestParameters.ExtraQueryParameters.Select(x => x.Key + "=" + x.Value.ToString()));
            //_brokerParameters.BrokerPayload.Add(BrokerParameter.ExtraQp, extraQP);
            _brokerParameters.BrokerPayload.Add(BrokerParameter.Username, authenticationRequestParameters.Account?.Username ?? string.Empty);
            _brokerParameters.BrokerPayload.Add(BrokerParameter.ExtraOidcScopes, BrokerParameter.OidcScopesValue);
        }


        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            var msalTokenResponse = await BrokerHelper.AcquireTokenUsingBrokerAsync(_brokerParameters.BrokerPayload, ServiceBundle).ConfigureAwait(false);

            return CacheTokenResponseAndCreateAuthenticationResult(msalTokenResponse);
        }
    }
}
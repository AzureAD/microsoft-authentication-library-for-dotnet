// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal interface IWamPlugin
    {
        Task<IReadOnlyList<IAccount>> GetAccountsAsync(
            string clientID,
            AuthorityInfo authorityInfo,
            Cache.ICacheSessionManager cacheSessionManager, 
            Instance.Discovery.IInstanceDiscoveryManager instanceDiscoveryManager);

        Task<WebTokenRequest> CreateWebTokenRequestAsync(
            WebAccountProvider provider,
            AuthenticationRequestParameters authenticationRequestParameters,
            bool isForceLoginPrompt,
            bool isInteractive,
            bool isAccountInWam, 
            string scopeOverride=null);

        Task<WebTokenRequest> CreateWebTokenRequestAsync(
            WebAccountProvider provider,
            string clientId,
            string scopes);

        MsalTokenResponse ParseSuccessfullWamResponse(
            WebTokenResponse webTokenResponse, 
            out Dictionary<string, string> allProperties);

        Tuple<string, string, bool> MapTokenRequestError(
            WebTokenRequestStatus status, 
            uint errorCode, 
            bool isInteractive);

        string GetHomeAccountIdOrNull(WebAccount webAccount);
    }
}

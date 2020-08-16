// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
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
        Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID);

        Task<WebTokenRequest> CreateWebTokenRequestAsync(
            WebAccountProvider provider,
            bool isInteractive,
            bool isAccountInWam,
            AuthenticationRequestParameters authenticationRequestParameters);

        MsalTokenResponse ParseSuccesfullWamResponse(WebTokenResponse webTokenResponse);

        string MapTokenRequestError(WebTokenRequestStatus status, uint errorCode, bool isInteractive);
        string GetHomeAccountIdOrNull(WebAccount webAccount);
    }
}

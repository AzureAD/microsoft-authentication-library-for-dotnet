using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    internal interface IWamPlugin
    {
        Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID);
        string GetHomeAccountIdOrNull(WebAccount webAccount);
        Task<WebAccount> FindWamAccountForMsalAccountAsync(WebAccountProvider provider, IAccount account, string loginHint, string clientId);
        Task<WebAccountProvider> GetAccountProviderAsync(string tenant = "organizations");

    
        WebTokenRequest CreateWebTokenRequest(
            WebAccountProvider provider, 
            bool isInteractive,
            bool isAccountInWam,
            AuthenticationRequestParameters authenticationRequestParameters);
        MsalTokenResponse ParseSuccesfullWamResponse(WebTokenResponse webTokenResponse);
        string MapTokenRequestError(WebTokenRequestStatus status, uint errorCode, bool isInteractive);
    }
}

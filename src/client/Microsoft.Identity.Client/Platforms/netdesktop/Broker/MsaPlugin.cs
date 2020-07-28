using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    internal class MsaPlugin : IWamPlugin
    {
        public Task<MsalTokenResponse> AcquireTokenSilentAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            throw new NotImplementedException();
        }

        public WebTokenRequest CreateWebTokenRequest(WebAccountProvider provider, bool isInteractive, bool isAccountInWam, AuthenticationRequestParameters authenticationRequestParameters)
        {
            throw new NotImplementedException();
        }

        public Task<WebAccount> FindWamAccountForMsalAccountAsync(WebAccountProvider provider, IAccount account, string loginHint, string clientId)
        {
            throw new NotImplementedException();
        }

        public Task<WebAccountProvider> GetAccountProviderAsync(string tenant)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID)
        {
            return Task.FromResult(Enumerable.Empty<IAccount>());
        }

        public string GetHomeAccountIdOrNull(WebAccount webAccount)
        {
            throw new NotImplementedException();
        }

        public string MapTokenRequestError(WebTokenRequestStatus status, uint errorCode, bool isInteractive)
        {
            throw new NotImplementedException();
        }

        public MsalTokenResponse ParseSuccesfullWamResponse(WebTokenResponse webTokenResponse)
        {
            throw new NotImplementedException();
        }
    }


}

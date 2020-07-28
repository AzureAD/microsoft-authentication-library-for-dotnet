using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    internal class WamProxy

    {
        private readonly WebAccountProvider _webAccountProvider;
        private readonly ICoreLogger _logger;

        public WamProxy(WebAccountProvider webAccountProvider, ICoreLogger logger)
        {
            _webAccountProvider = webAccountProvider;
            _logger = logger;
        }

       
        public async Task<IEnumerable<WebAccount>> FindAllWebAccountsAsync(string clientID)
        {
            // Win 10 RS3 release and above
            if (!ApiInformation.IsMethodPresent(
               "Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager",
               "FindAllAccountsAsync"))
            {
                _logger.Info("[WamProxy] FindAllAccountsAsync method does not exist (it was introduced in Win 10 RS3). " +
                    "Returning 0 broker accounts. ");
                return Enumerable.Empty<WebAccount>();
            }

            FindAllAccountsResult findResult = await WebAuthenticationCoreManager.FindAllAccountsAsync(_webAccountProvider, clientID);

            if (findResult.Status != FindAllWebAccountsStatus.Success)
            {
                var error = findResult.ProviderError;

                // TODO: bogavril - exceptions vs silent failures
                _logger.Error($"[WAM Proxy] WebAuthenticationCoreManager.FindAllAccountsAsync failed " +
                    $" with error code {error.ErrorCode} error message {error.ErrorMessage} and status {findResult.Status}");

                return Enumerable.Empty<WebAccount>();
            }

            _logger.Info($"[WAM Proxy] FindAllWebAccountsAsync returning {findResult.Accounts.Count()} WAM accounts");
            return findResult.Accounts;
        }
    }
}

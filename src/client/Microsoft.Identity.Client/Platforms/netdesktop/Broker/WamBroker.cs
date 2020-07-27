using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    //TODO: bogavril - C++ impl catches all exceptions and emits telemetry - consider the same?
    internal class WamBroker : IBroker
    {
        private readonly IBroker _aadProvider;
        private readonly IBroker _msaProvider;


        private CoreUIParent _uiParent;
        private ICoreLogger _logger;


        public WamBroker(CoreUIParent uiParent, ICoreLogger logger)
        {

            _uiParent = uiParent;
            _logger = logger;

            _aadProvider = new WamAadProvider(_logger, _uiParent);
            _msaProvider = new WamMsaProvider();

        }

        public Task<MsalTokenResponse> AcquireTokenInteractiveAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            throw new NotImplementedException();
        }

        public Task<MsalTokenResponse> AcquireTokenSilentAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID, string redirectUri)
        {
            
            if (!ApiInformation.IsMethodPresent(
                "Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager", 
                "FindAllAccountsAsync"))
            {
                _logger.Info("WAM::FindAllAccountsAsync method does not exist. Returning 0 broker accounts. ");
                return Enumerable.Empty<IAccount>();
            }
            
            var aadAccounts = await _aadProvider.GetAccountsAsync(clientID, redirectUri).ConfigureAwait(false);
            var msaAccounts = await _msaProvider.GetAccountsAsync(clientID, redirectUri).ConfigureAwait(false);

            return aadAccounts.Concat(msaAccounts);
        }

        public void HandleInstallUrl(string appLink)
        {
            throw new NotImplementedException();
        }

        public bool IsBrokerInstalledAndInvokable()
        {
            throw new NotImplementedException();
        }

        public Task RemoveAccountAsync(string clientID, IAccount account)
        {
            throw new NotImplementedException();
        }

       
    }

    internal class WamAadProvider : IBroker
    {
        private ICoreLogger _logger;
        private CoreUIParent _uiParent;

        public WamAadProvider(ICoreLogger logger, CoreUIParent uiParent)
        {
            _logger = logger;
            _uiParent = uiParent;
        }

        public Task<MsalTokenResponse> AcquireTokenInteractiveAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            throw new NotImplementedException();
        }

        public Task<MsalTokenResponse> AcquireTokenSilentAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID, string redirectUri)
        {
            var provider = await GetAccountProviderAsync().ConfigureAwait(false);
            FindAllAccountsResult findResult = await WebAuthenticationCoreManager.FindAllAccountsAsync(provider, clientID);

            if (findResult.Status != FindAllWebAccountsStatus.Success)
            {
                var error = findResult.ProviderError;

                // TODO: bogavril - exceptions vs silent failures
                _logger.Error($"[WAM AAD Provider] WebAuthenticationCoreManager.FindAllAccountsAsync failed " +
                    $" with error code {error.ErrorCode} error message { error.ErrorMessage} and status {findResult.Status}");
            }

            return findResult.Accounts
                .Select(webAcc => ConvertToMsalAccountOrNull(webAcc))
                .Where(a => a != null)
                .ToList();
        }

      

        public void HandleInstallUrl(string appLink)
        {
            throw new NotImplementedException();
        }

        public bool IsBrokerInstalledAndInvokable()
        {
            throw new NotImplementedException();
        }

        public Task RemoveAccountAsync(string clientID, IAccount account)
        {
            throw new NotImplementedException();
        }

        private async Task<WebAccountProvider> GetAccountProviderAsync()
        {
            return await GetAccountProviderAsync("organizations").ConfigureAwait(false);
        }

        private async Task<WebAccountProvider> GetAccountProviderAsync(string tenant)
        {
            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                "https://login.microsoft.com", // TODO bogavril: what about other clouds?
               tenant);

            return provider;
        }


        private Account ConvertToMsalAccountOrNull(WebAccount webAccount)
        {
            IReadOnlyDictionary<string, string> properties = webAccount.Properties;
            string username = webAccount.UserName;
            string wamId = webAccount.Id; //TODO: bogavril - needed?

            if (!webAccount.Properties.TryGetValue("TenantId", out string tenantId))
            {
                _logger.WarningPii(
                    $"Could not convert the WAM account {webAccount.UserName} (id: {webAccount.Id}) to an MSAL account because the tenant ID could not be found",
                    $"Could not convert the WAM account {webAccount.Id} to an MSAL account because the tenant ID could not be found");
                return null;
            }

            if (!webAccount.Properties.TryGetValue("Authority", out string authority))
            {
                _logger.WarningPii(
                    $"Could not convert the WAM account {webAccount.UserName} (id: {webAccount.Id}) to an MSAL account because the Authority could not be found",
                    $"Could not convert the WAM account {webAccount.Id} to an MSAL account because the Authority could not be found");

                return null;
            }

            if (!webAccount.Properties.TryGetValue("OID", out string oid))
            {
                _logger.WarningPii(
                    $"Could not convert the WAM account {webAccount.UserName} (id: {webAccount.Id}) to an MSAL account because the OID could not be found",
                    $"Could not convert the WAM account {webAccount.Id} to an MSAL account because the OID could not be found");

                return null;
            }

            string environment = (new Uri(authority)).Host;


            // TODO bogavril - this task was copied from C++ implementation and may not be relevant for MSAL .net
            // AAD WAM plugin returns both guest and home accounts as part of FindAllAccountAsync call.
            // We will need to de-dupe WAM accounts before writing them to MSAL cache.
            string homeAccountId = oid + "." + tenantId;

            var msalAccount = new Account(homeAccountId, username, environment);
            return msalAccount;
        }

    }


   

    internal class WamMsaProvider : IBroker
    {
        public Task<MsalTokenResponse> AcquireTokenInteractiveAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            throw new NotImplementedException();
        }

        public Task<MsalTokenResponse> AcquireTokenSilentAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID, string redirectUri)
        {
            return Task.FromResult(Enumerable.Empty<IAccount>());
        }

        public void HandleInstallUrl(string appLink)
        {
            throw new NotImplementedException();
        }

        public bool IsBrokerInstalledAndInvokable()
        {
            throw new NotImplementedException();
        }

        public Task RemoveAccountAsync(string clientID, IAccount account)
        {
            throw new NotImplementedException();
        }
    }

   
}

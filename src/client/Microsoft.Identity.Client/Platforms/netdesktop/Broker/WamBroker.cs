using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    //TODO: bogavril - C++ impl catches all exceptions and emits telemetry - consider the same?
    internal class WamBroker : IBroker
    {
        private readonly IBroker _aadProvider;
        private readonly IBroker _msaProvider;

        public WamBroker()
        {
            _aadProvider = new WamAadProvider();
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

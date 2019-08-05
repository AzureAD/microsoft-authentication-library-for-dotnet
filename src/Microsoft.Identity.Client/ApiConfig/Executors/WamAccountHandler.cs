// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_WAM

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.UI.ApplicationSettings;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal class WamAccountHandler : IDisposable
    {
        private const string MicrosoftProviderId = "https://login.microsoft.com";
        private const string MicrosoftAccountAuthority = "consumers";
        private const string AzureActiveDirectoryAuthority = "organizations";

        private WebAccountProviderCommand _webAccountProviderCommand;
        private ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

        public WamAccountHandler()
        {
            AccountsSettingsPane.GetForCurrentView().AccountCommandsRequested += OnAccountCommandsRequested;
        }

        public async Task<WebAccountProviderCommand> ExecuteAsync()
        {
            _manualResetEvent.Reset();
            AccountsSettingsPane.Show();

            while (!_manualResetEvent.WaitOne(10))
            {
                await Task.Delay(100).ConfigureAwait(true);
            }

            return _webAccountProviderCommand;
        }

        private async void OnAccountCommandsRequested(
            AccountsSettingsPane sender,
            AccountsSettingsPaneCommandsRequestedEventArgs e)
        {
            // In order to make async calls within this callback, the deferral object is needed
            AccountsSettingsPaneEventDeferral deferral = e.GetDeferral();

            await AddWebAccountProvidersToPaneAsync(e).ConfigureAwait(true);
            deferral.Complete();
        }

        private async Task AddWebAccountProvidersToPaneAsync(AccountsSettingsPaneCommandsRequestedEventArgs e)
        {
            // The order of providers displayed is determined by the order provided to the Accounts pane
            List<WebAccountProvider> providers = await GetAllProvidersAsync().ConfigureAwait(true);

            foreach (WebAccountProvider provider in providers)
            {
                WebAccountProviderCommand providerCommand = new WebAccountProviderCommand(provider, WebAccountProviderCommandInvoked);
                e.WebAccountProviderCommands.Add(providerCommand);
            }
        }

        private void WebAccountProviderCommandInvoked(WebAccountProviderCommand command)
        {
            _webAccountProviderCommand = command;
            _manualResetEvent.Set();

            //if ((command.WebAccountProvider.Id == MicrosoftProviderId) && (command.WebAccountProvider.Authority == MicrosoftAccountAuthority))
            //{
            //    // ClientID is ignored by MSA
            //    // await AuthenticateWithRequestToken(command.WebAccountProvider, MicrosoftAccountScopeRequested, MicrosoftAccountClientId);
            //}
        }

        private async Task<List<WebAccountProvider>> GetAllProvidersAsync()
        {
            List<WebAccountProvider> providers = new List<WebAccountProvider>
            {
                await GetProviderAsync(MicrosoftProviderId, MicrosoftAccountAuthority).ConfigureAwait(true),
                await GetProviderAsync(MicrosoftProviderId, AzureActiveDirectoryAuthority).ConfigureAwait(true)
            };

            return providers;
        }

        private async Task<WebAccountProvider> GetProviderAsync(string providerId, string authorityId = "")
        {
            return await WebAuthenticationCoreManager.FindAccountProviderAsync(providerId, authorityId);
        }

        #region IDisposable Support
        private bool _disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    AccountsSettingsPane.GetForCurrentView().AccountCommandsRequested -= OnAccountCommandsRequested;
                    _manualResetEvent.Dispose();
                    _manualResetEvent = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}

#endif // SUPPORTS_WAM

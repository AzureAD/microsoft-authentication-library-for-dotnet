using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Platforms.net45;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.UI.ApplicationSettings;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    internal class AccountPicker
    {
        private readonly IntPtr _parentHandle;
        private readonly ICoreLogger _logger;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly Authority _authority;
        private readonly bool _isMsaPassthrough;
        private volatile WebAccountProvider _provider;


        public AccountPicker(
            IntPtr parentHandle, 
            ICoreLogger logger, 
            SynchronizationContext synchronizationContext, 
            Authority authority, 
            bool isMsaPassthrough)
        {
            _parentHandle = parentHandle;
            _logger = logger;
            _synchronizationContext = synchronizationContext;
            _authority = authority;
            _isMsaPassthrough = isMsaPassthrough;
        }

        public async Task<WebAccountProvider> DetermineAccountInteractivelyAsync()
        {
            WebAccountProvider result = null;

            if (_synchronizationContext == null)
            {
                throw new MsalClientException("wam_ui_thread_only");
            }

            // at this point we should be back on the ui thread
            await _synchronizationContext;

            result = await ShowPickerAsync().ConfigureAwait(true);

            return result;
        }

        private async Task<WebAccountProvider> ShowPickerAsync()
        {
            AccountsSettingsPane retaccountPane = null;
            try
            {
                retaccountPane = AccountsSettingsPaneInterop.GetForWindow(_parentHandle);
                retaccountPane.AccountCommandsRequested += Authenticator_AccountCommandsRequested;
                await AccountsSettingsPaneInterop.ShowAddAccountForWindowAsync(_parentHandle);


                return _provider;
            }
            catch (Exception e)
            {
                _logger.ErrorPii(e);
                throw;
            }
            finally
            {
                if (retaccountPane != null)
                {
                    retaccountPane.AccountCommandsRequested -= Authenticator_AccountCommandsRequested;
                }
            }
        }

        private async void Authenticator_AccountCommandsRequested(
            AccountsSettingsPane sender,
            AccountsSettingsPaneCommandsRequestedEventArgs e)
        {
            AccountsSettingsPaneEventDeferral deferral = null;
            try
            {
                deferral = e.GetDeferral();

                if (string.Equals("common", _authority.TenantId) || _isMsaPassthrough )
                {
                    _logger.Verbose("Displaying selector for common");
                    e.WebAccountProviderCommands.Add(
                        new WebAccountProviderCommand(
                            await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", "consumers"),
                            WebAccountProviderCommandInvoked));

                    e.WebAccountProviderCommands.Add(
                        new WebAccountProviderCommand(
                            await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", "organizations"),
                            WebAccountProviderCommandInvoked));
                }
                else if (string.Equals("organizations", _authority.TenantId))
                {
                    _logger.Verbose("Displaying selector for organizations");

                    e.WebAccountProviderCommands.Add(
                       new WebAccountProviderCommand(
                           await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", "organizations"),
                           WebAccountProviderCommandInvoked));
                }
                else if (string.Equals("consumers", _authority.TenantId))
                {
                    _logger.Verbose("Displaying selector for consumers");

                    e.WebAccountProviderCommands.Add(
                      new WebAccountProviderCommand(
                          await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", "consumers"),
                          WebAccountProviderCommandInvoked));

                    if (_isMsaPassthrough)
                    {
                        e.WebAccountProviderCommands.Add(
                           new WebAccountProviderCommand(
                           await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", "organizations"),
                           WebAccountProviderCommandInvoked));
                    }
                }
                else
                {
                    _logger.Verbose("Displaying selector for tenanted authority");

                    e.WebAccountProviderCommands.Add(
                        new WebAccountProviderCommand(
                            await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", _authority.AuthorityInfo.CanonicalAuthority),
                        WebAccountProviderCommandInvoked));
                }

                e.HeaderText = "Please select an account to log in with"; // TODO: this is English only, try removing it
            }
            finally
            {
                deferral?.Complete();
            }
        }

        private void WebAccountProviderCommandInvoked(WebAccountProviderCommand command)
        {
            _provider = command.WebAccountProvider;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.UI.ApplicationSettings;

#if NET5_WIN
using Microsoft.Identity.Client.Platforms.net5win;
#elif DESKTOP || NET_CORE
using Microsoft.Identity.Client.Platforms;
#endif

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal class AccountPicker : IAccountPicker
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
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _authority = authority;
            _isMsaPassthrough = isMsaPassthrough;
        }

        public async Task<WebAccountProvider> DetermineAccountInteractivelyAsync()
        {
            WebAccountProvider result = null;

            // go back to the ui thread
            await _synchronizationContext;

            result = await ShowPickerAsync().ConfigureAwait(true);

            return result;
        }

        private async Task<WebAccountProvider> ShowPickerAsync()
        {
            AccountsSettingsPane retaccountPane = null;
            try
            {
#if WINDOWS_APP
                retaccountPane = AccountsSettingsPane.GetForCurrentView();
                retaccountPane.AccountCommandsRequested += Authenticator_AccountCommandsRequested;
                await AccountsSettingsPane.ShowAddAccountAsync();
#else
                retaccountPane = AccountsSettingsPaneInterop.GetForWindow(_parentHandle);
                retaccountPane.AccountCommandsRequested += Authenticator_AccountCommandsRequested;
                await AccountsSettingsPaneInterop.ShowAddAccountForWindowAsync(_parentHandle);
#endif
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

                if (string.Equals("common", _authority.TenantId))
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

                //e.HeaderText = "Please select an account to log in with"; // TODO: this is English only, try removing it
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

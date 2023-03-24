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

namespace Microsoft.Identity.Client.Platforms.uap.WamBroker
{
    internal class AccountPicker : IAccountPicker
    {
        private readonly IntPtr _parentHandle;
        private readonly ILoggerAdapter _logger;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly Authority _authority;
        private readonly bool _isMsaPassthrough;
        private readonly string _optionalHeaderText;
        private volatile WebAccountProvider _provider;

        public AccountPicker(
            IntPtr parentHandle,
            ILoggerAdapter logger,
            SynchronizationContext synchronizationContext,
            Authority authority,
            bool isMsaPassthrough,
            string optionalHeaderText)
        {
            _parentHandle = parentHandle;
            _logger = logger;
            _synchronizationContext = synchronizationContext;
            _authority = authority;
            _isMsaPassthrough = isMsaPassthrough;
            _optionalHeaderText = optionalHeaderText;
            _logger.Verbose(()=>"Is MSA passthrough? " + _isMsaPassthrough);
        }

        public async Task<WebAccountProvider> DetermineAccountInteractivelyAsync()
        {
            await ShowPicker_UWPAsync().ConfigureAwait(true);
            return _provider;
        }

        private async Task<WebAccountProvider> ShowPicker_UWPAsync()
        {
            await _synchronizationContext;

            AccountsSettingsPane retaccountPane = null;
            try
            {
                retaccountPane = AccountsSettingsPane.GetForCurrentView();
                retaccountPane.AccountCommandsRequested += Authenticator_AccountCommandsRequested;
                await AccountsSettingsPane.ShowAddAccountAsync();

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
#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Authenticator_AccountCommandsRequested(
#pragma warning restore VSTHRD100 // Avoid async void methods
            AccountsSettingsPane sender,
            AccountsSettingsPaneCommandsRequestedEventArgs args)
        {
            AccountsSettingsPaneEventDeferral deferral = null;
            try
            {
                deferral = args.GetDeferral();

                if (!string.IsNullOrEmpty(_optionalHeaderText))
                {
                    args.HeaderText = _optionalHeaderText;
                }

                if (string.Equals("common", _authority.TenantId))
                {
                    _logger.Verbose(()=>"Displaying selector for common");
                    await AddSelectorsAsync(
                        args,
                        addOrgAccounts: true,
                        addMsaAccounts: true).ConfigureAwait(true);
                }
                else if (string.Equals("organizations", _authority.TenantId))
                {
                    _logger.Verbose(()=>"Displaying selector for organizations");
                    await AddSelectorsAsync(
                        args,
                        addOrgAccounts: true,
                        addMsaAccounts: _isMsaPassthrough).ConfigureAwait(true);
                }
                else if (string.Equals("consumers", _authority.TenantId))
                {
                    _logger.Verbose(() => "Displaying selector for consumers");
                    await AddSelectorsAsync(
                        args,
                        addOrgAccounts: false,
                        addMsaAccounts: true).ConfigureAwait(true);
                }
                else
                {
                    _logger.Verbose(()=>"Displaying selector for tenanted authority");
                    await AddSelectorsAsync(
                        args,
                        addOrgAccounts: true,
                        addMsaAccounts: _isMsaPassthrough,
                        tenantId: _authority.AuthorityInfo.CanonicalAuthority.ToString()).ConfigureAwait(true);
                }
            }
            finally
            {
                deferral?.Complete();
            }
        }

        private async Task AddSelectorsAsync(AccountsSettingsPaneCommandsRequestedEventArgs args, bool addOrgAccounts, bool addMsaAccounts, string tenantId = null)
        {
            if (addOrgAccounts)
            {
                args.WebAccountProviderCommands.Add(
                    new WebAccountProviderCommand(
                        await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", tenantId ?? "organizations"),
                        WebAccountProviderCommandInvoked));
            }

            if (addMsaAccounts)
            {
                args.WebAccountProviderCommands.Add(
                    new WebAccountProviderCommand(
                        await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", "consumers"),
                        WebAccountProviderCommandInvoked));
            }
        }

        private void WebAccountProviderCommandInvoked(WebAccountProviderCommand command)
        {
            _provider = command.WebAccountProvider;

        }
    }
}

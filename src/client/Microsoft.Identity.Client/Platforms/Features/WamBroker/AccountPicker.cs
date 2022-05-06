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
using System.Runtime.InteropServices;

#if !UAP10_0
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
#endif

#if NET5_WIN
using Microsoft.Identity.Client.Platforms.net5win;
using AccountsSettingsPaneInterop = Microsoft.Identity.Client.Platforms.net5win.AccountsSettingsPaneInterop;
#elif DESKTOP || NET_CORE
using Microsoft.Identity.Client.Platforms;
#endif

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
#if NET5_WIN
    [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
#endif
    internal class AccountPicker : IAccountPicker
    {
        private readonly IntPtr _parentHandle;
        private readonly ICoreLogger _logger;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly Authority _authority;
        private readonly bool _isMsaPassthrough;
        private readonly string _optionalHeaderText;
        private volatile WebAccountProvider _provider;

        public AccountPicker(
            IntPtr parentHandle,
            ICoreLogger logger,
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
            _logger.Verbose("Is MSA passthrough? " + _isMsaPassthrough);
        }

        public async Task<WebAccountProvider> DetermineAccountInteractivelyAsync()
        {
#if WINDOWS_APP
            await ShowPicker_UWPAsync().ConfigureAwait(true);                    
#else
            await ShowPicker_Win32Async().ConfigureAwait(false);
#endif
            return _provider;
        }

#if !WINDOWS_APP
        private async Task ShowPicker_Win32Async()
        {
            // if there is a sync context, move to it (go to UI thread)
            if (_synchronizationContext != null)
            {
                await _synchronizationContext;
            }

            if (UseSplashScreen())
            {
                await ShowPickerWithSplashScreenAsync().ConfigureAwait(false);
            }
            else
            {
                if (_synchronizationContext == null)
                {
                    throw new MsalClientException(
                       MsalError.WamUiThread,
                       "AcquireTokenInteractive with broker must be called from the UI thread when using the Windows broker." +
                        WamBroker.ErrorMessageSuffix);
                }

                await ShowPickerForWin32WindowAsync(_parentHandle).ConfigureAwait(false);

            }
        }

        /// <summary>
        /// Account Picker APIs do not work well with console apps because the console
        /// window belongs to a different process, which causes a security exception. 
        /// In general, if the parent window handle does not belong to the current process, 
        /// we need to take control over the window handle by injecting a splash screen.
        /// </summary>
        private bool UseSplashScreen()
        {
            if (_synchronizationContext == null)
            {
                return true;
            }

            WindowsNativeMethods.GetWindowThreadProcessId(_parentHandle, out uint windowProcessId);
            uint appProcessId = WindowsNativeMethods.GetCurrentProcessId();

            return appProcessId != windowProcessId;
        }

        /// <summary>
        /// The account picker API has bug that prevent correct usage from console apps. 
        /// To workaround, show a splash screen and attach to it.
        /// </summary>
        /// <returns></returns>
        private Task<bool> ShowPickerWithSplashScreenAsync()
        {

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

                Thread thread = new Thread(() =>
                {
                    try
                    {
                        ShowPickerWithSplashScreenImpl();
                        tcs.SetResult(true);
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                return tcs.Task;
            }
            else
            {
                ShowPickerWithSplashScreenImpl();
                return Task.FromResult(true);
            }
        }

        private void ShowPickerWithSplashScreenImpl()
        {
            var win32Window = new SplashScreen.Win32Window(_parentHandle);

            using (var splash = new win32.Splash(win32Window))
            {
                splash.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
                splash.DialogResult = System.Windows.Forms.DialogResult.OK;
                splash.TopMost = true;

                splash.Shown += async (s, e) =>
                {
                    var windowHandle = splash.Handle;
                    await ShowPickerForWin32WindowAsync(windowHandle).ConfigureAwait(true);
                    splash.Close();
                };

                try
                {
                    splash.ShowDialog(win32Window);
                }
                catch (InvalidOperationException ex)
                {
                    if (_synchronizationContext == null)
                    {
                        throw new MsalClientException(
                           MsalError.WamUiThread,
                           "AcquireTokenInteractive with broker must be called from the UI thread when using the Windows broker." +
                            WamBroker.ErrorMessageSuffix, ex);
                    }
                    throw;
                }
            }
        }

        private async Task ShowPickerForWin32WindowAsync(IntPtr windowHandle)
        {
            AccountsSettingsPane retaccountPane = null;
            try
            {
                retaccountPane = AccountsSettingsPaneInterop.GetForWindow(windowHandle);
                retaccountPane.AccountCommandsRequested += Authenticator_AccountCommandsRequested;
                await AccountsSettingsPaneInterop.ShowAddAccountForWindowAsync(windowHandle);
            }
            catch (Exception ex)
            {
                _logger.ErrorPii(ex);
                throw;
            }
            finally
            {
                if (retaccountPane != null)
                {
                    retaccountPane.AccountCommandsRequested -= Authenticator_AccountCommandsRequested;                    
                    retaccountPane = null;
                }
            }
        }

#endif

#if WINDOWS_APP

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
#endif

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
                    _logger.Verbose("Displaying selector for common");
                    await AddSelectorsAsync(
                        args, 
                        addOrgAccounts: true, 
                        addMsaAccounts: true).ConfigureAwait(true);
                }
                else if (string.Equals("organizations", _authority.TenantId))
                {
                    _logger.Verbose("Displaying selector for organizations");
                    await AddSelectorsAsync(
                        args, 
                        addOrgAccounts: true, 
                        addMsaAccounts: _isMsaPassthrough).ConfigureAwait(true);
                }
                else if (string.Equals("consumers", _authority.TenantId))
                {
                    _logger.Verbose("Displaying selector for consumers");
                    await AddSelectorsAsync(
                        args, 
                        addOrgAccounts: false, 
                        addMsaAccounts: true).ConfigureAwait(true);
                }
                else
                {
                    _logger.Verbose("Displaying selector for tenanted authority");
                    await AddSelectorsAsync(
                        args, 
                        addOrgAccounts: true, 
                        addMsaAccounts: _isMsaPassthrough, 
                        tenantId: _authority.AuthorityInfo.CanonicalAuthority).ConfigureAwait(true);
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
                args.WebAccountProviderCommands.Add(
                    new WebAccountProviderCommand(
                        await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", tenantId ?? "organizations"),
                        WebAccountProviderCommandInvoked));

            if (addMsaAccounts)
                args.WebAccountProviderCommands.Add(
                    new WebAccountProviderCommand(
                        await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", "consumers"),
                        WebAccountProviderCommandInvoked));
        }

        private void WebAccountProviderCommandInvoked(WebAccountProviderCommand command)
        {
            _provider = command.WebAccountProvider;

        }
    }
}

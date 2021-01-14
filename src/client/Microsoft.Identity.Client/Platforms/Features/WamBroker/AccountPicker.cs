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
using Microsoft.Identity.Client.PlatformsCommon.Shared;

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
            _synchronizationContext = synchronizationContext;
            _authority = authority;
            _isMsaPassthrough = isMsaPassthrough;
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
            // if there is a sync context, move to it (go to ui thread)
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

            using (var splash = new Microsoft.Identity.Client.Platforms.Features.WamBroker.win32.Splash())
            {
                splash.DialogResult = System.Windows.Forms.DialogResult.OK;
                splash.Shown += async (s, e) =>
                {
                    var windowHandle = splash.Handle;
                    await ShowPickerForWin32WindowAsync(windowHandle).ConfigureAwait(true);
                    splash.Close();
                };

                var win32Window = new Microsoft.Identity.Client.Platforms.Features.WamBroker.SplashScreen.Win32Window(_parentHandle);

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

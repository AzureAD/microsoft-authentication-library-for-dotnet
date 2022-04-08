// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using System.Diagnostics;

#if !UAP10_0
using Microsoft.Identity.Client.NativeInterop;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.Utils.Windows;

#endif

#if DESKTOP || NET5_WIN
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Runtime.Versioning;
#endif

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    /// <summary>
    /// Important: all the WAM code has Win10 specific types and MUST be guarded against
    /// usage on older Windows, Mac and Linux, otherwise TypeLoadExceptions occur
    /// </summary>
#if NET5_WIN
    [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
#endif
    internal class RuntimeBroker : IBroker
    {
        private readonly ICoreLogger _logger;
        private readonly IntPtr _parentHandle;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly IMsaPassthroughHandler _msaPassthroughHandler;
        internal const string ErrorMessageSuffix = " For more details see https://aka.ms/msal-net-wam";
        private const string InfrastructureTenant = "f8cdef31-a31e-4b4a-93e4-5f571e91255a";
        private readonly WindowsBrokerOptions _wamOptions;

        /// <summary>
        /// Ctor. Only call if on Win10, otherwise a TypeLoadException occurs. See DesktopOsHelper.IsWin10
        /// </summary>
        public RuntimeBroker(
            CoreUIParent uiParent,
            ApplicationConfiguration appConfig,
            ICoreLogger logger,
            IMsaPassthroughHandler msaPassthroughHandler = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _synchronizationContext = uiParent?.SynchronizationContext;

            _parentHandle = GetParentWindow(uiParent);

            _wamOptions = appConfig.WindowsBrokerOptions ??
                WindowsBrokerOptions.CreateDefault();
        }

        /// <summary>
        /// AcquireTokenInteractiveAsync
        /// </summary>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="acquireTokenInteractiveParameters"></param>
        /// <returns></returns>
        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
#if WINDOWS_APP
            if (_synchronizationContext == null)
            {
                throw new MsalClientException(
                    MsalError.WamUiThread,
                    "AcquireTokenInteractive with broker must be called from the UI thread when using WAM." +
                     ErrorMessageSuffix);
            }
#endif

            _logger.Verbose("[WamBroker] Using Windows account picker.");

            return await AcquireInteractiveWithPickerAsync(authenticationRequestParameters)
                .ConfigureAwait(false);
        }


        /// <summary>
        /// AcquireInteractiveWithPickerAsync
        /// </summary>
        /// <param name="authenticationRequestParameters"></param>
        /// <returns></returns>
        /// <exception cref="MsalClientException"></exception>
        /// <exception cref="MsalServiceException"></exception>
        private async Task<MsalTokenResponse> AcquireInteractiveWithPickerAsync(AuthenticationRequestParameters authenticationRequestParameters)
        {
#if !UAP10_0
            bool isMsaPassthrough = _wamOptions.MsaPassthrough;
            var authority = authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority;
            MsalTokenResponse tokenResponse = null;

            NativeInterop.AuthResult result = null;

            try
            {
                if (WindowsNativeUtils.IsElevatedUser())
                {
                    _logger.Verbose($"[WamBroker] Current user is the member of the admin group. WAM should still work.");
                }

                using (var core = new NativeInterop.Core())
                using (var authParams = new NativeInterop.AuthParameters(authenticationRequestParameters.AppConfig.ClientId, authority))
                {
                    authParams.RequestedScopes = string.Join(" ", authenticationRequestParameters.Scope);
                    authParams.RedirectUri = authenticationRequestParameters.RedirectUri.ToString();

                    using (result = await core.SignInInteractivelyAsync(_parentHandle, authParams, authenticationRequestParameters.CorrelationId.ToString("D")).ConfigureAwait(false))
                    {
                        if (result.IsSuccess)
                        {
                            tokenResponse = ParseWamResponse(result, authenticationRequestParameters, out _);
                            _logger.Verbose("[WamBroker] Successfully retrieved token.");

                        }
                        else
                        {
                            _logger.Verbose($"[WamBroker] Could not get the account provider. {result.Error}");
                            throw new MsalServiceException(MsalError.WamPickerError, $"Could not get the account provider - account picker. {result.Error}");
                        }
                    }
                }
            }
            catch (System.Exception ex) when (!(ex is MsalException))
            {
                _logger.ErrorPii(ex);
                throw new MsalServiceException(
                    MsalError.WamPickerError,
                    "Could not get the account provider - account picker. See inner exception for details", ex);
            }

            return tokenResponse;
#endif

            throw new NotImplementedException();
        }
#if !UAP10_0
        /// <summary>
        /// ParseSuccessfullWamResponse
        /// </summary>
        /// <param name="authResult"></param>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="allProperties"></param>
        /// <returns></returns>
        public MsalTokenResponse ParseWamResponse(
                NativeInterop.AuthResult authResult, AuthenticationRequestParameters authenticationRequestParameters,
                out Dictionary<string, string> allProperties)
        {
            allProperties = new Dictionary<string, string>(8, StringComparer.OrdinalIgnoreCase);

            string expiresOn = authResult.ExpiresOn.ToString();

            string extendedExpiresOn = authResult.ExpiresOn.ToString();

            string correlationId = authenticationRequestParameters.CorrelationId.ToString("D");

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                _logger.Warning("No correlation ID in response");
                correlationId = null;
            }

            MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
            {
                AccessToken = authResult.AccessToken,
                IdToken = authResult.IdToken,
                CorrelationId = correlationId,
                Scope = authResult.GrantedScopes,
                ExpiresIn = DateTimeHelpers.GetDurationFromWindowsTimestamp(expiresOn, _logger),
                ExtendedExpiresIn = DateTimeHelpers.GetDurationFromWindowsTimestamp(extendedExpiresOn, _logger),
                ClientInfo = authResult.Account.ClientInfo.ToString(),
                TokenType = "Bearer",
                WamAccountId = authResult.Account.Id,
                TokenSource = TokenSource.Broker
            };

            return msalTokenResponse;

        }
#endif

        /// <summary>
        /// Gets the window handle
        /// </summary>
        /// <param name="uiParent"></param>
        /// <returns></returns>
        private IntPtr GetParentWindow(CoreUIParent uiParent)
        {
#if WINDOWS_APP
            // On UWP there is no need for a window handle
            return IntPtr.Zero;
#endif

#if DESKTOP || NET5_WIN // net core doesn't reference WinForms

            if (uiParent?.OwnerWindow is IWin32Window window)
            {
                _logger.Info("[WAM Broker] Owner window specified as IWin32Window.");
                return window.Handle;
            }
#endif

#if DESKTOP || NET5_WIN || NET_CORE

            if (uiParent?.OwnerWindow is IntPtr ptr)
            {
                _logger.Info("[WAM Broker] Owner window specified as IntPtr.");
                return ptr;
            }

            // other MSALs prefer to default to GetForegroundWindow() but this causes issues
            // for example if the user quickly switches windows.
            // GetDesktopWindow will make the default more consistent with the embedded browser
            _logger.Info("[WAM Broker] Using desktop as a parent window.");
            IntPtr desktopWindow = WindowsNativeMethods.GetDesktopWindow();
            return desktopWindow;
#endif
            throw new NotImplementedException();
        }

        /// <summary>
        /// Auth Broker Installation URL
        /// </summary>
        /// <param name="appLink"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void HandleInstallUrl(string appLink)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Check to see if broker is installed and invokabale
        /// </summary>
        /// <param name="authorityType"></param>
        /// <returns></returns>
        public bool IsBrokerInstalledAndInvokable(AuthorityType authorityType)
        {
#if NET_CORE
            if (!DesktopOsHelper.IsWindows())
            {
                _logger.Info("[WAM Broker] Not a Windows operating system. WAM broker is not available. ");
                return false;
            }
#endif
            // WAM does not work on pure ADFS environments
            if (authorityType == AuthorityType.Adfs)
            {
                _logger.Info("[WAM Broker] WAM does not work in pure ADFS environments. Falling back to browser for an ADFS authority. ");
                return false;
            }

            _logger.Info("[WAM Broker] Authority is AAD. Using WAM Broker. ");

            // WAM is present on Win 10 only
            return ApiInformation.IsMethodPresent(
                   "Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager",
                   "GetTokenSilentlyAsync");
        }

        /// <summary>
        /// AcquireTokenSilentAsync
        /// </summary>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="acquireTokenSilentParameters"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(AuthenticationRequestParameters authenticationRequestParameters, 
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            return await AcquireTokenSilentlyAsync(authenticationRequestParameters, acquireTokenSilentParameters).ConfigureAwait(false);
        }

        /// <summary>
        /// AcquireTokenSilentDefaultUserAsync
        /// </summary>ter
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="acquireTokenSilentParameters"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<MsalTokenResponse> AcquireTokenSilentDefaultUserAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            return await AcquireTokenSilentlyAsync(authenticationRequestParameters, acquireTokenSilentParameters).ConfigureAwait(false);
        }

        /// <summary>
        /// AcquireTokenSilentDefaultUserAsync
        /// </summary>ter
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="acquireTokenSilentParameters"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<MsalTokenResponse> AcquireTokenSilentlyAsync(AuthenticationRequestParameters authenticationRequestParameters, 
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            MsalTokenResponse msalTokenResponse = null;

#if !UAP10_0
            NativeInterop.Account account = null;

            try
            {
                using (var core = new NativeInterop.Core())
                using (var authParams = new NativeInterop.AuthParameters(authenticationRequestParameters.AppConfig.ClientId, authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority))
                {
                    authParams.RequestedScopes = string.Join(" ", authenticationRequestParameters.Scope);
                    authParams.RedirectUri = authenticationRequestParameters.RedirectUri.ToString();

                    using (account = await core.ReadAccountByIdAsync(acquireTokenSilentParameters.Account.HomeAccountId.ObjectId,
                        authenticationRequestParameters.CorrelationId.ToString("D")).ConfigureAwait(false))
                    {
                        if (account == null)
                            throw new MsalServiceException(
                                MsalError.FailedToAcquireTokenSilentlyFromBroker, "Failed to get accounts - Silent Token Acquisition with the provided account failed. ");

                        using (NativeInterop.AuthResult result = await core.AcquireTokenSilentlyAsync(authParams,
                            authenticationRequestParameters.CorrelationId.ToString("D"), account).ConfigureAwait(false))
                        {
                            if (result.IsSuccess)
                            {
                                msalTokenResponse = ParseWamResponse(result, authenticationRequestParameters, out _);

                            }
                            else
                            {
                                throw new MsalServiceException(MsalError.FailedToAcquireTokenSilentlyFromBroker, $"Failed to acquire token silently. {result.Error}");
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex) when (!(ex is MsalException))
            {
                _logger.ErrorPii(ex);
                throw new MsalServiceException(
                    MsalError.FailedToAcquireTokenSilentlyFromBroker, "Failed to acquire token silently. See inner exception for details", ex);
            }

            return msalTokenResponse;
#endif
            throw new NotImplementedException();
        }

        /// <summary>
        /// GetAccountsAsync
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="redirectUri"></param>
        /// <param name="authorityInfo"></param>
        /// <param name="cacheSessionManager"></param>
        /// <param name="instanceDiscoveryManager"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<IReadOnlyList<IAccount>> GetAccountsAsync(
            string clientID,
            string redirectUri,
            AuthorityInfo authorityInfo,
            ICacheSessionManager cacheSessionManager,
            IInstanceDiscoveryManager instanceDiscoveryManager)
        {
            return Array.Empty<IAccount>();
        }

        /// <summary>
        /// RemoveAccountAsync
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task RemoveAccountAsync(ApplicationConfiguration appConfig, IAccount account)
        {
            throw new NotImplementedException();
        }
    }
}

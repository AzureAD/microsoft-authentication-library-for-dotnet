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

namespace Microsoft.Identity.Client.Platforms.uap.WamBroker
{
    /// <summary>
    /// Important: all the WAM code has Win10 specific types and MUST be guarded against
    /// usage on older Windows, Mac and Linux, otherwise TypeLoadExceptions occur
    /// </summary>
    internal class WamBroker : IBroker
    {
        private readonly IWamPlugin _aadPlugin;
        private readonly IWamPlugin _msaPlugin;
        private readonly IWamProxy _wamProxy;
        private readonly IWebAccountProviderFactory _webAccountProviderFactory;
        private readonly IAccountPickerFactory _accountPickerFactory;
        private readonly ILoggerAdapter _logger;
        private readonly IntPtr _parentHandle;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly IMsaPassthroughHandler _msaPassthroughHandler;
        internal const string ErrorMessageSuffix = " For more details see https://aka.ms/msal-net-wam";
        private const string InfrastructureTenant = "f8cdef31-a31e-4b4a-93e4-5f571e91255a";
        private readonly WindowsBrokerOptions _wamOptions;

        public bool IsPopSupported => false;

        /// <summary>
        /// Ctor. Only call if on Win10, otherwise a TypeLoadException occurs. See DesktopOsHelper.IsWin10
        /// </summary>
        public WamBroker(
            CoreUIParent uiParent,
            ApplicationConfiguration appConfig,
            ILoggerAdapter logger,
            IWamPlugin testAadPlugin = null,
            IWamPlugin testmsaPlugin = null,
            IWamProxy wamProxy = null,
            IWebAccountProviderFactory webAccountProviderFactory = null,
            IAccountPickerFactory accountPickerFactory = null,
            IMsaPassthroughHandler msaPassthroughHandler = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _synchronizationContext = uiParent?.SynchronizationContext;

            _wamProxy = wamProxy ?? new WamProxy(_logger, _synchronizationContext);
            _parentHandle = GetParentWindow(uiParent);

            _webAccountProviderFactory = webAccountProviderFactory ?? new WebAccountProviderFactory();
            _accountPickerFactory = accountPickerFactory ?? new AccountPickerFactory();
            _aadPlugin = testAadPlugin ?? new AadPlugin(_wamProxy, _webAccountProviderFactory, _logger);
            _msaPlugin = testmsaPlugin ?? new MsaPlugin(_wamProxy, _webAccountProviderFactory, _logger);

            _msaPassthroughHandler = msaPassthroughHandler ??
                new MsaPassthroughHandler(_logger, _msaPlugin, _wamProxy, _parentHandle);

            _wamOptions = appConfig.UwpBrokerOptions ??
                WindowsBrokerOptions.CreateDefault();
        }

        /// <summary>
        /// In WAM, AcquireTokenInteractive is always associated to an account. WAM also allows for an "account picker" to be displayed,
        /// which is similar to the EVO browser experience, allowing the user to add an account or use an existing one.
        ///
        /// MSAL does not have a concept of account picker so MSAL.AcquireTokenInteractive will:
        ///
        /// 1. Call WAM.AccountPicker if an IAccount (or possibly login_hint) is not configured
        /// 2. Figure out the WAM.AccountID associated to the MSAL.Account
        /// 3. Call WAM.AcquireTokenInteractive with the WAM.AccountID
        ///
        /// To make matters more complicated, WAM has 2 plugins - AAD and MSA. With AAD plugin,
        /// it is possible to list all WAM accounts and find the one associated to the MSAL account.
        /// However, MSA plugin does NOT allow listing of accounts, and the only way to figure out the
        /// WAM account ID is to use the account picker. This makes AcquireTokenSilent impossible for MSA,
        /// because we would not be able to map an Msal.Account to a WAM.Account. To overcome this,
        /// we save the WAM.AccountID in MSAL's cache.
        /// </summary>
        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            if (_synchronizationContext == null)
            {
                throw new MsalClientException(
                    MsalError.WamUiThread,
                    "AcquireTokenInteractive with broker must be called from the UI thread when using WAM." +
                     ErrorMessageSuffix);
            }

            if (authenticationRequestParameters.Account != null ||
                !string.IsNullOrEmpty(authenticationRequestParameters.LoginHint))
            {
                _logger.Verbose(() => "[WamBroker] AcquireTokenIntractive - account information provided. Trying to find a Windows account that matches.");

                bool isMsaPassthrough = _wamOptions.MsaPassthrough;
                bool isMsa = await IsMsaRequestAsync(
                    authenticationRequestParameters.Authority,
                    authenticationRequestParameters?.Account?.HomeAccountId?.TenantId,
                    isMsaPassthrough).ConfigureAwait(false);

                IWamPlugin wamPlugin = isMsa ? _msaPlugin : _aadPlugin;
                WebAccountProvider provider = await GetProviderAsync(
                    authenticationRequestParameters.Authority.TenantId, isMsa)
                    .ConfigureAwait(false);

                if (PublicClientApplication.IsOperatingSystemAccount(authenticationRequestParameters.Account))
                {
                    var wamResult = await AcquireInteractiveWithWamAccountAsync(
                        authenticationRequestParameters,
                        acquireTokenInteractiveParameters.Prompt,
                        wamPlugin,
                        provider,
                        null)
                        .ConfigureAwait(false);
                    return WamAdapters.CreateMsalResponseFromWamResponse(
                        wamResult,
                        wamPlugin,
                        authenticationRequestParameters.AppConfig.ClientId,
                        _logger,
                        isInteractive: true);
                }

                var wamAccount = await FindWamAccountForMsalAccountAsync(
                    provider,
                    wamPlugin,
                    authenticationRequestParameters.Account,
                    authenticationRequestParameters.LoginHint,
                    authenticationRequestParameters.AppConfig.ClientId).ConfigureAwait(false);

                if (wamAccount != null)
                {
                    var wamResult = await AcquireInteractiveWithWamAccountAsync(
                        authenticationRequestParameters,
                        acquireTokenInteractiveParameters.Prompt,
                        wamPlugin,
                        provider,
                        wamAccount)
                        .ConfigureAwait(false);
                    return WamAdapters.CreateMsalResponseFromWamResponse(
                        wamResult,
                        wamPlugin,
                        authenticationRequestParameters.AppConfig.ClientId,
                        _logger,
                        isInteractive: true);
                }

                _logger.Verbose(() => "[WamBroker] AcquireTokenIntractive - account information provided but no matching account was found ");
            }

            // no account information available, need an account picker
            if (CanSkipAccountPicker(authenticationRequestParameters.Authority))
            {
                _logger.Verbose(() => "[WamBroker] Using AAD plugin account picker");
                return await AcquireInteractiveWithAadBrowserAsync(
                    authenticationRequestParameters,
                    acquireTokenInteractiveParameters.Prompt).ConfigureAwait(false);
            }

            _logger.Verbose(() => "[WamBroker] Using Windows account picker (AccountsSettingsPane)");
            return await AcquireInteractiveWithPickerAsync(
                authenticationRequestParameters,
                acquireTokenInteractiveParameters.Prompt)
                .ConfigureAwait(false);
        }

        // only works for AAD plugin. MSA plugin does not allow for privacy reasons
        private async Task<MsalTokenResponse> AcquireInteractiveWithAadBrowserAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            Prompt msalPrompt)
        {
            var provider = await _webAccountProviderFactory.GetAccountProviderAsync(
                            authenticationRequestParameters.Authority.TenantId).ConfigureAwait(true);

            WebTokenRequest webTokenRequest = await _aadPlugin.CreateWebTokenRequestAsync(
               provider,
               authenticationRequestParameters,
               isForceLoginPrompt: true,
               isInteractive: true,
               isAccountInWam: false)
                .ConfigureAwait(false);

            string differentAuthority = await WorkaroundOrganizationsBugAsync(authenticationRequestParameters, provider).ConfigureAwait(true);
            WamAdapters.AddMsalParamsToRequest(authenticationRequestParameters, webTokenRequest, _logger, differentAuthority);
            AddPromptToRequest(msalPrompt == Prompt.NotSpecified ? Prompt.SelectAccount : msalPrompt, true, webTokenRequest);

            var wamResult = await _wamProxy.RequestTokenForWindowAsync(
                  _parentHandle,
                  webTokenRequest).ConfigureAwait(false);

            return WamAdapters.CreateMsalResponseFromWamResponse(
                wamResult,
                _aadPlugin,
                authenticationRequestParameters.AppConfig.ClientId,
                _logger,
                isInteractive: true);
        }

        /// <summary>
        /// If the request authority is AAD (i.e. organizations or tenanted) , then skip the account picker.
        /// </summary>
        /// <param name="authority"></param>
        /// <returns></returns>
        private bool CanSkipAccountPicker(Authority authority)
        {
            // AAD plugin does not list MSA accounts for MSA-PT config
            if (_wamOptions.MsaPassthrough)
            {
                return false;
            }

            if (authority is AdfsAuthority)
            {
                return true;
            }

            if (authority is AadAuthority a && a.IsWorkAndSchoolOnly())
            {
                return true;
            }

            return false;
        }

        internal /* internal for test only */ static bool IsForceLoginPrompt(Prompt prompt)
        {
            if (prompt == Prompt.ForceLogin || prompt == Prompt.SelectAccount || prompt == Prompt.Consent)
            {
                return true;
            }

            return false;
        }

        private async Task<IWebTokenRequestResultWrapper> AcquireInteractiveWithWamAccountAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            Prompt msalPrompt,
            IWamPlugin wamPlugin,
            WebAccountProvider provider,
            WebAccount wamAccount)
        {
            WebTokenRequest webTokenRequest = await wamPlugin.CreateWebTokenRequestAsync(
                provider,
                authenticationRequestParameters,
                isForceLoginPrompt: false,
                isInteractive: true,
                isAccountInWam: true)
           .ConfigureAwait(false);

            // because of https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2476
            string differentAuthority = await WorkaroundOrganizationsBugAsync(authenticationRequestParameters, wamAccount?.WebAccountProvider).ConfigureAwait(true);
            WamAdapters.AddMsalParamsToRequest(authenticationRequestParameters, webTokenRequest, _logger, differentAuthority);

            try
            {
                IWebTokenRequestResultWrapper wamResult;
                if (wamAccount != null)
                {
                    wamResult = await _wamProxy.RequestTokenForWindowAsync(
                        _parentHandle,
                        webTokenRequest,
                        wamAccount).ConfigureAwait(false);
                }
                else
                {
                    // default user
                    wamResult = await _wamProxy.RequestTokenForWindowAsync(
                          _parentHandle,
                          webTokenRequest).ConfigureAwait(false);
                }
                return wamResult;

            }
            catch (Exception ex)
            {
                _logger.ErrorPii(ex);
                throw new MsalServiceException(
                    MsalError.WamInteractiveError,
                    "AcquireTokenInteractive without picker failed. See inner exception for details. ", ex);
            }
        }

        /// <summary>
        /// Some WAM operations fail for work and school accounts when the authority is env/organizations
        /// Changing the authority to env/common in this case works around this problem.
        /// 
        /// https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3217
        /// </summary>
        private async Task<string> WorkaroundOrganizationsBugAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            WebAccountProvider webAccountProvider)
        {
            string differentAuthority = null;
            if (string.Equals(authenticationRequestParameters.Authority.TenantId, Constants.OrganizationsTenant)) // /organizations used
            {
                if (webAccountProvider != null && _webAccountProviderFactory.IsOrganizationsProvider(webAccountProvider) ||
                    (await IsDefaultAccountAndAadAsync(authenticationRequestParameters.Account).ConfigureAwait(false)))
                {
                    differentAuthority = authenticationRequestParameters.Authority.GetTenantedAuthority("common", false);
                }
            }

            return differentAuthority;
        }

        private async Task<bool> IsDefaultAccountAndAadAsync(IAccount account)
        {
            if (account != null && PublicClientApplication.IsOperatingSystemAccount(account))
            {
                bool defaultOsAccountIsAAD = !(await _webAccountProviderFactory.IsDefaultAccountMsaAsync().ConfigureAwait(false));
                return defaultOsAccountIsAAD;
            }

            return false;
        }

        private static void AddPromptToRequest(Prompt prompt, bool isForceLoginPrompt, WebTokenRequest webTokenRequest)
        {
            if (isForceLoginPrompt &&
                prompt != Prompt.NotSpecified &&
                prompt != Prompt.NoPrompt &&
                ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 6))
            {
                // this feature works correctly since windows RS4, aka 1803 with the AAD plugin only!
                webTokenRequest.Properties["prompt"] = prompt.PromptValue;
            }
        }

        private async Task<MsalTokenResponse> AcquireInteractiveWithPickerAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            Prompt msalPrompt)
        {
            bool isMsaPassthrough = _wamOptions.MsaPassthrough;
            var accountPicker = _accountPickerFactory.Create(
                _parentHandle,
                _logger,
                _synchronizationContext,
                authenticationRequestParameters.Authority,
                isMsaPassthrough,
                _wamOptions.HeaderText);

            IWamPlugin wamPlugin;
            WebTokenRequest webTokenRequest;
            try
            {
                WebAccountProvider accountProvider = await
                    accountPicker.DetermineAccountInteractivelyAsync().ConfigureAwait(false);

                if (accountProvider == null)
                {
                    var errorMessage = "WAM Account Picker did not return an account.";

                    throw new MsalClientException(MsalError.AuthenticationCanceledError, errorMessage);
                }

                bool isConsumerTenant = _webAccountProviderFactory.IsConsumerProvider(accountProvider);
                // WAM returns the tenant here, not the full authority
                wamPlugin = (isConsumerTenant && !isMsaPassthrough) ? _msaPlugin : _aadPlugin;

                string transferToken = null;
                bool isForceLoginPrompt = false;
                if (isConsumerTenant && isMsaPassthrough)
                {
                    transferToken = await _msaPassthroughHandler.TryFetchTransferTokenInteractiveAsync(
                     authenticationRequestParameters,
                     accountProvider).ConfigureAwait(false);

                    // If a transfer token cannot be obtained, force the interactive experience again
                    isForceLoginPrompt = string.IsNullOrEmpty(transferToken);

                    // For MSA-PT, the MSA provider will issue v1 token, which cannot be used.
                    // Only the AAD provider can issue a v2 token
                    accountProvider = await _webAccountProviderFactory.GetAccountProviderAsync(
                            authenticationRequestParameters.AuthorityInfo.CanonicalAuthority.ToString())
                        .ConfigureAwait(false);
                }

                webTokenRequest = await wamPlugin.CreateWebTokenRequestAsync(
                     accountProvider,
                     authenticationRequestParameters,
                     isForceLoginPrompt: isForceLoginPrompt,
                     isInteractive: true,
                     isAccountInWam: false)
                    .ConfigureAwait(true);

                _msaPassthroughHandler.AddTransferTokenToRequest(webTokenRequest, transferToken);

                string differentAuthority = null;
                if (transferToken == null)
                {
                    differentAuthority = await WorkaroundOrganizationsBugAsync(authenticationRequestParameters, accountProvider).ConfigureAwait(true);
                }

                WamAdapters.AddMsalParamsToRequest(authenticationRequestParameters, webTokenRequest, _logger, differentAuthority);
                AddPromptToRequest(msalPrompt, isForceLoginPrompt, webTokenRequest);

            }
            catch (Exception ex) when (!(ex is MsalException))
            {
                _logger.ErrorPii(ex);
                throw new MsalServiceException(
                    MsalError.WamPickerError,
                    "Could not get the account provider - account picker. See inner exception for details", ex);
            }

            IWebTokenRequestResultWrapper wamResult;
            try
            {
                wamResult = await _wamProxy.RequestTokenForWindowAsync(_parentHandle, webTokenRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorPii(ex);
                throw new MsalServiceException(
                    MsalError.WamPickerError,
                    "Could not get the result - account picker. See inner exception for details", ex);
            }

            return WamAdapters.CreateMsalResponseFromWamResponse(
                wamResult,
                wamPlugin,
                authenticationRequestParameters.AppConfig.ClientId,
                _logger,
                isInteractive: true);
        }

        private IntPtr GetParentWindow(CoreUIParent uiParent)
        {
            // On UWP there is no need for a window handle
            return IntPtr.Zero;
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            using (_logger.LogMethodDuration())
            {
                // Important: MSAL will have already resolved the authority by now,
                // so we are not expecting "common" or "organizations" but a tenanted authority
                bool isMsa = await IsMsaRequestAsync(
                    authenticationRequestParameters.Authority,
                    null,
                    _wamOptions.MsaPassthrough)
                    .ConfigureAwait(false);

                IWamPlugin wamPlugin = isMsa ? _msaPlugin : _aadPlugin;

                WebAccountProvider provider;
                if (_wamOptions.MsaPassthrough)
                {
                    provider = await GetProviderAsync(
                        "organizations", false).ConfigureAwait(false);
                }
                else
                {
                    provider = await GetProviderAsync(
                        authenticationRequestParameters.AuthorityInfo.CanonicalAuthority.ToString(),
                        isMsa).ConfigureAwait(false);
                }

                WebAccount webAccount = await FindWamAccountForMsalAccountAsync(
                    provider,
                    wamPlugin,
                    authenticationRequestParameters.Account,
                    null, // ATS requires an account object, login_hint is not supported on its own
                    authenticationRequestParameters.AppConfig.ClientId).ConfigureAwait(false);

                if (webAccount == null && _wamOptions.MsaPassthrough)
                {
                    return await AcquireMsaTokenSilentForPassthroughAsync(
                        authenticationRequestParameters,
                        provider).ConfigureAwait(false);
                }

                if (webAccount == null)
                {
                    throw new MsalUiRequiredException(
                        MsalError.InteractionRequired,
                        "Could not find a WAM account for the silent request.");
                }

                WebTokenRequest webTokenRequest = await wamPlugin.CreateWebTokenRequestAsync(
                    provider,
                    authenticationRequestParameters,
                    isForceLoginPrompt: false,
                    isAccountInWam: true,
                    isInteractive: false)
                    .ConfigureAwait(false);

                // For MSA-PT scenario, MSAL's authority is wrong. MSAL will use Account.HomeTenantId
                // which will essentially be /consumers. This is wrong, we are not trying to obtain
                // an MSA token, we are trying to obtain an ADD *guest* token.
                string differentAuthority = null;
                if (_wamOptions.MsaPassthrough &&
                    authenticationRequestParameters.Authority is AadAuthority aadAuthority &&
                    aadAuthority.IsConsumers())
                {
                    differentAuthority = authenticationRequestParameters.Authority.GetTenantedAuthority("organizations", forceSpecifiedTenant: true);
                }

                WamAdapters.AddMsalParamsToRequest(authenticationRequestParameters, webTokenRequest, _logger, differentAuthority);

                var wamResult =
                    await _wamProxy.GetTokenSilentlyAsync(webAccount, webTokenRequest).ConfigureAwait(false);

                return WamAdapters.CreateMsalResponseFromWamResponse(
                    wamResult,
                    wamPlugin,
                    authenticationRequestParameters.AppConfig.ClientId,
                    _logger,
                    isInteractive: false);

            }
        }

        private async Task<MsalTokenResponse> AcquireMsaTokenSilentForPassthroughAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            WebAccountProvider aadAccountProvider)
        {
            // Try to find an MSA account which matches the MSAL account
            var msaProvider = await GetProviderAsync("consumers", true).ConfigureAwait(false);
            var msaWebAccount = await FindWamAccountForMsalAccountAsync(
                msaProvider,
                _msaPlugin,
                authenticationRequestParameters.Account,
                null, // ATS requires an account object, login_hint is not supported on its own
                authenticationRequestParameters.AppConfig.ClientId).ConfigureAwait(false);

            if (msaWebAccount == null)
            {
                throw new MsalUiRequiredException(
                    MsalError.InteractionRequired,
                    "Could not find a WAM MSA account for the silent request.");
            }

            // We can't use the account as is to get a token, because this account is from MSA but the provider is AAD
            // so we have to perform the transfer token flow
            string transferToken = await _msaPassthroughHandler.TryFetchTransferTokenSilentAsync(
                authenticationRequestParameters,
                msaWebAccount).ConfigureAwait(false);

            if (string.IsNullOrEmpty(transferToken))
            {
                throw new MsalUiRequiredException(
                    MsalError.InteractionRequired,
                    "Found an MSA account, but could not retrieve a transfer token for it.");
            }

            // Now make a request to AAD plugin, including the login hint and transfer token
            var webTokenRequest = await _aadPlugin.CreateWebTokenRequestAsync(
               aadAccountProvider,
               authenticationRequestParameters,
               isForceLoginPrompt: false,
               isInteractive: false,
               isAccountInWam: true)
              .ConfigureAwait(true);

            _msaPassthroughHandler.AddTransferTokenToRequest(webTokenRequest, transferToken);

            // We can't make this request on the /consumers authority, this is a known MSA-PT issue with the browser as well
            // but we can make the request over /organizations or over /MicrosoftInfrastructureTenant
            string overrideAuthority = null;
            if (authenticationRequestParameters.Authority is AadAuthority aadAuthority && aadAuthority.IsConsumers())
            {
                overrideAuthority =
                    authenticationRequestParameters.Authority.GetTenantedAuthority("organizations", true);
            }
            WamAdapters.AddMsalParamsToRequest(authenticationRequestParameters, webTokenRequest, _logger, overrideAuthority);

            var wamResult =
                await _wamProxy.RequestTokenForWindowAsync(_parentHandle, webTokenRequest).ConfigureAwait(false);

            return WamAdapters.CreateMsalResponseFromWamResponse(
                wamResult,
                _aadPlugin,
                authenticationRequestParameters.AppConfig.ClientId,
                _logger,
                isInteractive: false);
        }

        private async Task<WebAccountProvider> GetProviderAsync(
             string authority,
             bool isMsa)
        {
            WebAccountProvider provider;
            string tenantOrAuthority = isMsa ? "consumers" : authority;
            provider = await _webAccountProviderFactory.GetAccountProviderAsync(tenantOrAuthority)
                    .ConfigureAwait(false);
            return provider;
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentDefaultUserAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            using (_logger.LogMethodDuration())
            {
                var defaultAccountProvider = await _webAccountProviderFactory.GetDefaultProviderAsync().ConfigureAwait(false);
                if (defaultAccountProvider == null)
                {
                    throw new MsalUiRequiredException(
                        MsalError.InteractionRequired,
                        "A default account was not found");

                }
                // special case: passthrough + default MSA account. Need to use the transfer token protocol.
                if (_wamOptions.MsaPassthrough &&
                    _webAccountProviderFactory.IsConsumerProvider(defaultAccountProvider))
                {
                    return await AcquireTokenSilentDefaultUserPassthroughAsync(authenticationRequestParameters, defaultAccountProvider).ConfigureAwait(false);
                }

                bool isMsa = await IsMsaRequestAsync(
                    authenticationRequestParameters.Authority,
                    null,
                    _wamOptions.MsaPassthrough).ConfigureAwait(false);

                IWamPlugin wamPlugin = isMsa ? _msaPlugin : _aadPlugin;
                WebAccountProvider provider = await GetProviderAsync(
                    authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority.ToString(),
                    isMsa).ConfigureAwait(false);

                WebTokenRequest webTokenRequest = await wamPlugin.CreateWebTokenRequestAsync(
                    provider,
                    authenticationRequestParameters,
                    isForceLoginPrompt: false,
                    isAccountInWam: false,
                    isInteractive: false)
                    .ConfigureAwait(false);

                WamAdapters.AddMsalParamsToRequest(authenticationRequestParameters, webTokenRequest, _logger);

                var wamResult =
                    await _wamProxy.GetTokenSilentlyForDefaultAccountAsync(webTokenRequest).ConfigureAwait(false);

                return WamAdapters.CreateMsalResponseFromWamResponse(
                    wamResult,
                    wamPlugin,
                    authenticationRequestParameters.AppConfig.ClientId,
                    _logger,
                    isInteractive: false);
            }
        }

        private async Task<MsalTokenResponse> AcquireTokenSilentDefaultUserPassthroughAsync(AuthenticationRequestParameters authenticationRequestParameters, WebAccountProvider defaultAccountProvider)
        {
            var transferToken = await _msaPassthroughHandler.TryFetchTransferTokenSilentDefaultAccountAsync(authenticationRequestParameters, defaultAccountProvider).ConfigureAwait(false);

            if (string.IsNullOrEmpty(transferToken))
            {
                throw new MsalUiRequiredException(
                    MsalError.InteractionRequired,
                    "Cannot get a token silently (internal error: found an MSA account, but could not retrieve a transfer token for it when calling WAM)");
            }

            var aadAccountProvider = await _webAccountProviderFactory.GetAccountProviderAsync("organizations").ConfigureAwait(false);
            var webTokenRequest = await _aadPlugin.CreateWebTokenRequestAsync(
              aadAccountProvider,
              authenticationRequestParameters,
              isForceLoginPrompt: false,
              isInteractive: false,
              isAccountInWam: true)
             .ConfigureAwait(false);

            _msaPassthroughHandler.AddTransferTokenToRequest(webTokenRequest, transferToken);

            string overrideAuthority = authenticationRequestParameters.Authority.GetTenantedAuthority(InfrastructureTenant, true);

            WamAdapters.AddMsalParamsToRequest(authenticationRequestParameters, webTokenRequest, _logger, overrideAuthority);

            var wamResult =
                await _wamProxy.RequestTokenForWindowAsync(_parentHandle, webTokenRequest).ConfigureAwait(false);

            return WamAdapters.CreateMsalResponseFromWamResponse(
                wamResult,
                _aadPlugin,
                authenticationRequestParameters.AppConfig.ClientId,
                _logger,
                isInteractive: false);
        }

        private async Task<WebAccount> FindWamAccountForMsalAccountAsync(
           WebAccountProvider provider,
           IWamPlugin wamPlugin,
           IAccount msalAccount,
           string loginHint,
           string clientId)
        {
            if (msalAccount == null && string.IsNullOrEmpty(loginHint))
            {
                return null;
            }

            Account accountInternal = (msalAccount as Account);
            if (accountInternal?.WamAccountIds != null &&
                accountInternal.WamAccountIds.TryGetValue(clientId, out string wamAccountId))
            {
                _logger.Info("WAM will try to find an account based on the WAM account id from the cache");
                WebAccount result = await _wamProxy.FindAccountAsync(provider, wamAccountId).ConfigureAwait(false);
                if (result != null)
                {
                    return result;
                }

                _logger.Warning("WAM account was not found for given WAM account id.");
            }

            var wamAccounts = await _wamProxy.FindAllWebAccountsAsync(provider, clientId).ConfigureAwait(false);
            return MatchWamAccountToMsalAccount(
                wamPlugin,
                msalAccount,
                loginHint,
                wamAccounts);
        }

        private static WebAccount MatchWamAccountToMsalAccount(
            IWamPlugin wamPlugin,
            IAccount account,
            string loginHint,
            IEnumerable<WebAccount> wamAccounts)
        {
            WebAccount matchedAccountByLoginHint = null;
            foreach (var wamAccount in wamAccounts)
            {
                string homeAccountId = wamPlugin.GetHomeAccountIdOrNull(wamAccount);
                if (!string.IsNullOrEmpty(homeAccountId) &&
                    string.Equals(homeAccountId, account?.HomeAccountId?.Identifier, StringComparison.OrdinalIgnoreCase))
                {
                    return wamAccount;
                }

                if (!string.IsNullOrEmpty(loginHint) &&
                    string.Equals(loginHint, wamAccount.UserName, StringComparison.OrdinalIgnoreCase))
                {
                    matchedAccountByLoginHint = wamAccount;
                }

                if (!string.IsNullOrEmpty(account?.Username) &&
                    string.Equals(account.Username, wamAccount.UserName, StringComparison.OrdinalIgnoreCase))
                {
                    matchedAccountByLoginHint = wamAccount;
                }
            }

            return matchedAccountByLoginHint;
        }

        public async Task<IReadOnlyList<IAccount>> GetAccountsAsync(
            string clientID,
            string redirectUri,
            AuthorityInfo authorityInfo,
            ICacheSessionManager cacheSessionManager,
            IInstanceDiscoveryManager instanceDiscoveryManager)
        {
            using (_logger.LogMethodDuration())
            {
                if (!_wamOptions.ListWindowsWorkAndSchoolAccounts)
                {
                    _logger.Info("WAM::FindAllAccountsAsync returning no accounts due to configuration option");
                    return Array.Empty<IAccount>();
                }

                if (
                    !ApiInformation.IsMethodPresent(
                    "Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager",
                    "FindAllAccountsAsync"))
                {
                    _logger.Info("WAM::FindAllAccountsAsync method does not exist. Returning 0 broker accounts. ");
                    return Array.Empty<IAccount>();
                }

                var aadAccounts = await _aadPlugin.GetAccountsAsync(clientID, authorityInfo, cacheSessionManager, instanceDiscoveryManager).ConfigureAwait(false);
                var msaAccounts = await _msaPlugin.GetAccountsAsync(clientID, authorityInfo, cacheSessionManager, instanceDiscoveryManager).ConfigureAwait(false);

                return (aadAccounts.Concat(msaAccounts)).ToList();
            }
        }

        public void HandleInstallUrl(string appLink)
        {
            throw new NotImplementedException();
        }

        public bool IsBrokerInstalledAndInvokable(AuthorityType authorityType)
        {
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

        public async Task RemoveAccountAsync(ApplicationConfiguration appConfig, IAccount account)
        {
            string homeTenantId = account?.HomeAccountId?.TenantId;
            if (!string.IsNullOrEmpty(homeTenantId))
            {
                // If it's an AAD account, only the AAD plugin should remove it
                // If it's an MSA account - MSA plugin should remove it, but in MSA-PT scenarios it's still the AAD plugin
                bool isMsaRequest = await IsMsaRequestAsync(
                   appConfig.Authority,
                   homeTenantId,
                   _wamOptions.MsaPassthrough).ConfigureAwait(false);

                IWamPlugin wamPlugin = isMsaRequest ? _msaPlugin : _aadPlugin;
                WebAccountProvider provider;
                if (isMsaRequest)
                {
                    provider = await _webAccountProviderFactory.GetAccountProviderAsync("consumers").ConfigureAwait(false);
                }
                else
                {
                    provider = await _webAccountProviderFactory.GetAccountProviderAsync("organizations")
                        .ConfigureAwait(false);
                }

                var webAccount = await FindWamAccountForMsalAccountAsync(provider, wamPlugin, account, null, appConfig.ClientId)
                    .ConfigureAwait(false);
                _logger.Info(() => "Found a webAccount? " + (webAccount != null));

                if (webAccount != null)
                {
                    await webAccount.SignOutAsync();
                }
            }
        }

        private async Task<bool> IsGivenOrDefaultAccountMsaAsync(string homeTenantId)
        {
            if (!string.IsNullOrEmpty(homeTenantId))
            {
                bool result = IsConsumerTenantId(homeTenantId);
                _logger.Info(() => "[WAM Broker] Deciding plugin based on home tenant Id ... MSA? " + result);
                return result;
            }

            _logger.Warning("[WAM Broker] Cannot decide which plugin (AAD or MSA) to use. Using AAD. ");
            var isMsa = await _webAccountProviderFactory.IsDefaultAccountMsaAsync().ConfigureAwait(false);
            return isMsa;
        }

        private static bool IsConsumerTenantId(string tenantId)
        {
            return
                string.Equals(Constants.ConsumerTenant, tenantId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Constants.MsaTenantId, tenantId, StringComparison.OrdinalIgnoreCase);
        }

        internal /* for test only */ async Task<bool> IsMsaRequestAsync(
            Authority authority,
            string homeTenantId,
            bool msaPassthrough)
        {
            if (authority.AuthorityInfo.AuthorityType == AuthorityType.B2C)
            {
                throw new MsalClientException(
                    MsalError.WamNoB2C,
                    "The Windows broker (WAM) is only supported in conjunction with work and school and with Microsoft accounts.");
            }

            if (authority.AuthorityInfo.AuthorityType == AuthorityType.Adfs)
            {
                _logger.Info("[WAM Broker] ADFS authority - using only AAD plugin");
                return false;
            }

            if (msaPassthrough)
            {
                _logger.Info("[WAM Broker] MSA-PassThrough configured - using only AAD plugin");
                return false;
            }

            string authorityTenant = authority.TenantId;

            // common
            if (string.Equals(Constants.CommonTenant, authorityTenant, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info($"[WAM Broker] Tenant is common.");
                return await IsGivenOrDefaultAccountMsaAsync(homeTenantId).ConfigureAwait(false);
            }

            // org
            if (string.Equals(Constants.OrganizationsTenant, authorityTenant, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info($"[WAM Broker] Tenant is organizations, using WAM-AAD.");
                return false;
            }

            // consumers
            if (IsConsumerTenantId(authorityTenant))
            {
                _logger.Info($"[WAM Broker] Authority tenant is consumers. Using WAM-MSA ");
                return true; // for silent flow, the authority is MSA-tenant-id
            }

            _logger.Info("[WAM Broker] Tenant is not consumers and ATS will try WAM-AAD");
            return false;
        }

        public Task<MsalTokenResponse> AcquireTokenByUsernamePasswordAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenByUsernamePasswordParameters acquireTokenByUsernamePasswordParameters)
        {
            return Task.FromResult<MsalTokenResponse>(null); // nop
        }
    }
}

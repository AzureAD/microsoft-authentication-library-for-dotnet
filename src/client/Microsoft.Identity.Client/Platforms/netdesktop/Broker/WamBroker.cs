using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Platforms.net45;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    //TODO: bogavril - C++ impl catches all exceptions and emits telemetry - consider the same?
    internal class WamBroker : IBroker
    {
        private readonly IWamPlugin _aadPlugin;
        private readonly IWamPlugin _msaPlugin;


        private readonly ICoreLogger _logger;
        private readonly IntPtr _parentHandle;
        private readonly SynchronizationContext _syncronizationContext;

        // TODO: temprorary
        public const bool MSA_PASSTHROUGH = false;


        public WamBroker(CoreUIParent uiParent, ICoreLogger logger)
        {

            _logger = logger;
            _parentHandle = GetParentWindow(uiParent);
            _syncronizationContext = uiParent?.SynchronizationContext;

            _aadPlugin = new AadPlugin(_logger);
            _msaPlugin = new MsaPlugin(_logger);

        }

        /// <summary>
        /// In WAM, AcquireTokenInteractive is always associated to an account. WAM also allows for an "account picker" to be displayed, 
        /// which is similar to the EVO browser experience, allowing the user to add an account or use an existing one.
        /// 
        /// MSAL does not have a concept of account picker so MSAL.AccquireTokenInteractive will: 
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
            if (authenticationRequestParameters.Account != null ||
                !string.IsNullOrEmpty(authenticationRequestParameters.LoginHint))
            {
                bool isMsa = IsMsaRequest(
                    authenticationRequestParameters.Authority,
                    authenticationRequestParameters?.Account?.HomeAccountId?.TenantId, // TODO: we could furher optimize here by searching for an account based on UPN
                    IsMsaPassthrough(authenticationRequestParameters));

                IWamPlugin wamPlugin = isMsa ? _msaPlugin : _aadPlugin;
                WebAccountProvider provider = await GetAccountProviderAsync(authenticationRequestParameters.AuthorityInfo.CanonicalAuthority)
                    .ConfigureAwait(false);

                WebTokenRequestResult wamResult = null;

                var wamAccount = await FindWamAccountForMsalAccountAsync(
                    provider,
                    wamPlugin,
                    authenticationRequestParameters.Account,
                    authenticationRequestParameters.LoginHint,
                    authenticationRequestParameters.ClientId).ConfigureAwait(false);

                if (wamAccount != null)
                {
                    wamResult = await AcquireInteractiveWithoutPickerAsync(
                        authenticationRequestParameters,
                        wamPlugin,
                        provider,
                        wamAccount)
                        .ConfigureAwait(false);

                    return CreateMsalTokenResponse(wamResult, wamPlugin, isInteractive: true);
                }
            }

            return await AcquireInteractiveWithPickerAsync(
                authenticationRequestParameters)
                .ConfigureAwait(false);

        }

        private async Task<WebTokenRequestResult> AcquireInteractiveWithoutPickerAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            IWamPlugin wamPlugin,
            WebAccountProvider provider,
            WebAccount wamAccount)
        {
            WebTokenRequest webTokenRequest = await wamPlugin.CreateWebTokenRequestAsync(
                provider,
                isInteractive: true,
                isAccountInWam: true,
                authenticationRequestParameters)
           .ConfigureAwait(false);

            AddCommonParamsToRequest(authenticationRequestParameters, webTokenRequest);

            try
            {
                var wamResult = await WebAuthenticationCoreManagerInterop.RequestTokenWithWebAccountForWindowAsync(
                    _parentHandle, webTokenRequest, wamAccount);
                return wamResult;

            }
            catch (Exception ex)
            {
                // TODO: needs some testing to understand the kind of exceptions thrown here and how to extract more details
                _logger.ErrorPii(ex);
                throw new MsalServiceException("wam_interactive_error", "See inner exception for details", ex);
            }
        }

        private void AddCommonParamsToRequest(AuthenticationRequestParameters authenticationRequestParameters, WebTokenRequest webTokenRequest)
        {
            AddExtraParamsToRequest(webTokenRequest, authenticationRequestParameters.ExtraQueryParameters);
            AddAuthorityParamToRequest(authenticationRequestParameters, webTokenRequest);
            AddPOPParamsToRequest(webTokenRequest);
        }

        private static void AddAuthorityParamToRequest(AuthenticationRequestParameters authenticationRequestParameters, WebTokenRequest webTokenRequest)
        {
            //string aut = "https://login.windows-ppe.net/organizations";
            webTokenRequest.Properties.Add(
                            "authority",
                            authenticationRequestParameters.AuthorityInfo.CanonicalAuthority);
            webTokenRequest.Properties.Add(
                "validateAuthority",
                authenticationRequestParameters.AuthorityInfo.ValidateAuthority ? "yes" : "no");
        }

        private async Task<MsalTokenResponse> AcquireInteractiveWithPickerAsync(
            AuthenticationRequestParameters authenticationRequestParameters)
        {
            var accountPicker = new AccountPicker(
                _parentHandle,
                _logger,
                _syncronizationContext,
                authenticationRequestParameters.Authority,
                IsMsaPassthrough(authenticationRequestParameters));
            WebTokenRequest webTokenRequest = null;

            IWamPlugin wamPlugin = null;
            WebTokenRequestResult wamResult = null;
            try
            {
                var accountProvider = await accountPicker.DetermineAccountInteractivelyAsync().ConfigureAwait(false);

                if (accountProvider == null)
                {
                    throw new MsalClientException(MsalError.AuthenticationCanceledError, "WAM Account Picker did not return an account.");
                }

                wamPlugin = accountProvider.Authority == "consumers" ? _msaPlugin : _aadPlugin; //TODO: needs testing

                webTokenRequest = await wamPlugin.CreateWebTokenRequestAsync(
                     accountProvider,
                     isInteractive: true,
                     isAccountInWam: false,
                     authenticationRequestParameters).ConfigureAwait(false);

                AddCommonParamsToRequest(authenticationRequestParameters, webTokenRequest);

            }
            catch (Exception ex) when (!(ex is MsalException))
            {
                // TODO: needs some testing to understand the kind of exceptions thrown here and how to extract more details
                _logger.ErrorPii(ex);
                throw new MsalServiceException(
                    "wam_interactive_picker_error",
                    "Could not get the account provider. See inner exception for details", ex);
            }

            try
            {
                wamResult = await WebAuthenticationCoreManagerInterop.RequestTokenForWindowAsync(
                    _parentHandle, webTokenRequest);
            }
            catch (Exception ex)
            {
                _logger.ErrorPii(ex);
                throw new MsalServiceException(
                    "wam_interactive_picker_error",
                    "Could not get the result. See inner exception for details", ex);
            }

            return CreateMsalTokenResponse(wamResult, wamPlugin, isInteractive: true);
        }

        private IntPtr GetParentWindow(CoreUIParent uiParent)
        {
            if (uiParent == null || uiParent.OwnerWindow == null)
            {
                return WindowsNativeMethods.GetForegroundWindow();
            }

            if (uiParent.OwnerWindow is IntPtr ptr)
            {
                _logger.Info("Owner window specified as IntPtr.");
                return ptr;
            }

            if (uiParent.OwnerWindow is IWin32Window window) // this takes a dependency to WinForms, maybe not a good idea right now for .net core 
            {
                _logger.Info("Owner window specified as IWin32Window.");
                return window.Handle;
            }

            throw new MsalClientException(
                "wam_invalid_parent_window",
                "Invalid parent window type, expecting IntPtr but got " + uiParent.GetType());
        }

        private void AddPOPParamsToRequest(WebTokenRequest webTokenRequest)
        {
            // TODO bogavril: add POP support by adding "token_type" = "pop" and "req_cnf" = req_cnf
        }

        private bool IsMsaPassthrough(AuthenticationRequestParameters authenticationRequestParameters)
        {
            return 
                authenticationRequestParameters.ExtraQueryParameters.TryGetValue("MSAL_MSA_PT", out string val) &&
                string.Equals("1", val);
        }

        // TODO: bogavril - in C++ impl, ROPC is also included here. Will ommit for now.
        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            using (_logger.LogMethodDuration())
            {
                // Important: MSAL will have already resolved the authority by now, 
                // so we are not expecting "common" or "organizations" but a tenanted authority
                bool isMsa = IsMsaRequest(
                    authenticationRequestParameters.Authority, 
                    null, 
                    IsMsaPassthrough(authenticationRequestParameters));

                IWamPlugin wamPlugin = isMsa ? _msaPlugin : _aadPlugin;

                WebAccountProvider provider;
                if (isMsa)
                {
                    provider = await GetAccountProviderAsync("consumers").ConfigureAwait(false);
                }
                else
                {
                    provider = await GetAccountProviderAsync(authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority)
                        .ConfigureAwait(false);
                }


                // TODO: store WAM client IDs to support 3rd parties
                WebAccount webAccount = await FindWamAccountForMsalAccountAsync(
                    provider,
                    wamPlugin,
                    authenticationRequestParameters.Account,
                    authenticationRequestParameters.LoginHint,
                    authenticationRequestParameters.ClientId).ConfigureAwait(false);

                if (webAccount == null)
                {
                    return new MsalTokenResponse()
                    {
                        Error = MsalError.InteractionRequired, // this will get translated to MSALUiRequiredEx
                        ErrorDescription = "Could not find a WAM account for the silent requst"
                    };
                }

                WebTokenRequest webTokenRequest = await wamPlugin.CreateWebTokenRequestAsync(
                    provider,
                    false /* is interactive */,
                    webAccount != null, /* is account in WAM */
                    authenticationRequestParameters)
                    .ConfigureAwait(false);

                AddCommonParamsToRequest(authenticationRequestParameters, webTokenRequest);

                WebTokenRequestResult wamResult;
                using (_logger.LogBlockDuration("WAM:GetTokenSilentlyAsync:"))
                {
                    wamResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest, webAccount);

                    // TODO bogavril - WAM allows to sign in with "default" account. MSAL has no such concept.                    
                    // wamResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest);

                }

                return CreateMsalTokenResponse(wamResult, wamPlugin, isInteractive: false);
            }
        }

        private async Task<WebAccount> FindWamAccountForMsalAccountAsync(
           WebAccountProvider provider,
           IWamPlugin wamPlugin,
           IAccount account,
           string loginHint,
           string clientId)
        {
            if (account == null && string.IsNullOrEmpty(loginHint))
            {
                return null;
            }

            WamProxy wamProxy = new WamProxy(provider, _logger);

            var webAccounts = await wamProxy.FindAllWebAccountsAsync(clientId).ConfigureAwait(false);

            WebAccount matchedAccountByLoginHint = null;
            foreach (var webAccount in webAccounts)
            {
                string homeAccountId = wamPlugin.GetHomeAccountIdOrNull(webAccount);
                if (string.Equals(homeAccountId, account?.HomeAccountId?.Identifier, StringComparison.OrdinalIgnoreCase))
                {
                    return webAccount;
                }

                if (string.Equals(loginHint, webAccount.UserName, StringComparison.OrdinalIgnoreCase))
                {
                    matchedAccountByLoginHint = webAccount;
                }
            }

            return matchedAccountByLoginHint;
        }

        private const string WamErrorPrefix = "WAM Error ";

        private MsalTokenResponse CreateMsalTokenResponse(
            WebTokenRequestResult wamResponse,
            IWamPlugin wamPlugin,
            bool isInteractive)
        {
            string internalErrorCode = null;
            string errorMessage;
            string errorCode;

            switch (wamResponse.ResponseStatus)
            {
                case WebTokenRequestStatus.Success:
                // Account Switch occurs when a login hint is passed to WAM but the user chooses a different account for login.
                // MSAL treats this as a success scenario
                case WebTokenRequestStatus.AccountSwitch:
                    return wamPlugin.ParseSuccesfullWamResponse(wamResponse.ResponseData[0]);
                    
                case WebTokenRequestStatus.UserInteractionRequired:
                    errorCode =
                        wamPlugin.MapTokenRequestError(wamResponse.ResponseStatus, wamResponse.ResponseError.ErrorCode, isInteractive);
                    internalErrorCode = wamResponse.ResponseError.ErrorCode.ToString(CultureInfo.InvariantCulture);
                    errorMessage = WamErrorPrefix +
                        $"Wam plugin {wamPlugin.GetType()}" +
                        $" error code: {internalErrorCode}" +
                        $" error: " + wamResponse.ResponseError.ErrorMessage;
                    break;
                case WebTokenRequestStatus.UserCancel:
                    errorCode = MsalError.AuthenticationCanceledError;
                    errorMessage = MsalErrorMessage.AuthenticationCanceled;
                    break;
                case WebTokenRequestStatus.ProviderError:
                    errorCode =
                        wamPlugin.MapTokenRequestError(wamResponse.ResponseStatus, wamResponse.ResponseError.ErrorCode, isInteractive);
                    errorMessage = WamErrorPrefix + wamPlugin.GetType() + wamResponse.ResponseError.ErrorMessage;
                    internalErrorCode = wamResponse.ResponseError.ErrorCode.ToString(CultureInfo.InvariantCulture);
                    break;
                default:
                    errorCode = MsalError.UnknownBrokerError;
                    internalErrorCode = wamResponse.ResponseError.ErrorCode.ToString(CultureInfo.InvariantCulture);
                    errorMessage = $"Unknown WebTokenRequestStatus {wamResponse.ResponseStatus} (internal error code {internalErrorCode})";
                    break;
            }

            return new MsalTokenResponse()
            {
                Error = errorCode,
                ErrorCodes = internalErrorCode != null ? new[] { internalErrorCode } : null,
                ErrorDescription = errorMessage
            };
        }



        private void AddExtraParamsToRequest(WebTokenRequest webTokenRequest, IDictionary<string, string> extraQueryParameters)
        {
            if (extraQueryParameters != null)
            {
                // MSAL uses instance_aware=true, but WAM calls it discover=home, so we rename the parameter before passing
                // it to WAM.
                foreach (var kvp in extraQueryParameters)
                {
                    string key = kvp.Key;
                    string value = kvp.Value;

                    if (string.Equals("instance_aware", key) && string.Equals("true", value))
                    {
                        key = "discover";
                        value = "home";
                    }

                    webTokenRequest.AppProperties.Add(key, value);
                }
            }
        }

        private bool IsMsaRequest(Authority authority, string homeTenantId, bool msaPassthrough)
        {
            if (authority.AuthorityInfo.AuthorityType == AuthorityType.B2C)
            {
                throw new MsalClientException(
                    "wam_no_b2c",
                    "WAM broker is not supported in conjuction with a B2C authority");
            }

            if (authority.AuthorityInfo.AuthorityType == AuthorityType.Adfs)
            {
                _logger.Info("[WAM Broker] ADFS authority - using only AAD plugin ");
                return false;
            }

            string authorityTenant = authority.TenantId;

            // common 
            if (string.Equals("common", authorityTenant, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info($"[WAM Broker] Tenant is common.");
                return IsHomeTidMSA(homeTenantId);
            }

            // org
            if (string.Equals("organizations", authorityTenant, StringComparison.OrdinalIgnoreCase))
            {
                if (msaPassthrough)
                {
                    _logger.Info($"[WAM Broker] Tenant is organizations, but with MSA-PT (similar to common).");
                    return IsHomeTidMSA(homeTenantId);
                }

                _logger.Info($"[WAM Broker] Tenant is organizations, using WAM-AAD.");
                return false;
            }

            // consumers
            if (IsConsumerTenantId(authorityTenant))
            {
                _logger.Info("[WAM Broker] Authority tenant is consumers. ATS will try WAM-MSA ");
                return true;
            }

            _logger.Info("[WAM Broker] Tenant is not consumers and ATS will try WAM-AAD");
            return false;
        }

        private bool IsHomeTidMSA(string homeTenantId)
        {
            if (!string.IsNullOrEmpty(homeTenantId))
            {
                bool result = IsConsumerTenantId(homeTenantId);
                _logger.Info("[WAM Broker] Deciding plugin based on home tenant Id ... Msa? " + result);
                return result;
            }

            _logger.Warning("[WAM Broker] Cannot decide which plugin (AAD or MSA) to use. Using AAD. ");
            return false;
        }

        private static bool IsConsumerTenantId(string tenantId)
        {
            return
                string.Equals("consumenrs", tenantId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("9188040d-6c67-4c5b-b112-36a304b66dad", tenantId, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID, string redirectUri)
        {
            using (_logger.LogMethodDuration())
            {
                if (!ApiInformation.IsMethodPresent(
                    "Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager",
                    "FindAllAccountsAsync"))
                {
                    _logger.Info("WAM::FindAllAccountsAsync method does not exist. Returning 0 broker accounts. ");
                    return Enumerable.Empty<IAccount>();
                }

                var aadAccounts = await _aadPlugin.GetAccountsAsync(clientID).ConfigureAwait(false);
                var msaAccounts = await _msaPlugin.GetAccountsAsync(clientID).ConfigureAwait(false);

                return aadAccounts.Concat(msaAccounts);
            }
        }

        public void HandleInstallUrl(string appLink)
        {
            throw new NotImplementedException();
        }

        public bool IsBrokerInstalledAndInvokable()
        {
            return true;
        }

        public Task RemoveAccountAsync(string clientID, IAccount account)
        {
            _logger.Verbose("WAM accounts are not removable.");
            return Task.CompletedTask;
        }

        #region Helpers
        private static async Task<WebAccountProvider> GetDefaultAccountProviderAsync()
        {
            return await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.windows.local");
        }

        public static async Task<bool> IsDefaultAccountMsaAsync()
        {
            var provider = await GetDefaultAccountProviderAsync().ConfigureAwait(false);
            return provider != null && string.Equals("consumers", provider.Authority);
        }

        public static string GetEffectiveScopes(ISet<string> scopes) // TODO: consolidate with MSAL logic
        {
            var effectiveScopeSet = scopes.Union(OAuth2Value.ReservedScopes);
            return effectiveScopeSet.AsSingleString();
        }

        public static async Task<WebAccountProvider> GetAccountProviderAsync(string authorityOrTenant)
        {
            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                "https://login.microsoft.com", // TODO bogavril: what about other clouds?
               authorityOrTenant);

            return provider;
        }

        #endregion
    }
}

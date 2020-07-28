using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Windows.ApplicationModel.Chat;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.Web;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    //TODO: bogavril - C++ impl catches all exceptions and emits telemetry - consider the same?
    internal class WamBroker : IBroker
    {
        private readonly IWamPlugin _aadPlugin;
        private readonly IWamPlugin _msaPlugin;


        private readonly CoreUIParent _uiParent;
        private readonly ICoreLogger _logger;


        public WamBroker(CoreUIParent uiParent, ICoreLogger logger)
        {

            _uiParent = uiParent;
            _logger = logger;

            _aadPlugin = new AadPlugin(_logger, _uiParent);
            _msaPlugin = new MsaPlugin();

        }

        public Task<MsalTokenResponse> AcquireTokenInteractiveAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            throw new NotImplementedException();
        }

        // TODO: bogavril - in C++ impl, ROPC is also included here. Will ommit for now.
        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            using (_logger.LogMethodDuration())
            {
                // TODO: bogavril - too many authority objects...
                string tenantId = authenticationRequestParameters.OriginalAuthority.TenantId;
                bool isMsa = await IsMsaSilentRequestAsync(tenantId).ConfigureAwait(false);
                IWamPlugin wamPlugin = isMsa ? _msaPlugin : _aadPlugin;
                WebAccountProvider provider = await wamPlugin.
                    GetAccountProviderAsync(authenticationRequestParameters.AuthorityInfo.CanonicalAuthority).ConfigureAwait(false);

                // In C++ impl WAM Account ID is stored in the cache and GetAccounts write the WAM derived accounts to the cache, 
                // which is a perf optimization. Should work fine with reading accounts on the fly each time.
                WebAccount webAccount = await wamPlugin.FindWamAccountForMsalAccountAsync(
                    provider,
                    authenticationRequestParameters.Account,
                    authenticationRequestParameters.LoginHint,
                    authenticationRequestParameters.ClientId).ConfigureAwait(false);

                WebTokenRequest webTokenRequest = wamPlugin.CreateWebTokenRequest(
                    provider,
                    false /* is interactive */,
                    webAccount != null, /* is account in WAM */
                    authenticationRequestParameters);

                AddExtraParamsToRequest(webTokenRequest, authenticationRequestParameters.ExtraQueryParameters);
                // TODO bogavril: add POP support by adding "token_type" = "pop" and "req_cnf" = req_cnf

                WebTokenRequestResult wamResult;
                using (_logger.LogBlockDuration("WAM:GetTokenSilentlyAsync:"))
                {
                    if (webAccount != null)
                    {
                        wamResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest, webAccount);
                    }
                    else
                    {
                        // TODO bogavril - question - what does this do ?
                        wamResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest);
                    }
                }

                return CreateMsalTokenResponse(wamResult, wamPlugin, isInteractive: false);
            }
        }

        private const string WamErrorPrefix = "The Windows Broker (WAM) encountered an error: ";

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
                    return wamPlugin.ParseSuccesfullWamResponse(wamResponse.ResponseData[0]);
                case WebTokenRequestStatus.UserInteractionRequired:
                    errorCode =
                        wamPlugin.MapTokenRequestError(wamResponse.ResponseStatus, wamResponse.ResponseError.ErrorCode, isInteractive);
                    internalErrorCode = wamResponse.ResponseError.ErrorCode.ToString(CultureInfo.InvariantCulture);
                    errorMessage = WamErrorPrefix + wamResponse.ResponseError.ErrorMessage;
                    break;
                case WebTokenRequestStatus.UserCancel:
                    errorCode = MsalError.AuthenticationCanceledError;
                    errorMessage = MsalErrorMessage.AuthenticationCanceled;
                    break;
                case WebTokenRequestStatus.ProviderError:
                    errorCode =
                        wamPlugin.MapTokenRequestError(wamResponse.ResponseStatus, wamResponse.ResponseError.ErrorCode, isInteractive);
                    errorMessage = WamErrorPrefix + wamResponse.ResponseError.ErrorMessage;
                    internalErrorCode = wamResponse.ResponseError.ErrorCode.ToString(CultureInfo.InvariantCulture);
                    break;
                case WebTokenRequestStatus.AccountSwitch: // TODO: bogavril - what does this mean?
                    errorCode = "account_switch";
                    errorMessage = "WAM returned AccountSwitch";
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

        /// <summary>
        /// 
        /// MSA request if: 
        ///  - tenant is "common" AND default WAM account in MSA
        ///  - tenant is "consumers"
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<bool> IsMsaSilentRequestAsync(string tenantId)
        {
            if (string.Equals("common", tenantId, StringComparison.OrdinalIgnoreCase))
            {
                bool isMsa = await IsDefaultAccountMsaAsync().ConfigureAwait(false);
                _logger.Verbose("[WAM Broker] Tenant: common. Default WAM account is MSA? " + isMsa);
                return isMsa;
            }

            if (string.Equals("consumers", tenantId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Verbose("[WAM Broker] Tenant is consumers. ATS will try WAM-MSA ");
                return true;
            }

            _logger.Verbose("[WAM Broker] ATS will try WAM-AAD");
            return false;
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
            throw new NotImplementedException();
        }

        private async Task<WebAccountProvider> GetDefaultAccountProviderAsync()
        {
            return await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.windows.local");
        }

        private async Task<bool> IsDefaultAccountMsaAsync()
        {
            var provider = await GetDefaultAccountProviderAsync().ConfigureAwait(false);
            return provider != null && string.Equals("consumers", provider.Authority);
        }
    }


}

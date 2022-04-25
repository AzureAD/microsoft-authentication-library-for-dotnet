// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
#if NET5_WIN
    [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
#endif
    internal class AadPlugin : IWamPlugin
    {
        private readonly IWamProxy _wamProxy;
        private readonly IWebAccountProviderFactory _webAccountProviderFactory;
        private readonly ICoreLogger _logger;
        private const int FACILITY_ADAL_HTTP = 0xAA3;
        private const int FACILITY_ADAL_URLMON = 0xAA7;
        private const int FACILITY_ADAL_INTERNET = 0xAA8;
        private const int FACILITY_ADAL_BACKGROUND_INFRASTRUCTURE = 0xAAD;
        private const int FACILITY_ADAL_DEVELOPER = 0xAA1;
        private const uint BT_E_SPURIOUS_ACTIVATION = 0x80080300;
        private const uint ERROR_ADAL_SERVER_ERROR_TEMPORARILY_UNAVAILABLE = 0xcaa20005;
        private const uint ERROR_ADAL_SERVER_ERROR_RECEIVED = 0xcaa20008;

        public AadPlugin(IWamProxy wamProxy, IWebAccountProviderFactory webAccountProviderFactory, ICoreLogger logger)
        {
            _wamProxy = wamProxy;
            _webAccountProviderFactory = webAccountProviderFactory;
            _logger = logger;
        }

        /// <summary>
        /// The algorithm here is much more complex in order to workaround a limitation in the AAD plugin's 
        /// handling of guest accounts: 
        /// 
        /// 1. Read the accounts from WAM.AADPlugin
        /// 2. For each account, we need to find its home_account_id as the one from WAM may not be correct
        /// 3. If we can find a cached account with the same LocalAccountId or UPN, use it
        /// 4. If not, make a simple silent token request and use the client info provided
        /// </summary>
        public async Task<IReadOnlyList<IAccount>> GetAccountsAsync(
            string clientId,
            AuthorityInfo authorityInfo,
            Cache.ICacheSessionManager cacheSessionManager,
            Instance.Discovery.IInstanceDiscoveryManager instanceDiscoveryManager)
        {
            var webAccountProvider = await _webAccountProviderFactory.GetAccountProviderAsync("organizations").ConfigureAwait(false);
            var wamAccounts = await _wamProxy.FindAllWebAccountsAsync(webAccountProvider, clientId).ConfigureAwait(false);

            if (wamAccounts.Count > 0)
            {
                var webAccountEnvs = wamAccounts
                    .Select(w =>
                    {
                        _wamProxy.TryGetAccountProperty(w, "Authority", out string accountAuthority);
                        if (accountAuthority != null)
                        {
                            return (new Uri(accountAuthority)).Host;
                        }
                        else
                        {
                            _logger.WarningPii(
                                $"[WAM AAD Provider] Could not convert the WAM account {w.UserName} (id: {w.Id}) to an MSAL account because the Authority could not be found",
                                $"[WAM AAD Provider] Could not convert the WAM account {w.Id} to an MSAL account because the Authority could not be found");

                            return null;
                        }
                    })
                    .Where(a => a != null);

                var instanceMetadata = await instanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(authorityInfo, webAccountEnvs, cacheSessionManager.RequestContext)
                    .ConfigureAwait(false);
                var accountsFromCache = await cacheSessionManager.GetAccountsAsync().ConfigureAwait(false);

                var msalAccountTasks = wamAccounts
                    .Select(
                        async webAcc =>
                            await ConvertToMsalAccountOrNullAsync(
                                clientId,
                                webAcc,
                                instanceMetadata,
                                cacheSessionManager,
                                accountsFromCache).ConfigureAwait(false));

                var msalAccounts = (await Task.WhenAll(msalAccountTasks).ConfigureAwait(false)).Where(a => a != null).ToList();

                _logger.Info($"[WAM AAD Provider] GetAccountsAsync converted {msalAccounts.Count} accounts from {wamAccounts.Count} WAM accounts");
                return msalAccounts;
            }

            _logger.Info("[WAM AAD provider] No accounts found.");
            return Array.Empty<IAccount>();
        }

        private async Task<Account> ConvertToMsalAccountOrNullAsync(
            string clientId,
            WebAccount webAccount,
            InstanceDiscoveryMetadataEntry envMetadata,
            ICacheSessionManager cacheManager,
            IEnumerable<IAccount> accountsFromCache)
        {

            webAccount.Properties.TryGetValue("TenantId", out string realm);

            if (!_wamProxy.TryGetAccountProperty(webAccount, "Authority", out string accountAuthority))
            {
                _logger.WarningPii(
                    $"[WAM AAD Provider] Could not convert the WAM account {webAccount.UserName} (id: {webAccount.Id}) to an MSAL account because the Authority could not be found",
                    $"[WAM AAD Provider] Could not convert the WAM account {webAccount.Id} to an MSAL account because the Authority could not be found");

                return null;
            }

            string accountEnv = (new Uri(accountAuthority)).Host;
            if (!envMetadata.Aliases.ContainsOrdinalIgnoreCase(accountEnv))
            {
                _logger.InfoPii(
                $"[WAM AAD Provider] Account {webAccount.UserName} environment {accountEnv} does not match input authority environment {envMetadata.PreferredNetwork} or an alias",
                $"[WAM AAD Provider] Account environment {accountEnv} does not match input authority environment {envMetadata.PreferredNetwork}");

                return null;
            }

            if (MatchCacheAccount(webAccount, accountsFromCache, out AccountId homeAccountId))
            {
                _logger.VerbosePii(
                    $"[WAM AAD Provider] ConvertToMsalAccountOrNullAsync account {webAccount.UserName} matched a cached account",
                    $"[WAM AAD Provider] Account matched a cache account");

                return new Account(
                    homeAccountId.Identifier,
                    webAccount.UserName,
                    envMetadata.PreferredNetwork,
                    new Dictionary<string, string>() { { clientId, webAccount.Id } });
            }

            return await GetIdFromWebResponseAsync(clientId, webAccount, envMetadata, cacheManager).ConfigureAwait(false);
        }

        private async Task<Account> GetIdFromWebResponseAsync(string clientId, WebAccount webAccount, InstanceDiscoveryMetadataEntry envMetadata, ICacheSessionManager cacheManager)
        {
            MsalTokenResponse response = await AcquireBasicTokenSilentAsync(
                webAccount,
                clientId).ConfigureAwait(false);

            if (response != null)
            {
                var tuple = await cacheManager.SaveTokenResponseAsync(response).ConfigureAwait(false);

                _logger.InfoPii(
                      $"[WAM AAD Provider] ConvertToMsalAccountOrNullAsync resolved account {webAccount.UserName} via web call? {tuple?.Item3 != null}",
                      $"[WAM AAD Provider] ConvertToMsalAccountOrNullAsync resolved account via web call? {tuple?.Item3 != null}");

                return tuple.Item3; // Account
            }

            return null;
        }

        private async Task<MsalTokenResponse> AcquireBasicTokenSilentAsync(
            WebAccount webAccount,
            string clientId)
        {
            // we checked that it exists previously
            _wamProxy.TryGetAccountProperty(webAccount, "Authority", out string accountAuthority);
            var provider = await _webAccountProviderFactory.GetAccountProviderAsync(accountAuthority).ConfigureAwait(false);

            // We are not requesting any additional scopes beyond "profile" and "openid" (which are added by default)
            // since we do not want to require any additional claims (and thus be unable to renew the refresh token).
            var request = await CreateWebTokenRequestAsync(provider, clientId, "profile openid").ConfigureAwait(false);

            // Note that this is never a guest flow, we are always acquiring a token for the home realm,
            // since we only need the client info.

            var wamResult = await _wamProxy.GetTokenSilentlyAsync(webAccount, request).ConfigureAwait(false);

            if (!wamResult.ResponseStatus.IsSuccessStatus())
            {
                _logger.Warning($"[WAM AAD Provider] GetIdFromWebResponseAsync failed {wamResult.ResponseStatus} - {wamResult.ResponseError}");
                return null;
            }

            return ParseSuccessfullWamResponse(wamResult.ResponseData[0], out _);
        }

        private bool MatchCacheAccount(
            WebAccount webAccount,
            IEnumerable<IAccount> accountsFromCache,
            out AccountId homeAccountId)
        {

            // TODO: a match can also be done on local account id
            // however this would require loading all IdTokens associated with the account
            // and parsing them

            var match = accountsFromCache.FirstOrDefault(acc =>
                string.Equals(acc.Username, webAccount.UserName));

            if (match != null)
            {
                homeAccountId = match.HomeAccountId;
                return true;
            }

            homeAccountId = null;
            return false;
        }

        public string GetHomeAccountIdOrNull(WebAccount webAccount)
        {
            if (!webAccount.Properties.TryGetValue("TenantId", out string tenantId))
            {
                _logger.WarningPii(
                    $"[WAM AAD Provider] Could not convert the WAM account {webAccount.UserName} (id: {webAccount.Id}) to an MSAL account because the tenant ID could not be found",
                    $"[WAM AAD Provider] Could not convert the WAM account id: {webAccount.Id} to an MSAL account because the tenant ID could not be found");
                return null;
            }

            if (!webAccount.Properties.TryGetValue("OID", out string oid))
            {
                _logger.WarningPii(
                    $"[WAM AAD Provider] Could not convert the WAM account {webAccount.UserName} (id: {webAccount.Id}) to an MSAL account because the OID could not be found",
                    $"[WAM AAD Provider] Could not convert the WAM account {webAccount.Id} to an MSAL account because the OID could not be found");

                return null;
            }

            return oid + "." + tenantId;
        }

        public Task<WebTokenRequest> CreateWebTokenRequestAsync(
            WebAccountProvider provider,
            AuthenticationRequestParameters authenticationRequestParameters,
            bool isForceLoginPrompt,
            bool isInteractive,
            bool isAccountInWam, 
            string scopeOverride = null)
        {
            string loginHint = !string.IsNullOrEmpty(authenticationRequestParameters.LoginHint) ?
                authenticationRequestParameters.LoginHint :
                authenticationRequestParameters.Account?.Username;

            bool setLoginHint = 
                isInteractive && 
                !isAccountInWam && 
                !string.IsNullOrEmpty(loginHint);

            var wamPrompt = setLoginHint || (isInteractive && isForceLoginPrompt) ?
                WebTokenRequestPromptType.ForceAuthentication :
                WebTokenRequestPromptType.Default;

            WebTokenRequest request = new WebTokenRequest(
                provider,
                scopeOverride ?? ScopeHelper.GetMsalScopes(authenticationRequestParameters.Scope).AsSingleString(),
                authenticationRequestParameters.AppConfig.ClientId,
                wamPrompt);

            if (setLoginHint)
            {
                request.Properties.Add("LoginHint", authenticationRequestParameters.LoginHint);
            }

            request.Properties.Add("wam_compat", "2.0");
            if (ApiInformation.IsPropertyPresent("Windows.Security.Authentication.Web.Core.WebTokenRequest", "CorrelationId"))
            {
                LegacyOsWamProxy.SetCorrelationId(request, authenticationRequestParameters.CorrelationId.ToString());
            }
            else
            {
                request.Properties.Add("correlationId", authenticationRequestParameters.CorrelationId.ToString());
            }

            if (!string.IsNullOrEmpty(authenticationRequestParameters.ClaimsAndClientCapabilities))
            {
                request.Properties.Add("claims", authenticationRequestParameters.ClaimsAndClientCapabilities);
            }

            return Task.FromResult(request);
        }

        public Task<WebTokenRequest> CreateWebTokenRequestAsync(WebAccountProvider provider, string clientId, string scopes)
        {
            WebTokenRequest request = new WebTokenRequest(
              provider,
              scopes,
              clientId);

            request.Properties.Add("wam_compat", "2.0");

            return Task.FromResult(request);
        }

        public MsalTokenResponse ParseSuccessfullWamResponse(
                WebTokenResponse webTokenResponse,
                out Dictionary<string, string> allProperties)
        {
            allProperties = new Dictionary<string, string>(8, StringComparer.OrdinalIgnoreCase);
            if (!webTokenResponse.Properties.TryGetValue("TokenExpiresOn", out string expiresOn))
            {
                _logger.Warning("Result from WAM does not have expiration. Marking access token as expired.");
                expiresOn = null;
            }

            if (!webTokenResponse.Properties.TryGetValue("ExtendedLifetimeToken", out string extendedExpiresOn))
            {
                extendedExpiresOn = null;
            }

            if (!webTokenResponse.Properties.TryGetValue("Authority", out string authority))
            {
                _logger.Error("Result from WAM does not have authority.");
                return new MsalTokenResponse()
                {
                    Error = "no_authority_in_wam_response",
                    ErrorDescription = "No authority in WAM response"
                };
            }

            if (!webTokenResponse.Properties.TryGetValue("correlationId", out string correlationId))
            {
                _logger.Warning("No correlation ID in response");
                correlationId = null;
            }

            bool hasIdToken = webTokenResponse.Properties.TryGetValue("wamcompat_id_token", out string idToken);
            _logger.Info("Result from WAM has id token? " + hasIdToken);

            bool hasClientInfo = webTokenResponse.Properties.TryGetValue("wamcompat_client_info", out string clientInfo);
            _logger.Info("Result from WAM has client info? " + hasClientInfo);

            bool hasScopes = webTokenResponse.Properties.TryGetValue("wamcompat_scopes", out string scopes);
            _logger.InfoPii("Result from WAM scopes: " + scopes,
                "Result from WAM has scopes? " + hasScopes);

            foreach (var kvp in webTokenResponse.Properties)
            {
                allProperties[kvp.Key] = kvp.Value;
            }

            MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
            {
                AccessToken = webTokenResponse.Token,
                IdToken = idToken,
                CorrelationId = correlationId,
                Scope = scopes,
                ExpiresIn = DateTimeHelpers.GetDurationFromWindowsTimestamp(expiresOn, _logger),
                ExtendedExpiresIn = DateTimeHelpers.GetDurationFromWindowsTimestamp(extendedExpiresOn, _logger),
                ClientInfo = clientInfo,
                TokenType = "Bearer",
                WamAccountId = webTokenResponse?.WebAccount?.Id,
                TokenSource = TokenSource.Broker
            };

            return msalTokenResponse;
        }

        public Tuple<string, string, bool> MapTokenRequestError(WebTokenRequestStatus status, uint errorCode, bool isInteractive)
        {
            if (status == WebTokenRequestStatus.UserInteractionRequired)
            {
                return Tuple.Create(MsalError.InteractionRequired, "", false);
            }

            if (status == WebTokenRequestStatus.ProviderError)
            {
                switch (errorCode)
                {
                    case ERROR_ADAL_SERVER_ERROR_TEMPORARILY_UNAVAILABLE:
                    case ERROR_ADAL_SERVER_ERROR_RECEIVED: // ERROR_ADAL_SERVER_ERROR_RECEIVED in AAD WAM plugin
                        return Tuple.Create("WAM_server_temporarily_unavailable", $"Windows broker server unavailable. Error: {errorCode}", true);

                    case BT_E_SPURIOUS_ACTIVATION: // BT_E_SPURIOUS_ACTIVATION in AAD WAM plugin
                        return Tuple.Create("WAM_plugin_process_interrupted", "Either WAM plugin process didn’t start, or WAM plugin process didn’t finish in expected protocol.", true);
                }

                unchecked // as per https://stackoverflow.com/questions/34198173/conversion-of-hresult-between-c-and-c-sharp
                {
                    var hresultFacility = (((errorCode) >> 16) & 0x1fff);
                    switch (hresultFacility)
                    {
                        case FACILITY_ADAL_HTTP: // FACILITY_ADAL_HTTP in AAD WAM plugin
                        case FACILITY_ADAL_URLMON: // FACILITY_ADAL_URLMON in AAD WAM plugin
                        case FACILITY_ADAL_INTERNET: // FACILITY_ADAL_INTERNET in AAD WAM plugin
                            return Tuple.Create("WAM_no_network", $"Windows broker network issue. HR result facility: {hresultFacility}", true);

                        case FACILITY_ADAL_BACKGROUND_INFRASTRUCTURE: // FACILITY_ADAL_BACKGROUND_INFRASTRUCTURE in AAD WAM plugin
                            return Tuple.Create($"WAM_background_infrastructure_cancelled", "Background infrastructure cancelled due to not enough resources at the moment.", true);

                        case FACILITY_ADAL_DEVELOPER: // FACILITY_ADAL_DEVELOPER in AAD WAM plugin
                            return Tuple.Create("WAM_internal_error_ApiContractViolation", "", false);

                        default:
                            return Tuple.Create($"WAM_aad_provider_error_{errorCode}", "", false);
                    }
                }
            }

            return Tuple.Create("WAM_unexpected_aad_error", "", false);
        }
    }
}

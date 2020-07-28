using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    internal class AadPlugin : IWamPlugin
    {
        private readonly ICoreLogger _logger;
        private readonly CoreUIParent _uiParent;

        public AadPlugin(ICoreLogger logger, CoreUIParent uiParent)
        {
            _logger = logger;
            _uiParent = uiParent;
        }

        public Task<MsalTokenResponse> AcquireTokenInteractiveAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            throw new NotImplementedException();
        }


        public async Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID)
        {
            var webAccounProvider = await GetAccountProviderAsync().ConfigureAwait(false);
            WamProxy wamProxy = new WamProxy(webAccounProvider, _logger); //TODO: not suitable for unit testing

            var webAccounts = await wamProxy.FindAllWebAccountsAsync(clientID).ConfigureAwait(false);

            var msalAccounts = webAccounts
                .Select(webAcc => ConvertToMsalAccountOrNull(webAcc))
                .Where(a => a != null)
                .ToList();

            _logger.Info($"[WAM AAD Provider] GetAccountsAsync converted {webAccounts.Count()} MSAL accounts");
            return msalAccounts;
        }


        public async Task<WebAccountProvider> GetAccountProviderAsync(string tenant = "organizations")
        {
            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                "https://login.microsoft.com", // TODO bogavril: what about other clouds?
               tenant);

            return provider;
        }

        private Account ConvertToMsalAccountOrNull(WebAccount webAccount)
        {
            string username = webAccount.UserName;

            if (!webAccount.Properties.TryGetValue("Authority", out string authority))
            {
                _logger.WarningPii(
                    $"[WAM AAD Provider] Could not convert the WAM account {webAccount.UserName} (id: {webAccount.Id}) to an MSAL account because the Authority could not be found",
                    $"[WAM AAD Provider] Could not convert the WAM account {webAccount.Id} to an MSAL account because the Authority could not be found");

                return null;
            }

            string environment = (new Uri(authority)).Host;


            // TODO bogavril - this TODO was copied from C++ implementation and may not be relevant for MSAL .net
            // AAD WAM plugin returns both guest and home accounts as part of FindAllAccountAsync call.
            // We will need to de-dupe WAM accounts before writing them to MSAL cache.
            string homeAccountId = GetHomeAccountIdOrNull(webAccount);

            if (homeAccountId != null)
            {
                var msalAccount = new Account(homeAccountId, username, environment);
                return msalAccount;
            }

            return null;
        }

        //TODO: bogavril - private?
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

        public async Task<WebAccount> FindWamAccountForMsalAccountAsync(
            WebAccountProvider provider,
            IAccount account,
            string loginHint,
            string clientId)
        {
            WamProxy wamProxy = new WamProxy(provider, _logger);

            var webAccounts = await wamProxy.FindAllWebAccountsAsync(clientId).ConfigureAwait(false);

            WebAccount matchedAccountByLoginHint = null;
            foreach (var webAccount in webAccounts)
            {
                string homeAccountId = GetHomeAccountIdOrNull(webAccount);
                if (string.Equals(homeAccountId, account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                {
                    return webAccount;
                }

                if (string.Equals(loginHint, account.Username, StringComparison.OrdinalIgnoreCase))
                {
                    matchedAccountByLoginHint = webAccount;
                }
            }

            return matchedAccountByLoginHint;
        }

        public WebTokenRequest CreateWebTokenRequest(
            WebAccountProvider provider,
            bool isInteractive,
            bool isAccountInWam,
            AuthenticationRequestParameters authenticationRequestParameters)
        {

            bool setLoginHint = isInteractive && !isAccountInWam && !string.IsNullOrEmpty(authenticationRequestParameters.LoginHint);
            var wamPrompt = setLoginHint ?
                WebTokenRequestPromptType.ForceAuthentication :
                WebTokenRequestPromptType.Default;

            WebTokenRequest request = new WebTokenRequest(
                provider,
                GetEffectiveScopes(authenticationRequestParameters.Scope),
                authenticationRequestParameters.ClientId,
                wamPrompt);

            if (setLoginHint)
            {
                request.AppProperties.Add("LoginHint", authenticationRequestParameters.LoginHint);
            }

            // TODO: bogavril - add support for ROPC ?

            request.AppProperties.Add("wam_compat", "2.0");
            if (ApiInformation.IsPropertyPresent("Windows.Security.Authentication.Web.Core.WebTokenRequest", "CorrelationId"))
            {
                request.CorrelationId = authenticationRequestParameters.CorrelationId.ToString();
            }
            else
            {
                request.AppProperties.Add("correlationId", authenticationRequestParameters.CorrelationId.ToString());
            }

            if (!string.IsNullOrEmpty(authenticationRequestParameters.ClaimsAndClientCapabilities))
            {
                request.AppProperties.Add("claims", authenticationRequestParameters.ClaimsAndClientCapabilities);
            }

            return request;
        }

        private string GetEffectiveScopes(SortedSet<string> scopes)
        {
            var effectiveScopeSet = scopes.Union(OAuth2Value.ReservedScopes);
            return effectiveScopeSet.AsSingleString();
        }

        public MsalTokenResponse ParseSuccesfullWamResponse(WebTokenResponse wamResponse)
        {
            if (!wamResponse.Properties.TryGetValue("TokenExpiresOn", out string expiresOn))
            {
                _logger.Warning("Result from WAM does not have expiration. Marking access token as expired.");
                expiresOn = null;
            }

            if (!wamResponse.Properties.TryGetValue("ExtendedLifetimeToken", out string extendedExpiresOn))
            {
                extendedExpiresOn = null;
            }

            if (!wamResponse.Properties.TryGetValue("Authority", out string authority))
            {
                _logger.Error("Result from WAM does not have authority.");
                return new MsalTokenResponse()
                {
                    Error = "no_authority_in_wam_response",
                    ErrorDescription = "No authority in WAM response"
                };
            }

            if (!wamResponse.Properties.TryGetValue("correlationId", out string correlationId))
            {
                _logger.Warning("No correlation ID in response");
                correlationId = null;
            }

            bool hasIdToken = wamResponse.Properties.TryGetValue("wamcompat_id_token", out string idToken);
            _logger.Info("Result from WAM has id token? " + hasIdToken);

            bool hasClientInfo = wamResponse.Properties.TryGetValue("wamcompat_client_info", out string clientInfo);
            _logger.Info("Result from WAM has client info? " + hasClientInfo);

            bool hasScopes = wamResponse.Properties.TryGetValue("wamcompat_scopes", out string scopes);
            _logger.InfoPii("Result from WAM scopes: " + scopes,
                "Result from WAM has scopes? " + hasScopes);

            MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
            {
                Authority = authority,
                AccessToken = wamResponse.Token,
                IdToken = idToken,
                CorrelationId = correlationId,
                Scope = scopes,
                ExpiresIn = CoreHelpers.GetDurationFromWindowsTimestamp(expiresOn, _logger),
                ExtendedExpiresIn = CoreHelpers.GetDurationFromWindowsTimestamp(extendedExpiresOn, _logger),
                ClientInfo = clientInfo,
                TokenType = "Bearer", // TODO: bogavril - token type?
                TokenSource = TokenSource.Broker
            };

            return msalTokenResponse;
        }

        public string MapTokenRequestError(WebTokenRequestStatus status, uint errorCode, bool isInteractive)
        {
            if (status == WebTokenRequestStatus.UserInteractionRequired)
            {
                return MsalError.InteractionRequired;
            }

            if (status == WebTokenRequestStatus.ProviderError)
            {
                if (errorCode == 0xcaa20005)
                    return "ServerTemporarilyUnavailable"; //TODO bogavril: find existing error codes for these

                var hresultFacility = (((errorCode) >> 16) & 0x1fff);
                if (hresultFacility == 0xAA3 // FACILITY_ADAL_HTTP in AAD WAM plugin
                     || hresultFacility == 0xAA7 // FACILITY_ADAL_URLMON in AAD WAM plugin
                     || hresultFacility == 0xAA8) // FACILITY_ADAL_INTERNET in AAD WAM plugin
                {
                    return "NoNetwork";
                }

                if (hresultFacility == 0xAA1) // FACILITY_ADAL_DEVELOPER in AAD WAM plugin
                {
                    return "ApiContractViolation";
                }
            }

            return "UnexpectedBrokerError";
        }

    }
}

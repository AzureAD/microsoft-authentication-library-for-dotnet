// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal class AadPlugin : IWamPlugin
    {
        private readonly ICoreLogger _logger;

        public AadPlugin(ICoreLogger logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID)
        {
            var webAccounProvider = await WamBroker.GetAccountProviderAsync("organizations").ConfigureAwait(false);
            WamProxy wamProxy = new WamProxy(webAccounProvider, _logger);

            var webAccounts = await wamProxy.FindAllWebAccountsAsync(clientID).ConfigureAwait(false);

            var msalAccounts = webAccounts
                .Select(webAcc => ConvertToMsalAccountOrNull(webAcc))
                .Where(a => a != null)
                .ToList();

            _logger.Info($"[WAM AAD Provider] GetAccountsAsync converted {webAccounts.Count()} MSAL accounts");
            return msalAccounts;
        }


        private Account ConvertToMsalAccountOrNull(WebAccount webAccount)
        {
            string username = webAccount.UserName;
            string wamId = webAccount.Id;

            if (!webAccount.Properties.TryGetValue("Authority", out string authority))
            {
                _logger.WarningPii(
                    $"[WAM AAD Provider] Could not convert the WAM account {webAccount.UserName} (id: {webAccount.Id}) to an MSAL account because the Authority could not be found",
                    $"[WAM AAD Provider] Could not convert the WAM account {webAccount.Id} to an MSAL account because the Authority could not be found");

                return null;
            }

            string environment = (new Uri(authority)).Host;
            string homeAccountId = GetHomeAccountIdOrNull(webAccount);

            if (homeAccountId != null)
            {
                var msalAccount = new Account(homeAccountId, username, environment);
                return msalAccount;
            }

            return null;
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
                ScopeHelper.GetMsalScopes(authenticationRequestParameters.Scope).AsSingleString(),
                authenticationRequestParameters.ClientId,
                wamPrompt);

            if (setLoginHint)
            {
                request.Properties.Add("LoginHint", authenticationRequestParameters.LoginHint);
            }

            request.Properties.Add("wam_compat", "2.0");
            if (ApiInformation.IsPropertyPresent("Windows.Security.Authentication.Web.Core.WebTokenRequest", "CorrelationId"))
            {
                request.CorrelationId = authenticationRequestParameters.CorrelationId.ToString();
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

        public MsalTokenResponse ParseSuccesfullWamResponse(WebTokenResponse webTokenResponse)
        {
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
                Trace.WriteLine($"Other params {kvp.Key}: {kvp.Value}");
            }

            MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
            {
                AccessToken = webTokenResponse.Token,
                IdToken = idToken,
                CorrelationId = correlationId,
                Scope = scopes,
                ExpiresIn = CoreHelpers.GetDurationFromWindowsTimestamp(expiresOn, _logger),
                ExtendedExpiresIn = CoreHelpers.GetDurationFromWindowsTimestamp(extendedExpiresOn, _logger),
                ClientInfo = clientInfo,
                TokenType = "Bearer",
                WamAccountId = webTokenResponse.WebAccount.Id,
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
                    return "WAM_server_temporarily_unavailable";

                unchecked // as per https://stackoverflow.com/questions/34198173/conversion-of-hresult-between-c-and-c-sharp
                {
                    var hresultFacility = (((errorCode) >> 16) & 0x1fff);
                    if (hresultFacility == 0xAA3 // FACILITY_ADAL_HTTP in AAD WAM plugin
                         || hresultFacility == 0xAA7 // FACILITY_ADAL_URLMON in AAD WAM plugin
                         || hresultFacility == 0xAA8) // FACILITY_ADAL_INTERNET in AAD WAM plugin
                    {
                        return "WAM_no_network";
                    }

                    if (hresultFacility == 0xAA1) // FACILITY_ADAL_DEVELOPER in AAD WAM plugin
                    {
                        return "WAM_internal_error_ApiContractViolation";
                    }
                }
            }

            return "WAM_unexpected_aad_error";
        }

    }
}

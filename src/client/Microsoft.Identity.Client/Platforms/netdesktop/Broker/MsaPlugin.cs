using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.Storage.Streams;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    internal class MsaPlugin : IWamPlugin
    {
        private readonly ICoreLogger _logger;
        private readonly CoreUIParent _uiParent;

        public MsaPlugin(ICoreLogger logger, CoreUIParent uiParent)
        {
            _logger = logger;
            _uiParent = uiParent;
        }

        public async Task<WebTokenRequest> CreateWebTokenRequestAsync(
            WebAccountProvider provider,
            bool isInteractive,
            bool isAccountInWam,
            AuthenticationRequestParameters authenticationRequestParameters)
        {
            // TODO: review logic around adding a new account, as it involves looking at the default account
            bool setLoginHint = false;
            bool addNewAccount = false;

            string loginHint = authenticationRequestParameters.LoginHint ?? authenticationRequestParameters.Account?.Username;

            if (isInteractive && !isAccountInWam)
            {
                if (!string.IsNullOrEmpty(loginHint))
                {
                    setLoginHint = true;
                }
                else
                {
                    addNewAccount = !(await WamBroker.IsDefaultAccountMsaAsync().ConfigureAwait(false));
                }
            }

            var promptType = setLoginHint ? WebTokenRequestPromptType.ForceAuthentication : WebTokenRequestPromptType.Default;

            WebTokenRequest request = new WebTokenRequest(
                provider,
                WamBroker.GetEffectiveScopes(authenticationRequestParameters.Scope),
                authenticationRequestParameters.ClientId,
                promptType);

            if (addNewAccount || setLoginHint)
            {
                request.AppProperties.Add("Client_uiflow", "new_account"); // launch add account flow

                if (setLoginHint)
                {
                    request.AppProperties.Add("LoginHint", loginHint); // prefill username
                }
            }

            request.AppProperties.Add("api-version", "2.0"); // request V2 tokens over V1
            request.AppProperties.Add("oauth2_batch", "1"); // request tokens as OAuth style name/value pairs
            request.AppProperties.Add("x-client-info", "1"); // request client_info
            if (ApiInformation.IsPropertyPresent("Windows.Security.Authentication.Web.Core.WebTokenRequest", "CorrelationId"))
            {
                request.CorrelationId = authenticationRequestParameters.CorrelationId.ToString();
            }
            else
            {
                _logger.Warning("[WAM MSA Plugin] Could not add the correlation ID to the request.");
            }

            return request;
        }

        public string GetHomeAccountIdOrNull(WebAccount webAccount)
        {
            const string msaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad"; // TODO: bogavril - is there a different value in PPE?

            if (!webAccount.Properties.TryGetValue("SafeCustomerId", out string cid))
            {
                _logger.Warning("[WAM MSA Plugin] MSAL account cannot be created without MSA CID");
                return null;
            }

            if (!TryConvertCidToGuid(cid, out string localAccountId))
            {
                _logger.WarningPii($"[WAM MSA Plugin] Invalid CID: {cid}", $"[WAM MSA Provider] Invalid CID, lenght {cid.Length}");
                return null;
            }

            if (localAccountId == null)
            {
                return null;
            }

            string homeAccountId = localAccountId + "." + msaTenantId;
            return homeAccountId;
        }

        /// <summary>
        /// Generally the MSA plugin will NOT return the accounts back to the app. This is due
        /// to privacy concerns. However, some test apps are allowed to do this, hence the code. 
        /// Normal 1st and 3rd party apps must use AcquireTokenInteractive to login first, and then MSAL will
        /// save the account for later use.
        /// </summary>
        public async Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID)
        {
            var webAccounProvider = await WamBroker.GetAccountProviderAsync("consumers").ConfigureAwait(false);
            WamProxy wamProxy = new WamProxy(webAccounProvider, _logger);

            var webAccounts = await wamProxy.FindAllWebAccountsAsync(clientID).ConfigureAwait(false);

            var msalAccounts = webAccounts
                .Select(webAcc => ConvertToMsalAccountOrNull(webAcc))
                .Where(a => a != null)
                .ToList();

            _logger.Info($"[WAM MSA Plugin] GetAccountsAsync converted {webAccounts.Count()} MSAL accounts");
            return msalAccounts;
        }

        private IAccount ConvertToMsalAccountOrNull(WebAccount webAccount)
        {
            const string environment = "login.windows.net"; //TODO: bogavril - other clouds?
            string homeAccountId = GetHomeAccountIdOrNull(webAccount);

            return new Account(homeAccountId, webAccount.UserName, environment);
        }

        // There are two commonly used formats for MSA CIDs:
        //    - hex format, which is a fixed length 16 characters string.
        //    - GUID format, which is the hex value CID prefixed with '00000000-0000-0000-'
        // For example for hex CID value '540648eb0b3075bb' the corresponding GUID representation is
        // '00000000-0000-0000-5406-48eb0b3075bb'
        // This helper method converts MSA CID from the Hex format to GUID format.
        private bool TryConvertCidToGuid(string cid, out string localAccountId)
        {
            if (cid.Length != 16)
            {
                localAccountId = null;
                return false;
            }

            string lowercaseCid = cid.ToLowerInvariant();
            localAccountId = "00000000-0000-0000-" + lowercaseCid.Insert(4, "-");
            return true;
        }


        public string MapTokenRequestError(WebTokenRequestStatus status, uint errorCode, bool isInteractive)
        {
            if (status != WebTokenRequestStatus.UserInteractionRequired)
            {
                return "UnexpectedBrokerError";
            }

            //TODO: need to translate these HR errors  https://github.com/AzureAD/microsoft-authentication-library-for-cpp/blob/75de1a8aee5f83d86941de6081fa351f207d9446/source/windows/broker/MSATokenRequest.cpp#L104
            //  
            // switch errorCode
            // case HRESULT_FROM_WIN32(ERROR_NETWORK_UNREACHABLE):
            //    // Network unavailable. Retry after fixing network.
            //    statusInternal = StatusInternal::NoNetwork;
            //    break;
            //case ONL_E_INVALID_APPLICATION:
            //    // Intentional fall thru to next.
            //case ONL_E_INVALID_AUTHENTICATION_TARGET:
            //    // Permanent failure. Caller not configured correctly, or request has invalid parameters.
            //    statusInternal = StatusInternal::ApiContractViolation;
            //    break;

            return MsalError.InteractionRequired;
        }

        public MsalTokenResponse ParseSuccesfullWamResponse(WebTokenResponse webTokenResponse)
        {
            string msaTokens = webTokenResponse.Token;
            if (string.IsNullOrEmpty(msaTokens))
            {
                //TODO: better to throw exceptions directly to have stack trace
                return new MsalTokenResponse()
                {
                    Error = "wam_msa_internal_error",
                    ErrorDescription = "Bad token format, msaTokens was unexpectedly empty"
                };
            }

            string accessToken = null, idToken = null, clientInfo = null;
            long expiresIn = 0;

            foreach (string keyValuePairString in msaTokens.Split('&'))
            {
                string[] keyValuePair = keyValuePairString.Split('=');
                if (keyValuePair.Length != 2)
                {
                    return new MsalTokenResponse()
                    {
                        Error = "wam_msa_internal_error",
                        ErrorDescription = "Bad token format, expected '=' separated pair"
                    };
                }

                if (keyValuePair[0] == "access_token")
                {
                    accessToken = keyValuePair[1];
                }
                else if (keyValuePair[0] == "id_token")
                {
                    idToken = keyValuePair[1];
                }
                else if (keyValuePair[0] == "client_info")
                {
                    clientInfo = keyValuePair[1];
                }
                else if (keyValuePair[0] == "expires_in")
                {
                    //TODO: IMPORTANT!!! review how to extract expires in
                    //const nlohmann::json j = { { token[0], token[1] } };
                    //const int64_t expiresIn = JsonUtils::ParseIntOrThrow(0x23619640 /* tag_9yzza */, j, "expires_in");
                    //_expiresOn = TimeUtils::GetTimePointNow() + chrono::seconds(expiresIn);
                    expiresIn = long.Parse(keyValuePair[1], CultureInfo.InvariantCulture);
                }
                else
                {
                    // TODO: C++ code saves the remaining properties, but I did not find a reason why
                    // TODO: figure out the other values!
                    Debug.WriteLine($"{keyValuePair[0]}={keyValuePair[1]}");
                }
            }

            MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
            {
                Authority = "TODO",
                AccessToken = accessToken,
                IdToken = idToken,
                CorrelationId = "TODO",
                Scope = "TODO", // TODO: are scopes nicely returned like AAD ?
                ExpiresIn = expiresIn,
                ExtendedExpiresIn = 0, // not supported on MSA
                ClientInfo = clientInfo,
                TokenType = "Bearer", // TODO: bogavril - token type?
                TokenSource = TokenSource.Broker
            };

            return msalTokenResponse;
        }
    }


}

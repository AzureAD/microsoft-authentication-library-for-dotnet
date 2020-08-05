using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    internal class MsaPlugin : IWamPlugin
    {
        private const string MsaErrorCode = "wam_msa_internal_error";
        private readonly ICoreLogger _logger;

        public MsaPlugin(ICoreLogger logger)
        {
            _logger = logger;
        }

        public async Task<WebTokenRequest> CreateWebTokenRequestAsync(
            WebAccountProvider provider,
            bool isInteractive,
            bool isAccountInWam,
            AuthenticationRequestParameters authenticationRequestParameters)
        {
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
                    // TODO: review logic around this
                    addNewAccount = !(await WamBroker.IsDefaultAccountMsaAsync().ConfigureAwait(false));
                }
            }

            var promptType = (setLoginHint || addNewAccount) ? 
                WebTokenRequestPromptType.ForceAuthentication : 
                WebTokenRequestPromptType.Default;

            string scopes = WamBroker.GetEffectiveScopes(authenticationRequestParameters.Scope);
            WebTokenRequest request = new WebTokenRequest(
                provider,
                scopes,
                authenticationRequestParameters.ClientId,
                promptType);

            if (addNewAccount || setLoginHint)
            {
                // TODO: what does this do?
                request.Properties.Add("Client_uiflow", "new_account"); // launch add account flow

                if (setLoginHint)
                {
                    request.Properties.Add("LoginHint", loginHint); // prefill username
                }
            }

            request.Properties.Add("api-version", "2.0"); // request V2 tokens over V1
            request.Properties.Add("oauth2_batch", "1"); // request tokens as OAuth style name/value pairs
            request.Properties.Add("x-client-info", "1"); // request client_info

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
                return MsaErrorCode;
            }

            // TODO: can further drill into errors by looking at HResult 
            // https://github.com/AzureAD/microsoft-authentication-library-for-cpp/blob/75de1a8aee5f83d86941de6081fa351f207d9446/source/windows/broker/MSATokenRequest.cpp#L104

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
                    Error = MsaErrorCode,
                    ErrorDescription = "Bad token format, msaTokens was unexpectedly empty"
                };
            }

            string accessToken = null, idToken = null, clientInfo = null, tokenType = null, scopes = null, correlationId = null;
            long expiresIn = 0;

            foreach (string keyValuePairString in msaTokens.Split('&'))
            {
                string[] keyValuePair = keyValuePairString.Split('=');
                if (keyValuePair.Length != 2)
                {
                    throw new MsalClientException(
                        MsaErrorCode,
                        "Bad token response format, expected '=' separated pair");
                }

                if (keyValuePair[0] == "access_token") //TODO: access token looks wierd!
                {
                    accessToken = keyValuePair[1];
                }
                else if (keyValuePair[0] == "id_token")
                {
                    idToken = keyValuePair[1];
                }
                else if (keyValuePair[0] == "token_type")
                {
                    tokenType = keyValuePair[1];
                }
                else if (keyValuePair[0] == "scope")
                {
                    scopes = keyValuePair[1];
                }
                else if (keyValuePair[0] == "client_info")
                {
                    clientInfo = keyValuePair[1];
                }
                else if (keyValuePair[0] == "expires_in")
                {
                    expiresIn = long.Parse(keyValuePair[1], CultureInfo.InvariantCulture);
                }
                else if (keyValuePair[0] == "correlation")
                {
                    correlationId = keyValuePair[1];
                }
                //else
                //{
                //    // TODO: C++ code saves the remaining properties, but I did not find a reason why                    
                //    Debug.WriteLine($"{keyValuePair[0]}={keyValuePair[1]}");
                //}
            }

            if (string.IsNullOrEmpty(tokenType) || string.Equals("bearer", tokenType, System.StringComparison.InvariantCultureIgnoreCase))
            {
                tokenType = "Bearer";
            }

            if (string.IsNullOrEmpty(scopes))
            {
                throw new MsalClientException(
                    MsaErrorCode,
                    "Bad token response format, no scopes");
            }

            var responseScopes = scopes.Replace("%20", " ");

            MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
            {
                AccessToken = accessToken,
                IdToken = idToken,
                CorrelationId = correlationId,
                Scope = responseScopes,
                ExpiresIn = expiresIn,
                ExtendedExpiresIn = 0, // not supported on MSA
                ClientInfo = clientInfo,
                TokenType = tokenType,
                TokenSource = TokenSource.Broker
            };

            return msalTokenResponse;
        }
    }


}

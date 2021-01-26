using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal class MsaPassthroughHandler : IMsaPassthroughHandler
    {
        public const string TransferTokenScopes =
            "openid profile offline_access service::http://Passport.NET/purpose::PURPOSE_AAD_WAM_TRANSFER";
        private readonly ICoreLogger _logger;
        private readonly IWamPlugin _msaPlugin;
        private readonly IWamProxy _wamProxy;
        private readonly IntPtr _parentHandle;

        public MsaPassthroughHandler(
            ICoreLogger logger,
            IWamPlugin msaPlugin,
            IWamProxy wamProxy,
            IntPtr parentHandle)
        {
            _logger = logger;
            _msaPlugin = msaPlugin;
            _wamProxy = wamProxy;
            _parentHandle = parentHandle;
        }

        public bool IsPassthroughEnabled(AuthenticationRequestParameters authenticationRequestParameters)
        {
            // TODO: change this to a config object
            bool enabled = authenticationRequestParameters.ExtraQueryParameters.TryGetValue("msal_msa_pt", out string val) &&
                string.Equals("1", val);
            _logger.Info("WAM MSA-PT: PassThrough is enabled? " + enabled);

            return enabled;
        }

        public async Task<string> FetchTransferTokenAsync(AuthenticationRequestParameters authenticationRequestParameters, WebAccountProvider accountProvider)
        {
            _logger.Verbose("WAM MSA-PT - fetching transfer token");
            string transferToken = await FetchMsaPassthroughTransferTokenAsync(
                authenticationRequestParameters, accountProvider)
                .ConfigureAwait(false);

            return transferToken;
        }

        public void AddTransferTokenToRequest(
            Windows.Security.Authentication.Web.Core.WebTokenRequest webTokenRequest,
            string transferToken)
        {
            if (!string.IsNullOrEmpty(transferToken))
            {
                webTokenRequest.Properties.Add("SamlAssertion", transferToken);
                webTokenRequest.Properties.Add("SamlAssertionType", "SAMLV1");
            }
        }

        private async Task<string> FetchMsaPassthroughTransferTokenAsync(
           AuthenticationRequestParameters authenticationRequestParameters,
           WebAccountProvider accountProvider)
        {
            // step 1 - get a response from the MSA provider, just to have a WebAccount
            _logger.Info("WAM MSA-PT: Making initial call to MSA provider");
            WebAccount msaPtWebAccount = await FetchWebAccountFromMsaAsync(
                authenticationRequestParameters,
                accountProvider).ConfigureAwait(false);

            // step 2 - get a trasnfer token 
            _logger.Info("WAM MSA-PT: Getting transfer token");
            string transferToken = await FetchTransferTokenAsync(
                accountProvider,
                msaPtWebAccount,
                authenticationRequestParameters.ClientId).ConfigureAwait(true);

            return transferToken;
        }

        private async Task<WebAccount> FetchWebAccountFromMsaAsync(
            AuthenticationRequestParameters authenticationRequestParameters, WebAccountProvider accountProvider)
        {
            // This response has an v1 MSA Access Token, which MSAL should expose to the user
            var webTokenRequestMsa = await _msaPlugin.CreateWebTokenRequestAsync(
                     accountProvider,
                     authenticationRequestParameters,
                     isForceLoginPrompt: false,
                     isInteractive: true,
                     isAccountInWam: false)
                    .ConfigureAwait(false);

            WamAdapters.AddMsalParamsToRequest(authenticationRequestParameters, webTokenRequestMsa);

            var webTokenResponseMsa = await _wamProxy.RequestTokenForWindowAsync(_parentHandle, webTokenRequestMsa)
                .ConfigureAwait(true);

            if (!webTokenResponseMsa.ResponseStatus.IsSuccessStatus())
            {
                var errorResp = WamAdapters.CreateMsalResponseFromWamResponse(webTokenResponseMsa, _msaPlugin, _logger, true);
                throw new MsalServiceException(
                    errorResp.Error,
                    "Error fetching the MSA-PT initial token - " + errorResp.ErrorDescription);
            }

            // Cannot use this WebAccount with the AAD provider
            WebAccount msaPtWebAccount = webTokenResponseMsa.ResponseData[0].WebAccount;
            return msaPtWebAccount;
        }

        private async Task<string> FetchTransferTokenAsync(
            WebAccountProvider accountProvider,
            WebAccount wamAcc,
            string clientId)
        {
            var transferTokenRequest = await _msaPlugin.CreateWebTokenRequestAsync(
                      accountProvider,
                      clientId,
                      TransferTokenScopes)
                      .ConfigureAwait(true);

            var transferResponse = await _wamProxy.RequestTokenForWindowAsync(
                _parentHandle,
                transferTokenRequest,
                wamAcc).ConfigureAwait(false);

            if (!transferResponse.ResponseStatus.IsSuccessStatus())
            {
                var errorResp = WamAdapters.CreateMsalResponseFromWamResponse(transferResponse, _msaPlugin, _logger, true);
                throw new MsalServiceException(
                    errorResp.Error,
                    "Error fetching the MSA-PT transfer token - " + errorResp.ErrorDescription);
            }

            var resp = _msaPlugin.ParseSuccesfullWamResponse(transferResponse.ResponseData[0], out var properties);

            properties.TryGetValue("code", out string code);
            _logger.Info("WAM MSA-PT: Transfer token obtained? " + !string.IsNullOrEmpty(code));

            return code;
        }
    }
}

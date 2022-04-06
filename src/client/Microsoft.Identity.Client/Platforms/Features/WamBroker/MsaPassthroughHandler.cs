// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    /// <summary>
    /// This class only deals with MSA-PT transfer token protocol. MSA-PT is 
    /// a legacy configuration available to First Party apps only.
    /// </summary>
#if NET5_WIN
    [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
#endif
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

        public async Task<string> TryFetchTransferTokenInteractiveAsync(AuthenticationRequestParameters authenticationRequestParameters, WebAccountProvider accountProvider)
        {
            // First party apps can have MSA-PT enabled and can configured to allow MSA users
            _logger.Verbose("WAM MSA-PT - fetching transfer token for interactive flow");

            var webTokenRequestMsa = await _msaPlugin.CreateWebTokenRequestAsync(
                accountProvider,
                authenticationRequestParameters,
                isForceLoginPrompt: false,
                isInteractive: true,
                isAccountInWam: false,
                TransferTokenScopes)
               .ConfigureAwait(false);

            WamAdapters.AddMsalParamsToRequest(authenticationRequestParameters, webTokenRequestMsa, _logger);

            var transferResponse = await _wamProxy.RequestTokenForWindowAsync(_parentHandle, webTokenRequestMsa)
                .ConfigureAwait(true);

            return ExtractTransferToken(
                authenticationRequestParameters.AppConfig.ClientId, 
                transferResponse,
                isInteractive: true);
        }

        public async Task<string> TryFetchTransferTokenSilentDefaultAccountAsync(AuthenticationRequestParameters authenticationRequestParameters, WebAccountProvider accountProvider)
        {
            // First party apps can have MSA-PT enabled and can configured to allow MSA users
            _logger.Verbose("WAM MSA-PT - fetching transfer token for silent flow with default OS account");

            var webTokenRequestMsa = await _msaPlugin.CreateWebTokenRequestAsync(
                accountProvider,
                authenticationRequestParameters,
                isForceLoginPrompt: false,
                isInteractive: false,
                isAccountInWam: true,
                TransferTokenScopes)
               .ConfigureAwait(false);

            WamAdapters.AddMsalParamsToRequest(authenticationRequestParameters, webTokenRequestMsa, _logger);

            var transferResponse = await _wamProxy.GetTokenSilentlyForDefaultAccountAsync(webTokenRequestMsa)
                .ConfigureAwait(true);

            return ExtractTransferToken(
                authenticationRequestParameters.AppConfig.ClientId, 
                transferResponse,
                isInteractive: false);
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

        public async Task<string> TryFetchTransferTokenSilentAsync(AuthenticationRequestParameters authenticationRequestParameters, WebAccount account)
        {
            _logger.Verbose("WAM MSA-PT - fetching transfer token for silent flow");

            var webTokenRequestMsa = await _msaPlugin.CreateWebTokenRequestAsync(
                account.WebAccountProvider,
                authenticationRequestParameters,
                isForceLoginPrompt: false,
                isInteractive: false,
                isAccountInWam: true,
                TransferTokenScopes)
               .ConfigureAwait(false);

            WamAdapters.AddMsalParamsToRequest(authenticationRequestParameters, webTokenRequestMsa, _logger);

            var transferResponse = await _wamProxy.RequestTokenForWindowAsync(
                _parentHandle, 
                webTokenRequestMsa, 
                account)
                    .ConfigureAwait(true);

            return ExtractTransferToken(
                authenticationRequestParameters.AppConfig.ClientId, 
                transferResponse, 
                isInteractive: false);
        }

        private string ExtractTransferToken(
            string clientId, 
            IWebTokenRequestResultWrapper transferResponse, 
            bool isInteractive)
        {
            if (!transferResponse.ResponseStatus.IsSuccessStatus())
            {
                try
                {
                    _ = WamAdapters.CreateMsalResponseFromWamResponse(
                        transferResponse,
                        _msaPlugin,
                        clientId,
                        _logger,
                        isInteractive: isInteractive);
                }
                catch (MsalServiceException exception)
                {
                    _logger.Warning(
                        "WAM MSA-PT: could not get a transfer token, ussually this is because the " +
                        "1st party app is configured for MSA-PT but not configured to login MSA users (signinaudience =2). " +
                        "Error was: " + exception.ErrorCode + " " + exception.Message);

                }

                return null;
            }

            _ = _msaPlugin.ParseSuccessfullWamResponse(transferResponse.ResponseData[0], out var properties);
            properties.TryGetValue("code", out string code);

            // Important: cannot use this WebAccount with the AAD provider
            WebAccount msaPtWebAccount = transferResponse.ResponseData[0].WebAccount;
            _logger.InfoPii($"Obtained a transfer token for {msaPtWebAccount.UserName} ?  {code != null}", $"Obtained a transfer token? {code != null}");

            return code;
        }
    }
}

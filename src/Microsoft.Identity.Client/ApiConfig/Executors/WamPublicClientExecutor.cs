// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_WAM

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal class WamPublicClientExecutor : AbstractExecutor, IPublicClientApplicationExecutor, IClientApplicationBaseExecutor
    {
        private readonly WamPublicClientApplication _wamPublicClientApplication;

        public WamPublicClientExecutor(IServiceBundle serviceBundle, WamPublicClientApplication wamPublicClientApplication)
            : base(serviceBundle, wamPublicClientApplication.AppConfig)
        {
            _wamPublicClientApplication = wamPublicClientApplication;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenInteractiveParameters interactiveParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", "organizations");

            WebTokenRequest request = new WebTokenRequest(provider, "", clientId: requestContext.ServiceBundle.Config.ClientId);
            request.CorrelationId = Guid.NewGuid().ToString();
            //request.Properties["resource"] = resource.Text;

            WebTokenRequestResult result;
            WebAccount webAccount = null;

            // todo(wam): figure out account id to IAccount mapping/lookup
            //if (accountId.Text != "")
            //{
            //    webAccount = await WebAuthenticationCoreManager.FindAccountAsync(provider, accountId.Text);
            //}

            if (webAccount != null)
            {
                result = await WebAuthenticationCoreManager.RequestTokenAsync(request, webAccount);
            }
            else
            {
                result = await WebAuthenticationCoreManager.RequestTokenAsync(request);
            }

            //HandleResult output = new HandleResult((string trace) => this.output.AppendText(trace + "\r\n"));
            //await output.ParseResult(result);

            switch (result.ResponseStatus)
            {
            case WebTokenRequestStatus.Success: // success, account is the same, or was never passed.
            case WebTokenRequestStatus.AccountSwitch: // success, but account switch happended. There was a prompt and user typed diffrent acount from original.
                // accountId.Text = result.ResponseData[0].WebAccount.Id; // saving last account for future use
                break;

            case WebTokenRequestStatus.UserCancel:
                // user unwilling to perform, he closed the window.
                break;

            case WebTokenRequestStatus.ProviderError:
                // some error happened.
                break;

            case WebTokenRequestStatus.AccountProviderNotAvailable:
                // treat it as error.
                break;

            case WebTokenRequestStatus.UserInteractionRequired:
                // should never come as output of RequestToken* methods.
                break;

            }

            throw new NotImplementedException();
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenSilentParameters silentParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);
            throw new NotImplementedException();
        }

        #region Not Relevant For WAM

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByRefreshTokenParameters byRefreshTokenParameters,
            CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenWithDeviceCodeParameters withDeviceCodeParameters,
            CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByIntegratedWindowsAuthParameters integratedWindowsAuthParameters,
            CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByUsernamePasswordParameters usernamePasswordParameters,
            CancellationToken cancellationToken) => throw new NotImplementedException();
        #endregion
    }
}

#endif // SUPPORTS_WAM

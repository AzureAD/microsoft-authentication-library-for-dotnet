// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;

using Windows.Security.Credentials;
using Windows.Security.Authentication.Web.Core;
using Windows.Foundation;
using Microsoft.Identity.Client.Wam;
using System.Runtime.InteropServices.WindowsRuntime;

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

        private IWebAuthenticationCoreManagerInterop GetWAMDesktopUI()
        {
            return (IWebAuthenticationCoreManagerInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(WebAuthenticationCoreManager));
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenInteractiveParameters interactiveParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            Guid returnInterface = typeof(IAsyncOperation<WebTokenRequestResult>).GUID;
            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", "organizations");

            //            WebTokenRequest request = new WebTokenRequest(provider, scope: scope.Text, clientId: clientId.Text);
            WebTokenRequest request = new WebTokenRequest(provider, "", clientId: requestContext.ServiceBundle.Config.ClientId);
            request.CorrelationId = Guid.NewGuid().ToString();
            request.Properties["resource"] = resource.Text;

            // GetWAMDesktopUI needed only for Desktop applications, UWP applications should use WebAuthenticationCoreManager.RequestToken directly.
            // We have separate interop interface only for desktopUI as it require passing window handle which not needed for UWPs.
            IWebAuthenticationCoreManagerInterop desktopUI = GetWAMDesktopUI();

            WebTokenRequestResult result;
            WebAccount webAccount = null;

            // todo(wam): figure out account id to IAccount mapping/lookup
            //if (accountId.Text != "")
            //{
            //    webAccount = await WebAuthenticationCoreManager.FindAccountAsync(provider, accountId.Text);
            //}

            IntPtr windowHandle = (IntPtr)interactiveParameters.UiParent.OwnerWindow;

            if (webAccount != null)
            {
                // we have web account, we don't need to guess, which one to use.
                result = desktopUI.RequestTokenWithWebAccountForWindowAsync(windowHandle, request, webAccount, ref returnInterface);
            }
            else
            {
                // we don't have web account, WAM will try to use default, and prompt for user if it not able to find.
                result = await desktopUI.RequestTokenForWindowAsync(windowHandle, request, ref returnInterface);
            }

            WAMSample.HandleResult output = new WAMSample.HandleResult((string trace) => this.output.AppendText(trace + "\r\n"));
            await output.ParseResult(result);

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

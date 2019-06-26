// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_WAM

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.WsTrust;
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

        private AuthenticationResult CreateAuthenticationResultFromWebTokenRequestResult(WebTokenRequestResult result)
        {
            WebTokenResponse tokenResponse = result.ResponseData[0];
            WebAccount webAccount = tokenResponse.WebAccount;

            string uniqueId = string.Empty;

            // TokenExpiresOn is seconds since January 1, 1601 00:00:00 (Gregorian calendar)
            long tokenExpiresOn = long.Parse(tokenResponse.Properties["TokenExpiresOn"], CultureInfo.InvariantCulture);
            DateTime expiresOn = new DateTime(1601, 1, 1).AddTicks(tokenExpiresOn);

            string tenantId = tokenResponse.Properties["TenantId"];
            var account = WamUtils.CreateMsalAccountFromWebAccount(webAccount);            
            string idToken = string.Empty;
            var returnedScopes = new List<string>();

            return new AuthenticationResult(
                tokenResponse.Token,
                false,
                uniqueId,
                expiresOn,
                expiresOn,
                tenantId,
                account,
                idToken,
                returnedScopes);
        }

        private Task<WebAccountProvider> FindAccountProviderForAuthorityAsync(
            RequestContext requestContext,
            AcquireTokenCommonParameters commonParameters)
        {
            return WamUtils.FindAccountProviderForAuthorityAsync(requestContext.ServiceBundle, commonParameters.AuthorityOverride);
        }

        private async Task<WebAccount> GetWebAccountFromMsalAccountAsync(WebAccountProvider webAccountProvider, IAccount account)
        {
            // TODO(WAM): We'll need to hook GetAccount(s) for PCA when WAM is available in order for normal MSAL account lookup flow to work.
            if (account != null && !string.IsNullOrWhiteSpace(account.HomeAccountId.Identifier))
            {
                return await WebAuthenticationCoreManager.FindAccountAsync(webAccountProvider, account.HomeAccountId.Identifier);
            }
            return null;
        }

        private WebTokenRequest CreateWebTokenRequest(
            WebAccountProvider provider,
            AcquireTokenCommonParameters commonParameters,
            RequestContext requestContext,
            bool forceAuthentication = false )
        {
            string scope = string.Empty;
            WebTokenRequest request = forceAuthentication
                ? new WebTokenRequest(provider, scope: scope, clientId: requestContext.ServiceBundle.Config.ClientId, promptType: WebTokenRequestPromptType.ForceAuthentication)
                : new WebTokenRequest(provider, scope: scope, clientId: requestContext.ServiceBundle.Config.ClientId);

            // Populate extra query parameters.  Any parameter unknown to WAM will be forwarded to server.
            if (commonParameters.ExtraQueryParameters != null)
            {
                foreach (var kvp in commonParameters.ExtraQueryParameters)
                {
                    request.Properties[kvp.Key] = kvp.Value;
                }
            }

            request.CorrelationId = commonParameters.TelemetryCorrelationId.ToString("N");

            // todo(wam): how to take scopes and convert them to proper values for WAM?
            request.Properties["resource"] = "https://graph.microsoft.com";  

            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 6))
            {
                // This feature works correctly since windows RS4, aka 1803
                request.Properties["prompt"] = "select_account";
            }

            return request;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenInteractiveParameters interactiveParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            WebAccountProvider provider = await FindAccountProviderForAuthorityAsync(requestContext, commonParameters).ConfigureAwait(true);
            WebAccount webAccount = await GetWebAccountFromMsalAccountAsync(provider, interactiveParameters.Account).ConfigureAwait(true);
            WebTokenRequest request = CreateWebTokenRequest(provider, commonParameters, requestContext, forceAuthentication: true);

            WebTokenRequestResult result;

            if (webAccount == null)
            {
                if (!string.IsNullOrWhiteSpace(interactiveParameters.LoginHint))
                {
                    request.Properties["LoginHint"] = interactiveParameters.LoginHint;
                }

                result = await WebAuthenticationCoreManager.RequestTokenAsync(request);
            }
            else
            {
                result = await WebAuthenticationCoreManager.RequestTokenAsync(request, webAccount);
            }

            return HandleWebTokenRequestResult(result);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenSilentParameters silentParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            WebAccountProvider provider = await FindAccountProviderForAuthorityAsync(requestContext, commonParameters).ConfigureAwait(true);
            WebAccount webAccount = await GetWebAccountFromMsalAccountAsync(provider, silentParameters.Account).ConfigureAwait(true);
            WebTokenRequest request = CreateWebTokenRequest(provider, commonParameters, requestContext);

            WebTokenRequestResult result = webAccount == null
                ? await WebAuthenticationCoreManager.GetTokenSilentlyAsync(request)
                : await WebAuthenticationCoreManager.GetTokenSilentlyAsync(request, webAccount);

            return HandleWebTokenRequestResult(result);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByIntegratedWindowsAuthParameters integratedWindowsAuthParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            WebAccountProvider provider = await FindAccountProviderForAuthorityAsync(requestContext, commonParameters).ConfigureAwait(true);
            WebTokenRequest request = CreateWebTokenRequest(provider, commonParameters, requestContext, forceAuthentication: true);
            WebTokenRequestResult result = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(request);

            return HandleWebTokenRequestResult(result);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByUsernamePasswordParameters usernamePasswordParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            WebAccountProvider provider = await FindAccountProviderForAuthorityAsync(requestContext, commonParameters).ConfigureAwait(true);
            WebTokenRequest request = CreateWebTokenRequest(provider, commonParameters, requestContext, forceAuthentication: true);

            request.Properties["Username"] = usernamePasswordParameters.Username;
            request.Properties["Password"] = new string(usernamePasswordParameters.Password.PasswordToCharArray());

            WebTokenRequestResult result = await WebAuthenticationCoreManager.RequestTokenAsync(request);

            return HandleWebTokenRequestResult(result);
        }

        private AuthenticationResult HandleWebTokenRequestResult(WebTokenRequestResult result)
        {
            switch (result.ResponseStatus)
            {
            case WebTokenRequestStatus.Success:
            // success, account is the same, or was never passed.
            case WebTokenRequestStatus.AccountSwitch:
                // success, but account switch happended. There was a prompt and user typed diffrent acount from original.
                return CreateAuthenticationResultFromWebTokenRequestResult(result);

            // TODO(WAM): proper error data conversion for exceptions

            case WebTokenRequestStatus.UserCancel:
                // user unwilling to perform, he closed the window.
                throw new OperationCanceledException("WebTokenRequestStatus.UserCancel");

            case WebTokenRequestStatus.ProviderError:
                // some error happened.
                throw new InvalidOperationException($"WebTokenRequestStatus.ProviderError: {result.ResponseError.ErrorMessage}");

            case WebTokenRequestStatus.AccountProviderNotAvailable:
                // treat it as error.
                throw new InvalidOperationException($"WebTokenRequestStatus.AccountProviderNotAvailable");

            case WebTokenRequestStatus.UserInteractionRequired:
                // should never come as output of RequestToken* methods.
                throw new MsalUiRequiredException(result.ResponseError.ErrorCode.ToString(CultureInfo.InvariantCulture), result.ResponseError.ErrorMessage);

            default:
                throw new InvalidOperationException($"Unknown ResponseStatus: {result.ResponseStatus}");
            }
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
        #endregion
    }
}

#endif // SUPPORTS_WAM

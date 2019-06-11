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
using Microsoft.Identity.Client.Instance;
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

            long tokenExpiresOn = long.Parse(tokenResponse.Properties["TokenExpiresOn"], CultureInfo.InvariantCulture);

            string uniqueId = string.Empty;
            DateTime expiresOn = new DateTime(tokenExpiresOn);  // TODO(WAM): verify the tokenExpiresOn value is in TICKS.  If not, change constructor.
            string tenantId = tokenResponse.Properties["TenantId"];
            var account = new Account(webAccount.Id, webAccount.UserName, environment: string.Empty);
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

        private async Task<WebAccountProvider> FindAccountProviderForAuthorityAsync(
            RequestContext requestContext,
            AcquireTokenCommonParameters commonParameters)
        {
            var authority = commonParameters.AuthorityOverride == null
                ? Authority.CreateAuthority(requestContext.ServiceBundle)
                : Authority.CreateAuthorityWithOverride(requestContext.ServiceBundle, commonParameters.AuthorityOverride);

            // TODO(wam): WAM does not like https://login.microsoftonline.com for provider or common for authority
            Uri uri = new Uri(authority.AuthorityInfo.CanonicalAuthority);
            string providerId = $"{uri.Scheme}://{uri.Host}";
            string authorityVal = uri.AbsolutePath.Replace("/", string.Empty);
            // WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(providerId, authorityVal);

            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", "organizations");
            return provider;
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
            RequestContext requestContext)
        {
            string scope = string.Empty;
            WebTokenRequest request = new WebTokenRequest(provider, scope, clientId: requestContext.ServiceBundle.Config.ClientId)
            {
                CorrelationId = commonParameters.TelemetryCorrelationId.ToString("N")
            };
            request.Properties["resource"] = "https://graph.microsoft.com";  // todo(wam): how to take scopes and convert them to proper values for WAM?

            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 6))
            {
                // this feature works correctly since windows RS4, aka 1803
                request.Properties["prompt"] = "select_account"; // to avoid user to eneter credentials
            }

            return request;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenInteractiveParameters interactiveParameters,
            CancellationToken cancellationToken)
        {
            // TODO(WAM): How to force Account Selection / UI Interaction?  AcquireTokenInteractive should always prompt.
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            WebAccountProvider provider = await FindAccountProviderForAuthorityAsync(requestContext, commonParameters).ConfigureAwait(false);
            WebAccount webAccount = await GetWebAccountFromMsalAccountAsync(provider, interactiveParameters.Account).ConfigureAwait(false);
            WebTokenRequest request = CreateWebTokenRequest(provider, commonParameters, requestContext);

            WebTokenRequestResult result = webAccount == null
                ? await WebAuthenticationCoreManager.RequestTokenAsync(request)
                : await WebAuthenticationCoreManager.RequestTokenAsync(request, webAccount);

            return HandleWebTokenRequestResult(result);
        }

        private AuthenticationResult HandleWebTokenRequestResult(WebTokenRequestResult result)
        {
            switch (result.ResponseStatus)
            {
            case WebTokenRequestStatus.Success: // success, account is the same, or was never passed.
            case WebTokenRequestStatus.AccountSwitch: // success, but account switch happended. There was a prompt and user typed diffrent acount from original.
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

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenSilentParameters silentParameters,
            CancellationToken cancellationToken)
        {
            // TODO(WAM): How to ensure this will NEVER prompt and throw MsalUiRequiredException if it fails?
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.TelemetryCorrelationId);

            WebAccountProvider provider = await FindAccountProviderForAuthorityAsync(requestContext, commonParameters).ConfigureAwait(false);
            WebAccount webAccount = await GetWebAccountFromMsalAccountAsync(provider, silentParameters.Account).ConfigureAwait(false);
            WebTokenRequest request = CreateWebTokenRequest(provider, commonParameters, requestContext);

            WebTokenRequestResult result = webAccount == null
                ? await WebAuthenticationCoreManager.RequestTokenAsync(request)
                : await WebAuthenticationCoreManager.RequestTokenAsync(request, webAccount);

            return HandleWebTokenRequestResult(result);
        }

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByIntegratedWindowsAuthParameters integratedWindowsAuthParameters,
            CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByUsernamePasswordParameters usernamePasswordParameters,
            CancellationToken cancellationToken) => throw new NotImplementedException();

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

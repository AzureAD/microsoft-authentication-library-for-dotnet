// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_WAM

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal abstract class WamRequestBase : RequestBase
    {
        protected WamRequestBase(
            IServiceBundle serviceBundle, 
            AuthenticationRequestParameters authenticationRequestParameters, 
            IAcquireTokenParameters acquireTokenParameters) : base(serviceBundle, authenticationRequestParameters, acquireTokenParameters)
        {
        }

        protected WebTokenRequest CreateWebTokenRequest(
            WebAccountProvider provider,
            bool forceAuthentication = false)
        {
            string scope = AuthenticationRequestParameters.Scope.AsSingleString();
            WebTokenRequest request = forceAuthentication
                ? new WebTokenRequest(provider, scope: scope, clientId: AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.ClientId, promptType: WebTokenRequestPromptType.ForceAuthentication)
                : new WebTokenRequest(provider, scope: scope, clientId: AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.ClientId);

            // Populate extra query parameters.  Any parameter unknown to WAM will be forwarded to server.
            if (AuthenticationRequestParameters.ExtraQueryParameters != null)
            {
                foreach (var kvp in AuthenticationRequestParameters.ExtraQueryParameters)
                {
                    request.Properties[kvp.Key] = kvp.Value;
                }
            }

            request.CorrelationId = AuthenticationRequestParameters.RequestContext.CorrelationId.ToString("D");

            // TODO(WAM): verify with server team that this is the proper value
            request.Properties["api-version"] = "2.0";

            // Since we've set api-version=2.0, we can send in scopes in the scope parameter since the WAM providers are now set to v2 protocol and token semantics.
            // Therefore we don't need the resource parameter.
            // TODO(WAM): Update (7/29).  Apparently we _do_ have to have this, but ONLY for MSA/consumers.  So need to figure out how to get this resource URL out of the MSAL request.
            if (provider.Authority == "consumers")
            {
                request.Properties["resource"] = "https://graph.microsoft.com";
            }

            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 6))
            {
                // This feature works correctly since windows RS4, aka 1803
                request.Properties["prompt"] = "select_account";
            }

            return request;
        }

        protected async Task<AuthenticationResult> CreateAuthenticationResultFromWebTokenRequestResultAsync(WebTokenRequestResult result)
        {
            WebTokenResponse tokenResponse = result.ResponseData[0];
            WebAccount webAccount = tokenResponse.WebAccount;

            string uniqueId = string.Empty;

            // TODO(WAM): since we don't get these values from both MSA and AAD (both are only in AAD),
            // should we just NOT pull them out and have a consistent WAM experience that these are not returned?

            DateTime expiresOn = DateTime.UtcNow;

            if (tokenResponse.Properties.ContainsKey("TokenExpiresOn"))
            {
                // TokenExpiresOn is seconds since January 1, 1601 00:00:00 (Gregorian calendar)
                long tokenExpiresOn = long.Parse(tokenResponse.Properties["TokenExpiresOn"], CultureInfo.InvariantCulture);
                expiresOn = new DateTime(1601, 1, 1).AddSeconds(tokenExpiresOn);
            }

            string tenantId = string.Empty;
            if (tokenResponse.Properties.ContainsKey("TenantId"))
            {
                tenantId = tokenResponse.Properties["TenantId"];
            }
            var account = WamUtils.CreateMsalAccountFromWebAccount(webAccount);
            string idToken = string.Empty;
            var returnedScopes = new List<string>();

            // TODO(WAM):  This should also cache account information so that we can call GetAccounts() to determine the proper information
            // needed to do ATS later.
            // do we ALWAYS want to do that (e.g. in case of UsernamePassword?)  or should we move the caching part out to the specific call?

            await AuthenticationRequestParameters.CacheSessionManager.SaveWamResponseAsync(account).ConfigureAwait(false);

            return new AuthenticationResult(
                tokenResponse.Token,
                false,
                uniqueId,
                expiresOn,
                expiresOn,
                tenantId,
                account,
                idToken,
                returnedScopes,
                Guid.NewGuid());  // todo(wam): need to get correlation id from the request to inject here.
        }

        protected Task<WebAccountProvider> FindAccountProviderForAuthorityAsync(
            RequestContext requestContext,
            AcquireTokenCommonParameters commonParameters)
        {
            return WamUtils.FindAccountProviderForAuthorityAsync(requestContext.ServiceBundle, commonParameters.AuthorityOverride);
        }

        protected async Task<AuthenticationResult> HandleWebTokenRequestResultAsync(WebTokenRequestResult result)
        {
            switch (result.ResponseStatus)
            {
                case WebTokenRequestStatus.Success:
                // success, account is the same, or was never passed.
                case WebTokenRequestStatus.AccountSwitch:
                    // success, but account switch happended. There was a prompt and user typed diffrent acount from original.
                    return await CreateAuthenticationResultFromWebTokenRequestResultAsync(result).ConfigureAwait(false);

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
    }
}

#endif // SUPPORTS_WAM

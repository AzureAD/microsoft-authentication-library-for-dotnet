// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Windows.Security.Authentication.Web.Core;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
#if NET5_WIN
    [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
#endif
    internal class WamAdapters
    {
        private const string WamErrorPrefix = "WAM Error ";

        internal static void AddMsalParamsToRequest(
          AuthenticationRequestParameters authenticationRequestParameters,
          WebTokenRequest webTokenRequest,
          string overridenAuthority = null)
        {
            AddExtraParamsToRequest(webTokenRequest, authenticationRequestParameters.ExtraQueryParameters);
            string authority = overridenAuthority ??
                 authenticationRequestParameters.AuthorityManager.OriginalAuthority.AuthorityInfo.CanonicalAuthority;
            bool validate = authenticationRequestParameters.AuthorityInfo.ValidateAuthority;
            AddAuthorityParamToRequest(authority, validate, webTokenRequest); ;
        }

        internal static MsalTokenResponse CreateMsalResponseFromWamResponse(
         IWebTokenRequestResultWrapper wamResponse,
         IWamPlugin wamPlugin,
         string clientId,
         ICoreLogger logger,
         bool isInteractive)
        {
            string internalErrorCode = null;
            string errorMessage;
            string errorCode;

            switch (wamResponse.ResponseStatus)
            {
                case WebTokenRequestStatus.Success:
                    logger.Info("WAM response status success");
                    return wamPlugin.ParseSuccessfullWamResponse(wamResponse.ResponseData[0], out _);

                // Account Switch occurs when a login hint is passed to WAM but the user chooses a different account for login.
                // MSAL treats this as a success scenario
                case WebTokenRequestStatus.AccountSwitch:
                    logger.Info("WAM response status account switch. Treating as success");
                    return wamPlugin.ParseSuccessfullWamResponse(wamResponse.ResponseData[0], out _);

                case WebTokenRequestStatus.UserInteractionRequired:
                    errorCode =
                        wamPlugin.MapTokenRequestError(wamResponse.ResponseStatus, wamResponse.ResponseError?.ErrorCode ?? 0, isInteractive);
                    internalErrorCode = (wamResponse.ResponseError?.ErrorCode ?? 0).ToString(CultureInfo.InvariantCulture);
                    errorMessage = WamErrorPrefix +
                        $"Wam plugin {wamPlugin.GetType()}" +
                        $" Error code: {internalErrorCode}" +
                        $" Error Message: " + wamResponse.ResponseError?.ErrorMessage;
                    break;
                case WebTokenRequestStatus.UserCancel:
                    errorCode = MsalError.AuthenticationCanceledError;
                    errorMessage = MsalErrorMessage.AuthenticationCanceled;
                    break;
                case WebTokenRequestStatus.ProviderError:
                    errorCode =
                        wamPlugin.MapTokenRequestError(wamResponse.ResponseStatus, wamResponse.ResponseError?.ErrorCode ?? 0, isInteractive);
                    errorMessage =
                        $"{WamErrorPrefix} {wamPlugin.GetType()} \n" +
                        $" Error Code: {errorCode} \n" +
                        $" Error Message: {wamResponse.ResponseError?.ErrorMessage} \n"  +
                        $" Possible causes: \n " +
                        $"- Invalid redirect uri - ensure you have configured the following url in the AAD portal App Registration: {GetExpectedRedirectUri(clientId)} \n" +
                        $"- No Internet connection \n" +
                        $"Please see https://aka.ms/msal-net-wam for details about Windows Broker integration";

                internalErrorCode = (wamResponse.ResponseError?.ErrorCode ?? 0).ToString(CultureInfo.InvariantCulture);
                    break;
                default:
                    errorCode = MsalError.UnknownBrokerError;
                    internalErrorCode = wamResponse.ResponseError.ErrorCode.ToString(CultureInfo.InvariantCulture);
                    errorMessage = $"Unknown WebTokenRequestStatus {wamResponse.ResponseStatus} (internal error code {internalErrorCode})";
                    break;
            }

            return new MsalTokenResponse()
            {
                Error = errorCode,
                ErrorCodes = internalErrorCode != null ? new[] { internalErrorCode } : null,
                ErrorDescription = errorMessage
            };
        }

        private static string GetExpectedRedirectUri(string clientId)
        {
#if WINDOWS_APP
            string sid = Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper();            
            return $"ms-appx-web://microsoft.aad.brokerplugin/{sid}";
#else

            return $"ms-appx-web://microsoft.aad.brokerplugin/{clientId}";
#endif
        }

        private static void AddAuthorityParamToRequest(string authority, bool validate, WebTokenRequest webTokenRequest)
        {
            webTokenRequest.Properties.Add(
                            "authority",
                            authority);
            webTokenRequest.Properties.Add(
                "validateAuthority",
                validate ? "yes" : "no");
        }

        private static void AddExtraParamsToRequest(WebTokenRequest webTokenRequest, IDictionary<string, string> extraQueryParameters)
        {
            if (extraQueryParameters != null)
            {
                // MSAL uses instance_aware=true, but WAM calls it discover=home, so we rename the parameter before passing
                // it to WAM.
                foreach (var kvp in extraQueryParameters)
                {
                    string key = kvp.Key;
                    string value = kvp.Value;

                    if (string.Equals("instance_aware", key) && string.Equals("true", value))
                    {
                        key = "discover";
                        value = "home";
                    }

                    webTokenRequest.Properties.Add(key, value);
                }
            }
        }

    }
}

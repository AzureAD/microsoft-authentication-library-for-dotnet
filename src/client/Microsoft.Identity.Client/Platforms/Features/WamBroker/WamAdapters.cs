// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Windows.Security.Authentication.Web.Core;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal class WamAdapters
    {
        private const string WamErrorPrefix = "WAM Error ";

        internal static void AddMsalParamsToRequest(
          AuthenticationRequestParameters authenticationRequestParameters,
          WebTokenRequest webTokenRequest)
        {
            AddExtraParamsToRequest(webTokenRequest, authenticationRequestParameters.ExtraQueryParameters);
            AddAuthorityParamToRequest(authenticationRequestParameters, webTokenRequest);
            AddPOPParamsToRequest(webTokenRequest);
        }

        internal static MsalTokenResponse CreateMsalResponseFromWamResponse(
         IWebTokenRequestResultWrapper wamResponse,
         IWamPlugin wamPlugin,
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
                        $" error code: {internalErrorCode}" +
                        $" error: " + wamResponse.ResponseError?.ErrorMessage;
                    break;
                case WebTokenRequestStatus.UserCancel:
                    errorCode = MsalError.AuthenticationCanceledError;
                    errorMessage = MsalErrorMessage.AuthenticationCanceled;
                    break;
                case WebTokenRequestStatus.ProviderError:
                    errorCode =
                        wamPlugin.MapTokenRequestError(wamResponse.ResponseStatus, wamResponse.ResponseError?.ErrorCode ?? 0, isInteractive);
                    errorMessage =
                        WamErrorPrefix +
                        " " +
                        wamPlugin.GetType() +
                        $" Error Code: {errorCode}." +
                        $" Possible causes: no Internet connection or invalid redirect uri - please see https://aka.ms/msal-net-wam" +
                        $" Details: " + wamResponse.ResponseError?.ErrorMessage;
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

        private static void AddPOPParamsToRequest(WebTokenRequest webTokenRequest)
        {
            // TODO: add POP support by adding "token_type" = "pop" and "req_cnf" = req_cnf
        }

        private static void AddAuthorityParamToRequest(AuthenticationRequestParameters authenticationRequestParameters, WebTokenRequest webTokenRequest)
        {
            webTokenRequest.Properties.Add(
                            "authority",
                            authenticationRequestParameters.UserConfiguredAuthority.AuthorityInfo.CanonicalAuthority);
            webTokenRequest.Properties.Add(
                "validateAuthority",
                authenticationRequestParameters.AuthorityInfo.ValidateAuthority ? "yes" : "no");
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

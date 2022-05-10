// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using static Microsoft.Identity.Client.Broker.RuntimeBroker;

namespace Microsoft.Identity.Client.Broker
{
    internal class WamAdapters
    {
        private const string WamErrorPrefix = "WAM Error ";
        
        //MSA-PT Auth Params
        private const string NativeInteropMsalRequestType = "msal_request_type";
        private const string ConsumersPassthroughRequest = "consumer_passthrough";

        //MSAL Runtime Error Response 
        private enum ResponseStatus : int
        {
            Unexpected = 0,
            Reserved = 1,
            InteractionRequired = 2,
            NoNetwork = 3,
            NetworkTemporarilyUnavailable = 4,
            ServerTemporarilyUnavailable = 5,
            ApiContractViolation = 6,
            UserCanceled = 7,
            ApplicationCanceled = 8,
            IncorrectConfiguration = 9,
            InsufficientBuffer = 10,
            AuthorityUntrusted = 11,
            UserSwitch = 12,
            AccountUnusable = 13,
            UserDataRemovalRequired = 14
        };

        /// <summary>
        /// Create WAM Error Response
        /// </summary>
        /// <param name="authResult"></param>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="MsalClientException"></exception>
        /// <exception cref="MsalUiRequiredException"></exception>
        /// <exception cref="MsalServiceException"></exception>
        internal static MsalException CreateWamErrorResponse(
            NativeInterop.AuthResult authResult,
            AuthenticationRequestParameters authenticationRequestParameters,
            ICoreLogger logger)
        {
            MsalServiceException serviceException = null;
            string internalErrorCode = null;
            string errorMessage;
            int errorCode;

            switch ((int)authResult.Error.Status)
            {
                case (int)ResponseStatus.UserCanceled:
                    throw new MsalClientException(MsalError.AuthenticationCanceledError, MsalErrorMessage.AuthenticationCanceled);

                case (int)ResponseStatus.InteractionRequired:
                case (int)ResponseStatus.AccountUnusable:
                    errorCode = authResult.Error.ErrorCode;
                    internalErrorCode = authResult.Error.Tag.ToString(CultureInfo.InvariantCulture);
                    errorMessage = 
                        $"{WamErrorPrefix} \n" +
                        $" Error Code: {errorCode} \n" +
                        $" Error Message: {authResult.Error.Context} \n" +
                        $" Internal Error Code: {internalErrorCode} \n";
                    throw new MsalUiRequiredException(MsalError.FailedToAcquireTokenSilentlyFromBroker, errorMessage);

                case (int)ResponseStatus.IncorrectConfiguration:
                case (int)ResponseStatus.ApiContractViolation:
                    errorCode = authResult.Error.ErrorCode;
                    internalErrorCode = (authResult.Error.Tag).ToString(CultureInfo.InvariantCulture);
                    errorMessage =
                        $"{WamErrorPrefix} \n" +
                        $" Error Code: {errorCode} \n" +
                        $" Error Message: {authResult.Error.Status} \n" +
                        $" WAM Error Message: {authResult.Error.Context} \n" +
                        $" Internal Error Code: {internalErrorCode} \n" +
                        $" Is Retryable: false \n" +
                        $" Possible causes: \n" +
                        $"- Invalid redirect uri - ensure you have configured the following url in the AAD portal App Registration: {WamAdapters.GetExpectedRedirectUri(authenticationRequestParameters.AppConfig.ClientId)} \n" +
                        $"- No Internet connection \n" +
                        $"Please see https://aka.ms/msal-net-wam for details about Windows Broker integration";

                    serviceException = new MsalServiceException($"WAM_provider_error_{errorCode}", errorMessage);
                    serviceException.IsRetryable = false;
                    throw serviceException;

                case (int)ResponseStatus.NetworkTemporarilyUnavailable:
                case (int)ResponseStatus.NoNetwork:
                case (int)ResponseStatus.ServerTemporarilyUnavailable:
                    errorCode = authResult.Error.ErrorCode;
                    internalErrorCode = (authResult.Error.Tag).ToString(CultureInfo.InvariantCulture);
                    errorMessage =
                        $"{WamErrorPrefix} \n" +
                        $" Error Code: {errorCode} \n" +
                        $" Error Message: {authResult.Error.Status} \n" +
                        $" WAM Error Message: {authResult.Error.Context} \n" +
                        $" Internal Error Code: {internalErrorCode} \n" +
                        $" Is Retryable: true";

                    serviceException = new MsalServiceException(errorCode.ToString(), errorMessage);
                    serviceException.IsRetryable = false;
                    throw serviceException;

                default:
                    errorCode = authResult.Error.ErrorCode;
                    internalErrorCode = (authResult.Error.ErrorCode).ToString(CultureInfo.InvariantCulture);
                    errorMessage = $"Unknown {authResult.Error} (error code {errorCode}) (internal error code {internalErrorCode})";

                    throw new MsalServiceException(MsalError.UnknownBrokerError, errorMessage);
            }
        }

        /// <summary>
        /// Gets the Common Auth Parameters to be passed to Native Interop
        /// </summary>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="isMsaPassthrough"></param>
        /// <returns></returns>
        public static NativeInterop.AuthParameters GetCommonAuthParameters(
            AuthenticationRequestParameters authenticationRequestParameters, 
            bool isMsaPassthrough)
        {
            var authParams = new NativeInterop.AuthParameters
                (authenticationRequestParameters.AppConfig.ClientId,
                authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority);

            //scopes
            authParams.RequestedScopes = string.Join(" ", authenticationRequestParameters.Scope);

            //redirect URI
            authParams.RedirectUri = authenticationRequestParameters.RedirectUri.ToString();

            //MSA-PT
            if (isMsaPassthrough)
                authParams.Properties[NativeInteropMsalRequestType] = ConsumersPassthroughRequest;

            //Client Claims
            if (!string.IsNullOrWhiteSpace(authenticationRequestParameters.ClaimsAndClientCapabilities))
            {
                authParams.DecodedClaims = authenticationRequestParameters.ClaimsAndClientCapabilities;
            }

            //pass extra query parameters if there are any
            if (authenticationRequestParameters.ExtraQueryParameters != null)
            {
                foreach (KeyValuePair<string, string> kvp in authenticationRequestParameters.ExtraQueryParameters)
                {
                    authParams.Properties[kvp.Key] = kvp.Value;
                }
            }

            return authParams;
        }

        /// <summary>
        /// Parse Native Interop AuthResult Response to MSAL Token Response
        /// </summary>
        /// <param name="authResult"></param>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="MsalServiceException"></exception>
        public static MsalTokenResponse ParseRuntimeResponse(
                NativeInterop.AuthResult authResult, 
                AuthenticationRequestParameters authenticationRequestParameters,
                ICoreLogger logger)
        {
            try
            {
                string expiresOn = authResult.ExpiresOn.ToString();
                string correlationId = authenticationRequestParameters.CorrelationId.ToString("D");

                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    logger.Warning("No correlation ID in response");
                    correlationId = null;
                }

                MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
                {
                    AccessToken = authResult.AccessToken,
                    IdToken = authResult.RawIdToken,
                    CorrelationId = correlationId,
                    Scope = authResult.GrantedScopes,
                    ExpiresIn = DateTimeHelpers.GetDurationFromWindowsTimestamp(expiresOn, logger),
                    ClientInfo = authResult.Account.ClientInfo.ToString(),
                    TokenType = "Bearer",
                    WamAccountId = authResult.Account.Id,
                    TokenSource = TokenSource.Broker
                };

                logger.Info("WAM response status success");

                return msalTokenResponse;
            }
            catch (NativeInterop.MsalRuntimeException ex)
            {
                throw new MsalServiceException("wam_failed", $"Could not acquire token using WAM. {ex.Message}");
            }

        }

        /// <summary>
        /// Get WAM Application Redirect URI
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private static string GetExpectedRedirectUri(string clientId)
        {
#if WINDOWS_APP
            string sid = Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper();            
            return $"ms-appx-web://microsoft.aad.brokerplugin/{sid}";
#else

            return $"ms-appx-web://microsoft.aad.brokerplugin/{clientId}";
#endif
        }
    }
}

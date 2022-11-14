// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
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
        private const string WamHeaderTitle = "msal_accounts_control_title";

        //MSAL Runtime Error Response 
        private enum ResponseStatus
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
        /// Create WAM AuthResult Error Response
        /// </summary>
        /// <param name="authResult"></param>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="logger"></param>
        /// <exception cref="MsalClientException"></exception>
        /// <exception cref="MsalUiRequiredException"></exception>
        /// <exception cref="MsalServiceException"></exception>
        internal static void ThrowExceptionFromWamError(
            NativeInterop.AuthResult authResult,
            AuthenticationRequestParameters authenticationRequestParameters,
            ILoggerAdapter logger)
        {
            MsalServiceException serviceException = null;
            string internalErrorCode = authResult.Error.Tag.ToString(CultureInfo.InvariantCulture);
            long errorCode = authResult.Error.ErrorCode;
            string errorMessage;

            logger.Info("[WamBroker] Processing WAM exception");
            logger.Verbose($"[WamBroker] TelemetryData: {authResult.TelemetryData}");

            switch ((ResponseStatus)authResult.Error.Status)
            {
                case ResponseStatus.UserCanceled:
                    logger.Error($"[WamBroker] {MsalError.AuthenticationCanceledError} {MsalErrorMessage.AuthenticationCanceled}");
                    throw new MsalClientException(MsalError.AuthenticationCanceledError, MsalErrorMessage.AuthenticationCanceled);

                case ResponseStatus.InteractionRequired:
                case ResponseStatus.AccountUnusable:
                    errorMessage = 
                        $"{WamErrorPrefix} \n" +
                        $" Error Code: {errorCode} \n" +
                        $" Error Message: {authResult.Error.Context} \n" +
                        $" Internal Error Code: {internalErrorCode} \n";
                    logger.Error($"[WamBroker] {MsalError.FailedToAcquireTokenSilentlyFromBroker} {errorMessage}");
                    throw new MsalUiRequiredException(MsalError.FailedToAcquireTokenSilentlyFromBroker, errorMessage);

                case ResponseStatus.IncorrectConfiguration:
                case ResponseStatus.ApiContractViolation:
                    errorMessage =
                        $"{WamErrorPrefix} \n" +
                        $" Error Code: {errorCode} \n" +
                        $" Error Message: {authResult.Error.Status} \n" +
                        $" WAM Error Message: {authResult.Error.Context} \n" +
                        $" Internal Error Code: {internalErrorCode} \n" +
                        $" Is Retryable: false \n" +
                        $" Possible causes: \n" +
                        $"- Invalid redirect uri - ensure you have configured the following url in the AAD portal App Registration: " +
                        $"{GetExpectedRedirectUri(authenticationRequestParameters.AppConfig.ClientId)} \n" +
                        $"- No Internet connection \n" +
                        $"Please see https://aka.ms/msal-net-wam for details about Windows Broker integration";
                    logger.Error($"[WamBroker] WAM_provider_error_{errorCode} {errorMessage}");
                    serviceException = new MsalServiceException($"WAM_provider_error_{errorCode}", errorMessage);
                    serviceException.IsRetryable = false;
                    throw serviceException;

                case ResponseStatus.NetworkTemporarilyUnavailable:
                case ResponseStatus.NoNetwork:
                case ResponseStatus.ServerTemporarilyUnavailable:
                    errorMessage =
                        $"{WamErrorPrefix} \n" +
                        $" Error Code: {errorCode} \n" +
                        $" Error Message: {authResult.Error.Status} \n" +
                        $" WAM Error Message: {authResult.Error.Context} \n" +
                        $" Internal Error Code: {internalErrorCode} \n" +
                        $" Is Retryable: true";
                    logger.Error($"[WamBroker] WAM_network_error_{errorCode} {errorMessage}");
                    serviceException = new MsalServiceException(errorCode.ToString(), errorMessage);
                    serviceException.IsRetryable = true;
                    throw serviceException;

                default:
                    errorMessage = $"Unknown {authResult.Error} (error code {errorCode}) (internal error code {internalErrorCode})";
                    logger.Verbose($"[WamBroker] {MsalError.UnknownBrokerError} {errorMessage}");
                    throw new MsalServiceException(MsalError.UnknownBrokerError, errorMessage);
            }
        }

        /// <summary>
        /// Create WAM SignOutResult Error Response
        /// </summary>
        /// <param name="signoutResult"></param>
        /// <param name="logger"></param>
        /// <exception cref="MsalServiceException"></exception>
        internal static void ThrowExceptionFromWamError(
            NativeInterop.SignOutResult signoutResult,
            ILoggerAdapter logger)
        {
            logger.Verbose("[WamBroker] Processing WAM exception");
            logger.Verbose($"[WamBroker] TelemetryData: {signoutResult.TelemetryData}");

            string internalErrorCode = signoutResult.Error.Tag.ToString(CultureInfo.InvariantCulture);
            long errorCode = signoutResult.Error.ErrorCode;
            string errorMessage = $"{signoutResult.Error} (error code {errorCode}) (internal error code {internalErrorCode})";

            logger.Verbose($"[WamBroker] {MsalError.WamFailedToSignout} {errorMessage}");
            throw new MsalServiceException(MsalError.WamFailedToSignout, errorMessage);
        }

        /// <summary>
        /// Gets the Common Auth Parameters to be passed to Native Interop
        /// </summary>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="brokerOptions"></param>
        /// <param name="logger"></param>
        public static NativeInterop.AuthParameters GetCommonAuthParameters(
            AuthenticationRequestParameters authenticationRequestParameters, 
            WindowsBrokerOptions brokerOptions,
            ILoggerAdapter logger)
        {
            logger.Verbose("[WamBroker] Validating Common Auth Parameters.");
            ValidateAuthParams(authenticationRequestParameters, logger);

            var authParams = new NativeInterop.AuthParameters
                (authenticationRequestParameters.AppConfig.ClientId,
                authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority.ToString());

            //scopes
            authParams.RequestedScopes = string.Join(" ", authenticationRequestParameters.Scope);

            //WAM redirect URi does not need to be configured by the user
            //this is used internally by the interop to fallback to the browser 
            authParams.RedirectUri = authenticationRequestParameters.RedirectUri.ToString();

            //MSA-PT
            if (brokerOptions.MsaPassthrough)
            {
                authParams.Properties[NativeInteropMsalRequestType] = ConsumersPassthroughRequest;
            }

            //WAM Header Title
            if (!string.IsNullOrEmpty(brokerOptions.HeaderText))
            {
                authParams.Properties[WamHeaderTitle] = brokerOptions.HeaderText;
            }

            //Client Claims
            if (!string.IsNullOrWhiteSpace(authenticationRequestParameters.ClaimsAndClientCapabilities))
            {
                authParams.DecodedClaims = authenticationRequestParameters.ClaimsAndClientCapabilities;
            }

            if (authenticationRequestParameters.AppConfig.MultiCloudSupportEnabled)
            {
                authParams.Properties["instance_aware"] = "true";
            }

            //pass extra query parameters if there are any
            if (authenticationRequestParameters.ExtraQueryParameters != null)
            {
                foreach (KeyValuePair<string, string> kvp in authenticationRequestParameters.ExtraQueryParameters)
                {
                    authParams.Properties[kvp.Key] = kvp.Value;
                }
            }

            AddPopParams(authenticationRequestParameters, authParams);

            logger.Verbose("[WamBroker] Acquired Common Auth Parameters.");

            return authParams;
        }

        /// <summary>
        /// Configures the MSAL Runtime authentication request to use proof of possession .
        /// </summary>
        private static void AddPopParams(AuthenticationRequestParameters authenticationRequestParameters, NativeInterop.AuthParameters authParams)
        {
            // if PopAuthenticationConfiguration is set, proof of possession will be performed via the runtime broker
            if (authenticationRequestParameters.PopAuthenticationConfiguration != null)
            {
                authenticationRequestParameters.RequestContext.Logger.Info("[WamBroker] Proof-of-Possession is configured. Using Proof-of-Possession with broker request. ");
                authParams.PopParams.HttpMethod = authenticationRequestParameters.PopAuthenticationConfiguration.HttpMethod?.Method;
                authParams.PopParams.UriHost = authenticationRequestParameters.PopAuthenticationConfiguration.HttpHost;
                authParams.PopParams.UriPath = authenticationRequestParameters.PopAuthenticationConfiguration.HttpPath;
                authParams.PopParams.Nonce = authenticationRequestParameters.PopAuthenticationConfiguration.Nonce;
            }
        }

        public static MsalTokenResponse HandleResponse(
                NativeInterop.AuthResult authResult,
                AuthenticationRequestParameters authenticationRequestParameters,
                ILoggerAdapter logger, string errorMessage = null)
        {
            MsalTokenResponse msalTokenResponse = null;

            if (authResult.IsSuccess)
            {
                msalTokenResponse = ParseRuntimeResponse(authResult, authenticationRequestParameters, logger);
                logger.Verbose("[WamBroker] Successfully retrieved token.");
            }
            else
            {
                logger.Error($"[WamBroker] {errorMessage} {authResult.Error}");
                ThrowExceptionFromWamError(authResult, authenticationRequestParameters, logger);
            }

            return msalTokenResponse;
        }

        /// <summary>
        /// Parse Native Interop AuthResult Response to MSAL Token Response
        /// </summary>
        /// <param name="authResult"></param>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="logger"></param>
        /// <exception cref="MsalServiceException"></exception>
        public static MsalTokenResponse ParseRuntimeResponse(
                NativeInterop.AuthResult authResult, 
                AuthenticationRequestParameters authenticationRequestParameters,
                ILoggerAdapter logger)
        {
            try
            {
                string correlationId = authenticationRequestParameters.CorrelationId.ToString("D");

                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    logger.Warning("[WamBroker] No correlation ID in response");
                    correlationId = null;
                }
                
                //parsing Pop token from auth header if pop was performed. Otherwise use access token field.
                var token = authResult.IsPopAuthorization ? authResult.AuthorizationHeader.Split(' ')[1] : authResult.AccessToken;

                string authorityUrl = null;

                // workaround for bug https://identitydivision.visualstudio.com/Engineering/_workitems/edit/2047936
                // i.e. environment is not set correctly in multi-cloud apps and home_enviroment is not set
                if (authenticationRequestParameters.AppConfig.MultiCloudSupportEnabled && string.IsNullOrEmpty(authorityUrl))
                {
                    IdToken idToken = IdToken.Parse(authResult.RawIdToken);
                    authorityUrl = idToken.ClaimsPrincipal.FindFirst("iss")?.Value;
                    if (authorityUrl.EndsWith("v2.0"))
                        authorityUrl = authorityUrl.Substring(0, authorityUrl.Length - "v2.0".Length);
                }

                MsalTokenResponse msalTokenResponse = new MsalTokenResponse()
                {                    
                    AuthorityUrl = authorityUrl,
                    AccessToken = token,
                    IdToken = authResult.RawIdToken,
                    CorrelationId = correlationId,
                    Scope = authResult.GrantedScopes,
                    ExpiresIn = (long)(DateTime.SpecifyKind(authResult.ExpiresOn, DateTimeKind.Utc) - DateTimeOffset.UtcNow).TotalSeconds,
                    ClientInfo = authResult.Account.ClientInfo.ToString(),
                    TokenType = authResult.IsPopAuthorization ? Constants.PoPAuthHeaderPrefix : BrokerResponseConst.Bearer,
                    WamAccountId = authResult.Account.AccountId,
                    TokenSource = TokenSource.Broker
                };

                logger.Info("[WamBroker] WAM response status success");

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
        private static string GetExpectedRedirectUri(string clientId)
        {
            return $"ms-appx-web://microsoft.aad.brokerplugin/{clientId}";
        }

        /// <summary>
        /// Validate common auth params
        /// </summary>
        /// <param name="authenticationRequestParameters"></param>
        /// <param name="logger"></param>
        /// <exception cref="MsalClientException"></exception>
        private static void ValidateAuthParams(
            AuthenticationRequestParameters authenticationRequestParameters,
            ILoggerAdapter logger)
        {
            //MSAL Runtime throws an ApiContractViolation Exception with Tag: 0x2039c1cb (InvalidArg)
            //When no scopes are passed, this will check if user is passing scopes
            if (!ScopeHelper.HasNonMsalScopes(authenticationRequestParameters.Scope))                
            {
                logger.Error($"[WamBroker] {MsalError.WamScopesRequired} " +
                    $"{MsalErrorMessage.ScopesRequired}");

                throw new MsalClientException(
                    MsalError.WamScopesRequired,
                    MsalErrorMessage.ScopesRequired);
            }
        }
    }
}

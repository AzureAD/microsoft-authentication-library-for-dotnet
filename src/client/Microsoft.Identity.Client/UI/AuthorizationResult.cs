// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.UI
{
    internal enum AuthorizationStatus
    {
        Success,
        ErrorHttp,
        ProtocolError,
        UserCancel,
        UnknownError
    }

    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class AuthorizationResult
    {
        public static AuthorizationResult FromUri(string webAuthenticationResult)
        {
            if (string.IsNullOrWhiteSpace(webAuthenticationResult))
            {
                return FromStatus(AuthorizationStatus.UnknownError,
                   MsalError.AuthenticationFailed,
                   MsalErrorMessage.AuthorizationServerInvalidResponse);
            }

            var resultUri = new Uri(webAuthenticationResult);

            // NOTE: The Fragment property actually contains the leading '#' character and that must be dropped
            string resultData = resultUri.Query;

            if (string.IsNullOrWhiteSpace(resultData))
            {
                return FromStatus(AuthorizationStatus.UnknownError,
                   MsalError.AuthenticationFailed,
                   MsalErrorMessage.AuthorizationServerInvalidResponse);
            }

            Dictionary<string, string> uriParams = CoreHelpers.ParseKeyValueList(
                resultData.Substring(1), '&', true, null);

            var result = FromParsedValues(uriParams);

            if (uriParams.ContainsKey(TokenResponseClaim.Code))
            {
                result.Code = uriParams[TokenResponseClaim.Code];
            }
            else if (webAuthenticationResult.StartsWith("msauth://", StringComparison.OrdinalIgnoreCase))
            {
                result.Code = webAuthenticationResult;
            }
            else
            {
                return FromStatus(
                   AuthorizationStatus.UnknownError,
                   MsalError.AuthenticationFailed,
                   MsalErrorMessage.AuthorizationServerInvalidResponse);
            }

            return result;
        }
#if !UAP10_0 && !NETSTANDARD1_3
        public static AuthorizationResult FromPostData(byte[] postData)
        {
            if (postData == null)
            {
                return FromStatus(AuthorizationStatus.UnknownError,
                   MsalError.AuthenticationFailed,
                   MsalErrorMessage.AuthorizationServerInvalidResponse);
            }

            var post = System.Text.Encoding.Default.GetString(postData);

            Dictionary<string, string> uriParams = CoreHelpers.ParseKeyValueList(
                post, '&', true, null);

            var result = FromParsedValues(uriParams);

            if (uriParams.ContainsKey(TokenResponseClaim.Code))
            {
                result.Code = uriParams[TokenResponseClaim.Code];
            }
            else
            {
                return FromStatus(
                   AuthorizationStatus.UnknownError,
                   MsalError.AuthenticationFailed,
                   MsalErrorMessage.AuthorizationServerInvalidResponse);
            }

            return result;
        }
#endif

        private static AuthorizationResult FromParsedValues(Dictionary<string, string> parameters)
        {
            if (parameters.ContainsKey(TokenResponseClaim.Error))
            {
                return FromStatus(AuthorizationStatus.ProtocolError,
                    parameters[TokenResponseClaim.Error],
                    parameters.ContainsKey(TokenResponseClaim.ErrorDescription)
                            ? parameters[TokenResponseClaim.ErrorDescription]
                        : null);
            }

            var result = new AuthorizationResult
            {
                Status = AuthorizationStatus.Success
            };

            if (parameters.ContainsKey(OAuth2Parameter.State))
            {
                result.State = parameters[OAuth2Parameter.State];
            }

            if (parameters.ContainsKey(TokenResponseClaim.CloudInstanceHost))
            {
                result.CloudInstanceHost = parameters[TokenResponseClaim.CloudInstanceHost];
            }

            if (parameters.ContainsKey(TokenResponseClaim.ClientInfo))
            {
                result.ClientInfo = parameters[TokenResponseClaim.ClientInfo];
            }

            return result;
        }

        internal static AuthorizationResult FromStatus(AuthorizationStatus status)
        {
            if (status == AuthorizationStatus.Success)
            {
                throw new InvalidOperationException("Use the FromUri builder");
            }

            var result = new AuthorizationResult() { Status = status };

            if (status == AuthorizationStatus.UserCancel)
            {
                result.Error = MsalError.AuthenticationCanceledError;
#if ANDROID
                result.ErrorDescription = MsalErrorMessage.AuthenticationCanceledAndroid;
#else
                result.ErrorDescription = MsalErrorMessage.AuthenticationCanceled;
#endif
            }
            else if (status == AuthorizationStatus.UnknownError)
            {
                result.Error = MsalError.UnknownError;
                result.ErrorDescription = MsalErrorMessage.Unknown;
            }

            return result;
        }

        public static AuthorizationResult FromStatus(AuthorizationStatus status, string error, string errorDescription)
        {
            return new AuthorizationResult()
            {
                Status = status,
                Error = error,
                ErrorDescription = errorDescription,
            };
        }

        public AuthorizationStatus Status { get; private set; }

        [JsonProperty]
        public string Code { get; set; }

        [JsonProperty]
        public string Error { get; set; }

        [JsonProperty]
        public string ErrorDescription { get; set; }

        [JsonProperty]
        public string CloudInstanceHost { get; set; }

        public string ClientInfo { get; set; }

        /// <summary>
        /// A string that is added to each Authorization Request and is expected to be sent back along with the
        /// authorization code. MSAL is responsible for validating that the state sent is identical to the state received.
        /// </summary>
        /// <remarks>
        /// This is in addition to PKCE, which is validated by the server to ensure that the system redeeming the auth code
        /// is the same as the system who asked for it. It protects against XSRF https://openid.net/specs/openid-connect-core-1_0.html
        /// </remarks>
        public string State { get; set; }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
#else
using Microsoft.Identity.Json;
#endif
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

            return FromParsedValues(uriParams, webAuthenticationResult);
        }

        public static AuthorizationResult FromPostData(byte[] postData)
        {
            if (postData == null)
            {
                return FromStatus(AuthorizationStatus.UnknownError,
                   MsalError.AuthenticationFailed,
                   MsalErrorMessage.AuthorizationServerInvalidResponse);
            }
#if !UAP10_0 && !NETSTANDARD2_0
            var post = System.Text.Encoding.Default.GetString(postData).TrimEnd('\0');
#else
            var post = System.Text.Encoding.UTF8.GetString(postData).TrimEnd('\0');
#endif

            Dictionary<string, string> uriParams = CoreHelpers.ParseKeyValueList(
                post, '&', true, null);

            return FromParsedValues(uriParams);
        }

        private static AuthorizationResult FromParsedValues(Dictionary<string, string> parameters, string url = null)
        {
            if (parameters.TryGetValue(TokenResponseClaim.Error, out string error))
            {
                if (parameters.TryGetValue(TokenResponseClaim.ErrorSubcode, out string subcode))
                {
                    if (TokenResponseClaim.ErrorSubcodeCancel.Equals(subcode, StringComparison.OrdinalIgnoreCase))
                    {
                        return FromStatus(AuthorizationStatus.UserCancel);
                    }
                }

                return FromStatus(AuthorizationStatus.ProtocolError,
                    error,
                    parameters.TryGetValue(TokenResponseClaim.ErrorDescription, out string errorDescription)
                            ? errorDescription
                        : null);
            }

            var authResult = new AuthorizationResult
            {
                Status = AuthorizationStatus.Success
            };

            if (parameters.TryGetValue(OAuth2Parameter.State, out string state))
            {
                authResult.State = state;
            }

            if (parameters.TryGetValue(TokenResponseClaim.CloudInstanceHost, out string cloudInstanceHost))
            {
                authResult.CloudInstanceHost = cloudInstanceHost;
            }

            if (parameters.TryGetValue(TokenResponseClaim.ClientInfo, out string clientInfo))
            {
                authResult.ClientInfo = clientInfo;
            }

            if (parameters.TryGetValue(TokenResponseClaim.Code, out string code))
            {
                authResult.Code = code;
            }
            else if (!string.IsNullOrEmpty(url) && url.StartsWith("msauth://", StringComparison.OrdinalIgnoreCase))
            {
                authResult.Code = url;
            }
            else
            {
                return FromStatus(
                   AuthorizationStatus.UnknownError,
                   MsalError.AuthenticationFailed,
                   MsalErrorMessage.AuthorizationServerInvalidResponse);
            }

            return authResult;
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

#if !SUPPORTS_SYSTEM_TEXT_JSON
        [JsonProperty]
#endif
        public string Code { get; set; }

#if !SUPPORTS_SYSTEM_TEXT_JSON
        [JsonProperty]
#endif
        public string Error { get; set; }

#if !SUPPORTS_SYSTEM_TEXT_JSON
        [JsonProperty]
#endif
        public string ErrorDescription { get; set; }

#if !SUPPORTS_SYSTEM_TEXT_JSON
        [JsonProperty]
#endif
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

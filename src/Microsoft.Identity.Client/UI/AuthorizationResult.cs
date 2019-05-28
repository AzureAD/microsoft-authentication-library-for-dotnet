// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

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

    [DataContract]
    internal class AuthorizationResult
    {
        public static AuthorizationResult FromUri(string webAuthenticationResult)
        {
            if (String.IsNullOrWhiteSpace(webAuthenticationResult))
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

            Dictionary<string, string> uriParams = CoreHelpers.ParseKeyValueList(resultData.Substring(1), '&',
                true, null);

            if (uriParams.ContainsKey(TokenResponseClaim.Error))
            {
                return FromStatus(AuthorizationStatus.ProtocolError,
                    uriParams[TokenResponseClaim.Error],
                    uriParams.ContainsKey(TokenResponseClaim.ErrorDescription)
                            ? uriParams[TokenResponseClaim.ErrorDescription]
                        : null);
            }


            var result = new AuthorizationResult();
            result.Status = AuthorizationStatus.Success;

            if (uriParams.ContainsKey(OAuth2Parameter.State))
            {
                result.State = uriParams[OAuth2Parameter.State];
            }

            if (uriParams.ContainsKey(TokenResponseClaim.CloudInstanceHost))
            {
                result.CloudInstanceHost = uriParams[TokenResponseClaim.CloudInstanceHost];
            }

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

        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public string ErrorDescription { get; set; }

        [DataMember]
        public string CloudInstanceHost { get; set; }

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

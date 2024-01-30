// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    internal class MsalServiceExceptionFactory
    {
        static readonly ISet<string> s_nonUiSubErrors = new HashSet<string>(
            new[] { MsalError.ClientMismatch, MsalError.ProtectionPolicyRequired },
            StringComparer.OrdinalIgnoreCase);

        internal static MsalServiceException FromHttpResponse(
          string errorCode,
          string errorMessage,
          HttpResponse httpResponse,
          Exception innerException = null)
        {
            MsalServiceException ex = null;
            OAuth2ResponseBase oAuth2Response = JsonHelper.TryToDeserializeFromJson<OAuth2ResponseBase>(httpResponse?.Body);

            if (IsInvalidGrant(oAuth2Response?.Error, oAuth2Response?.SubError) || IsInteractionRequired(oAuth2Response?.Error))
            {
                if (IsThrottled(oAuth2Response))
                {
                    ex = new MsalUiRequiredException(errorCode, MsalErrorMessage.AadThrottledError, innerException);
                }
                else
                {
                    ex = new MsalUiRequiredException(errorCode, errorMessage, innerException);
                }
            }

            if (string.Equals(oAuth2Response?.Error, MsalError.InvalidClient, StringComparison.OrdinalIgnoreCase))
            {
                ex = new MsalServiceException(
                    MsalError.InvalidClient,
                    MsalErrorMessage.InvalidClient + " Original exception: " + oAuth2Response?.ErrorDescription,
                    innerException);
            }

            ex ??= new MsalServiceException(errorCode, errorMessage, innerException);

            SetHttpExceptionData(ex, httpResponse);

            ex.Claims = oAuth2Response?.Claims;
            ex.CorrelationId = oAuth2Response?.CorrelationId;
            ex.SubError = oAuth2Response?.SubError;
            ex.ErrorCodes = oAuth2Response?.ErrorCodes;

            return ex;
        }

        private static bool IsThrottled(OAuth2ResponseBase oAuth2Response)
        {
            return oAuth2Response.ErrorDescription != null &&
               oAuth2Response.ErrorDescription.StartsWith(Constants.AadThrottledErrorCode);
        }

        internal static MsalServiceException FromBrokerResponse(
          MsalTokenResponse msalTokenResponse,
          string errorMessage)
        {
            string errorCode = msalTokenResponse.Error;
            string correlationId = msalTokenResponse.CorrelationId;
            string subErrorCode = string.IsNullOrEmpty(msalTokenResponse.SubError) ?
                                                                     MsalError.UnknownBrokerError : msalTokenResponse.SubError;
            HttpResponse brokerHttpResponse = msalTokenResponse.HttpResponse;
            MsalServiceException ex = null;

            if (IsAppProtectionPolicyRequired(errorCode, subErrorCode))
            {
                ex = new IntuneAppProtectionPolicyRequiredException(errorCode, subErrorCode)
                {
                    Upn = msalTokenResponse.Upn,
                    AuthorityUrl = msalTokenResponse.AuthorityUrl,
                    TenantId = msalTokenResponse.TenantId,
                    AccountUserId = msalTokenResponse.AccountUserId,
                };
            }

            if (IsInvalidGrant(errorCode, subErrorCode) || IsInteractionRequired(errorCode))
            {
                ex = new MsalUiRequiredException(errorCode, errorMessage);
            }

            if (string.Equals(errorCode, MsalError.InvalidClient, StringComparison.OrdinalIgnoreCase))
            {
                ex = new MsalServiceException(
                    MsalError.InvalidClient,
                    MsalErrorMessage.InvalidClient + " Original exception: " + errorMessage);
            }

            if (ex == null)
            {
                ex = new MsalServiceException(errorCode, errorMessage);
            }

            SetHttpExceptionData(ex, brokerHttpResponse);

            ex.CorrelationId = correlationId;
            ex.SubError = subErrorCode;

            return ex;
        }

        internal static MsalServiceException FromImdsResponse(
          string errorCode,
          string errorMessage,
          HttpResponse httpResponse,
          Exception innerException = null)
        {
            MsalServiceException ex = new MsalServiceException(errorCode, errorMessage, innerException);

            SetHttpExceptionData(ex, httpResponse);

            return ex;
        }

        internal static MsalException CreateManagedIdentityException(
            string errorCode,
            string errorMessage,
            Exception innerException,
            ManagedIdentitySource managedIdentitySource,
            int? statusCode)
        {
            MsalException exception;

            if (statusCode.HasValue)
            {
                exception = new MsalServiceException(errorCode, errorMessage, (int)statusCode, innerException);

                var isRetryable = statusCode switch
                {
                    404 or 408 or 429 or 500 or 503 or 504 => true,
                    _ => false,
                };

                exception.IsRetryable = isRetryable;
            }
            else if (innerException != null)
            {
                exception = new MsalServiceException(errorCode, errorMessage, innerException);
            }
            else
            {
                exception = new MsalServiceException(errorCode, errorMessage);
            }

            exception = DecorateExceptionWithManagedIdentitySource(exception, managedIdentitySource);
            return exception;
        }

        private static MsalException DecorateExceptionWithManagedIdentitySource(
            MsalException exception,
            ManagedIdentitySource managedIdentitySource)
        {
            var result = new Dictionary<string, string>()
            {
                { MsalException.ManagedIdentitySource, managedIdentitySource.ToString() }
            };

            exception.AdditionalExceptionData = result;

            return exception;
        }

        internal static MsalThrottledServiceException FromThrottledAuthenticationResponse(HttpResponse httpResponse)
        {
            MsalServiceException ex = new MsalServiceException(MsalError.RequestThrottled, MsalErrorMessage.AadThrottledError);
            SetHttpExceptionData(ex, httpResponse);
            return new MsalThrottledServiceException(ex);
        }

        private static void SetHttpExceptionData(MsalServiceException ex, HttpResponse httpResponse)
        {
            ex.ResponseBody = httpResponse?.Body;
            ex.StatusCode = httpResponse != null ? (int)httpResponse.StatusCode : 0;
            ex.Headers = httpResponse?.Headers;
        }

        private static bool IsInteractionRequired(string errorCode)
        {
            return string.Equals(errorCode, MsalError.InteractionRequired, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsInvalidGrant(string errorCode, string subErrorCode)
        {
            return string.Equals(errorCode, MsalError.InvalidGrantError, StringComparison.OrdinalIgnoreCase)
                             && IsInvalidGrantSubError(subErrorCode);
        }

        private static bool IsAppProtectionPolicyRequired(string errorCode, string subErrorCode)
        {
#if iOS
            return string.Equals(errorCode, BrokerResponseConst.iOSBrokerProtectionPoliciesRequiredErrorCode, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(subErrorCode, MsalError.ProtectionPolicyRequired, StringComparison.OrdinalIgnoreCase);
#elif ANDROID
            return string.Equals(errorCode, BrokerResponseConst.AndroidUnauthorizedClient, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(subErrorCode, MsalError.ProtectionPolicyRequired, StringComparison.OrdinalIgnoreCase);
#else
            return false;
#endif
        }

        private static bool IsInvalidGrantSubError(string subError)
        {
            if (string.IsNullOrEmpty(subError))
            {
                return true;
            }

            return !s_nonUiSubErrors.Contains(subError);
        }
    }
}

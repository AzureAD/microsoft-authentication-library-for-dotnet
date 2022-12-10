// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client
{
    internal class MsalServiceExceptionFactoryPublic : MsalServiceExceptionFactory
    {
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

            ex ??= new MsalServiceException(errorCode, errorMessage);

            SetHttpExceptionData(ex, brokerHttpResponse);

            ex.CorrelationId = correlationId;
            ex.SubError = subErrorCode;

            return ex;
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
    }
}

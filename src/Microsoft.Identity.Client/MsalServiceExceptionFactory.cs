// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    internal class MsalServiceExceptionFactory
    {
        internal static MsalServiceException FromHttpResponse(
          string errorCode,
          string errorMessage,
          HttpResponse httpResponse,
          Exception innerException = null)
        {
            MsalServiceException ex = null;
            var oAuth2Response = JsonHelper.TryToDeserializeFromJson<OAuth2ResponseBase>(httpResponse?.Body);

            if (string.Equals(oAuth2Response?.Error, MsalError.InvalidGrantError, StringComparison.OrdinalIgnoreCase))
            {
                if (InvalidGrantClassification.IsUiInteractionRequired(oAuth2Response?.SubError))
                {
                    ex = new MsalUiRequiredException(errorCode, errorMessage, innerException);
                }
            }

            if (ex == null)
            {
                ex = new MsalServiceException(errorCode, errorMessage, innerException);
            }


            ex.ResponseBody = httpResponse?.Body;
            ex.StatusCode = httpResponse != null ? (int)httpResponse.StatusCode : 0;
            ex.Headers = httpResponse?.Headers;

            ex.Claims = oAuth2Response?.Claims;
            ex.CorrelationId = oAuth2Response?.CorrelationId;
            ex.SubError = oAuth2Response?.SubError;

            return ex;
        }
    }
}

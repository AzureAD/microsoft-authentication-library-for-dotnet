// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
    internal static class AndroidBrokerInteractiveResponseHelper
    {
        // When the broker responds, we cannot correlate back to a started task. 
        // So we make a simplifying assumption - only one broker open session can exist at a time
        // This semaphore is static to enforce this
        public static SemaphoreSlim ReadyForResponse { get; set; } = new SemaphoreSlim(0);
        public static MsalTokenResponse InteractiveBrokerTokenResponse { get; set; } = null;
        //Since the correlation ID is not returned from the broker response, it must be stored at the beginning of the authentication call and re-injected into the response at the end.
        public static string InteractiveRequestCorrelationId { get; set; }

        internal static void SetBrokerResult(Intent data, int resultCode, ILoggerAdapter unreliableLogger)
        {
            try
            {
                if (data == null)
                {
                    unreliableLogger?.Info("[Android broker] Data is null, stopping. ");
                    return;
                }

                switch (resultCode)
                {
                    case (int)BrokerResponseCode.ResponseReceived:
                        unreliableLogger?.Info("[Android broker] Response received, decoding... ");

                        InteractiveBrokerTokenResponse =
                            MsalTokenResponse.CreateFromAndroidBrokerResponse(
                                data.GetStringExtra(BrokerConstants.BrokerResultV2),
                                InteractiveRequestCorrelationId);
                        break;
                    case (int)BrokerResponseCode.UserCancelled:
                        unreliableLogger?.Info("[Android broker] Response received - user cancelled. ");

                        InteractiveBrokerTokenResponse = new MsalTokenResponse
                        {
                            Error = MsalError.AuthenticationCanceledError,
                            ErrorDescription = MsalErrorMessage.AuthenticationCanceled,
                        };
                        break;
                    case (int)BrokerResponseCode.BrowserCodeError:
                        unreliableLogger?.Info("[Android broker] Response received - error. ");

                        JObject errorResultObj = JObject.Parse(data.GetStringExtra(BrokerConstants.BrokerResultV2) ?? "{}");

                        string error;
                        string errorDescription;
                        
                        if (errorResultObj != null && errorResultObj.Count > 0)
                        {
                            JToken errorToken = errorResultObj[BrokerResponseConst.BrokerErrorCode];
                            error = errorToken?.ToString();
                            
                            JToken errorDescToken = errorResultObj[BrokerResponseConst.BrokerErrorMessage];
                            errorDescription = errorDescToken?.ToString();

                            unreliableLogger?.Error($"[Android broker] error: {error} errorDescription {errorDescription}. ");
                        }
                        else
                        {
                            error = BrokerConstants.BrokerUnknownErrorCode;
                            errorDescription = "[Android broker] Error Code received, but no error could be extracted. ";
                            unreliableLogger?.Error("[Android broker] Error response received, but not error could be extracted. ");
                        }

                        var httpResponse = new HttpResponse();
                        //Get HTTP body from the JSON
                        JToken bodyToken = errorResultObj[BrokerResponseConst.BrokerHttpBody];
                        httpResponse.Body = bodyToken?.ToString();

                        JToken subErrorToken = errorResultObj[BrokerResponseConst.BrokerSubError];
                        JToken tenantIdToken = errorResultObj[BrokerResponseConst.TenantId];
                        JToken upnToken = errorResultObj[BrokerResponseConst.UserName];
                        JToken accountUserIdToken = errorResultObj[BrokerResponseConst.LocalAccountId];
                        JToken authorityUrlToken = errorResultObj[BrokerResponseConst.Authority];

                        InteractiveBrokerTokenResponse = new MsalTokenResponse
                        {
                            Error = error,
                            ErrorDescription = errorDescription,
                            SubError = subErrorToken?.ToString(),
                            HttpResponse = httpResponse,
                            CorrelationId = InteractiveRequestCorrelationId,
                            TenantId = tenantIdToken?.ToString(),
                            Upn = upnToken?.ToString(),
                            AccountUserId = accountUserIdToken?.ToString(),
                            AuthorityUrl = authorityUrlToken?.ToString(),
                        };
                        break;
                    default:
                        unreliableLogger?.Error("[Android broker] Unknown broker response. ");
                        InteractiveBrokerTokenResponse = new MsalTokenResponse
                        {
                            Error = BrokerConstants.BrokerUnknownErrorCode,
                            ErrorDescription = "[Android broker] Broker result not returned. ",
                            CorrelationId = InteractiveRequestCorrelationId
                        };
                        break;
                }
            }
            finally
            {
                ReadyForResponse.Release();
            }
        }
    }
}

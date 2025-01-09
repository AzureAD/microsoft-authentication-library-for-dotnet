// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Threading;
using Android.Content;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

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

                        var errorResult = JsonHelper.DeserializeFromJson<BrokerErrorResult>
                            (data.GetStringExtra(BrokerConstants.BrokerResultV2));

                        string error;
                        string errorDescription;
                        if (errorResult != null)
                        {
                            error = errorResult.BrokerErrorCode;
                            errorDescription = errorResult.BrokerErrorMessage;

                            unreliableLogger?.Error($"[Android broker] error: {error} errorDescription {errorDescription}. ");
                        }
                        else
                        {
                            error = BrokerConstants.BrokerUnknownErrorCode;
                            errorDescription = "[Android broker] Error Code received, but no error could be extracted. ";
                            unreliableLogger?.Error("[Android broker] Error response received, but not error could be extracted. ");
                        }

                        var httpResponse = new HttpResponse
                        {
                            Body = errorResult?.BrokerHttpBody
                        };

                        InteractiveBrokerTokenResponse = new MsalTokenResponse
                        {
                            Error = error,
                            ErrorDescription = errorDescription,
                            SubError = errorResult?.BrokerSubError,
                            HttpResponse = httpResponse,
                            CorrelationId = InteractiveRequestCorrelationId,
                            TenantId = errorResult?.TenantId,
                            Upn = errorResult?.UserName,
                            AccountUserId = errorResult?.LocalAccountId,
                            AuthorityUrl = errorResult?.Authority,
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

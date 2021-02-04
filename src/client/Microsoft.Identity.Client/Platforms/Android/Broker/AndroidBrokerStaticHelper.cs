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
    internal class AndroidBrokerStaticHelper
    {
        // When the broker responds, we cannot correlate back to a started task. 
        // So we make a simplifying assumption - only one broker open session can exist at a time
        // This semaphore is static to enforce this
        public static SemaphoreSlim ReadyForResponse { get; set; } = new SemaphoreSlim(0);
        public static MsalTokenResponse InteractiveBrokerTokenResponse { get; set; } = null;
        //Since the correlation ID is not returned from the broker response, it must be stored at the beginning of the authentication call and re-injected into the response at the end.
        public static string InteractiveRequestCorrelationId { get; set; }

        internal static void SetBrokerResult(Intent data, int resultCode, ICoreLogger unreliableLogger)
        {
            try
            {
                if (data == null)
                {
                    unreliableLogger?.Info("Data is null, stopping. ");
                    return;
                }

                switch (resultCode)
                {
                    case (int)BrokerResponseCode.ResponseReceived:
                        unreliableLogger?.Info("Response received, decoding... ");

                        InteractiveBrokerTokenResponse =
                            MsalTokenResponse.CreateFromAndroidBrokerResponse(
                                data.GetStringExtra(BrokerConstants.BrokerResultV2),
                                InteractiveRequestCorrelationId);
                        break;
                    case (int)BrokerResponseCode.UserCancelled:
                        unreliableLogger?.Info("Response received - user cancelled. ");

                        InteractiveBrokerTokenResponse = new MsalTokenResponse
                        {
                            Error = MsalError.AuthenticationCanceledError,
                            ErrorDescription = MsalErrorMessage.AuthenticationCanceled,
                        };
                        break;
                    case (int)BrokerResponseCode.BrowserCodeError:
                        unreliableLogger?.Info("Response received - error. ");

                        dynamic errorResult = JObject.Parse(data.GetStringExtra(BrokerConstants.BrokerResultV2));
                        string error = null;
                        string errorDescription = null;

                        if (errorResult != null)
                        {
                            error = errorResult[BrokerResponseConst.BrokerErrorCode]?.ToString();
                            errorDescription = errorResult[BrokerResponseConst.BrokerErrorMessage]?.ToString();

                            unreliableLogger?.Error($"error: {error} errorDescription {errorDescription}. ");
                        }
                        else
                        {
                            error = BrokerConstants.BrokerUnknownErrorCode;
                            errorDescription = "Error Code received, but no error could be extracted. ";
                            unreliableLogger?.Error("Error response received, but not error could be extracted. ");
                        }

                        var httpResponse = new HttpResponse();
                        //TODO: figure out how to get status code properly deserialized from JObject
                        httpResponse.Body = errorResult[BrokerResponseConst.BrokerHttpBody]?.ToString();

                        InteractiveBrokerTokenResponse = new MsalTokenResponse
                        {
                            Error = error,
                            ErrorDescription = errorDescription,
                            SubError = errorResult[BrokerResponseConst.BrokerSubError],
                            HttpResponse = httpResponse,
                            CorrelationId = InteractiveRequestCorrelationId
                        };
                        break;
                    default:
                        unreliableLogger?.Error("Unknown broker response. ");
                        InteractiveBrokerTokenResponse = new MsalTokenResponse
                        {
                            Error = BrokerConstants.BrokerUnknownErrorCode,
                            ErrorDescription = "Broker result not returned from android broker. ",
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

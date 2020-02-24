// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using AndroidNative = Android;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Platforms.Android
{
    [AndroidNative.Runtime.Preserve(AllMembers = true)]
    internal class AndroidBroker : IBroker
    {
        // When the broker responds, we cannot correlate back to a started task. 
        // So we make a simplifying assumption - only one broker open session can exist at a time
        // This semaphore is static to enforce this
        private static SemaphoreSlim s_readyForResponse = new SemaphoreSlim(0);

        private static MsalTokenResponse s_androidBrokerTokenResponse = null;
        //Since the correlation ID is not returned from the broker response, it must be stored at the beginning of the authentication call and reinjected into the response at the end.
        private static string s_correlationId;
        private readonly AndroidBrokerHelper _brokerHelper;
        private readonly ICoreLogger _logger;
        private readonly Activity _activity;

        public AndroidBroker(CoreUIParent uiParent, ICoreLogger logger)
        {
            _activity = uiParent?.Activity;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _brokerHelper = new AndroidBrokerHelper(Application.Context, logger);
        }

        public bool CanInvokeBroker()
        {
            bool canInvoke = _brokerHelper.CanSwitchToBroker();
            _logger.Verbose("Can invoke broker? " + canInvoke);

            return canInvoke;
        }

        public async Task<MsalTokenResponse> AcquireTokenUsingBrokerAsync(Dictionary<string, string> brokerPayload)
        {
            s_androidBrokerTokenResponse = null;
            s_correlationId = AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.CorrelationId);
            
            try
            {
                // This task will kick off the broker and will block on the _readyForResponse semaphore
                // When the broker activity ends, SetBrokerResult is called, which releases the semaphore. 
                await AcquireTokenInternalAsync(brokerPayload).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error("Broker Operation Failed to complete. In order to perform brokered authentication on android" +
                    " you need to ensure that you have installed either Intune Company Portal (Version 5.0.4689.0 or greater) or Microsoft Authenticator (6.2001.0140 or greater).");
                if (ex is MsalException)
                    throw;
                else
                    throw new MsalClientException(MsalError.AndroidBrokerOperationFailed, ex.Message, ex);
            }

            return s_androidBrokerTokenResponse;
        }

        private async Task AcquireTokenInternalAsync(IDictionary<string, string> brokerPayload)
        {
            try
            {
                if (brokerPayload.ContainsKey(BrokerParameter.BrokerInstallUrl))
                {
                    _logger.Info("Android Broker - broker payload contains install url");

                    var appLink = AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.BrokerInstallUrl);
                    _logger.Info("Android Broker - Starting ActionView activity to " + appLink);
                    _activity.StartActivity(new Intent(Intent.ActionView, AndroidNative.Net.Uri.Parse(appLink)));

                    throw new MsalClientException(
                        MsalError.BrokerApplicationRequired,
                        MsalErrorMessage.BrokerApplicationRequired);
                }
                await _brokerHelper.InitiateBrokerHandshakeAsync(_activity).ConfigureAwait(false);

                brokerPayload[BrokerParameter.BrokerAccountName] = AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Username);

                // Don't send silent background request if account information is not provided
                if (brokerPayload.ContainsKey(BrokerParameter.IsSilentBrokerRequest))
                {
                    _logger.Verbose("User is specified for silent token request. Starting silent broker request.");
                    string silentResult = await _brokerHelper.GetBrokerAuthTokenSilentlyAsync(brokerPayload, _activity).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(silentResult))
                    {
                        s_androidBrokerTokenResponse = CreateMsalTokenResponseFromResult(silentResult);
                    }
                    else
                    {
                        s_androidBrokerTokenResponse = new MsalTokenResponse
                        {
                            Error = MsalError.BrokerResponseReturnedError,
                            ErrorDescription = "Failed to acquire token silently from the broker. In order to perform brokered authentication on android" +
                    " you need to ensure that you have installed either Intune Company Portal (Version 5.0.4689.0 or greater) or Microsoft Authenticator (6.2001.0140 or greater).",
                        };
                    }
                    return;
                }
                else
                {
                    _logger.Verbose("User is not specified for silent token request");
                }

                _logger.Verbose("Starting Android Broker interactive authentication");

                // onActivityResult will receive the response for this activity.
                // Lauching this activity will switch to the broker app.

                Intent brokerIntent = await _brokerHelper
                    .GetIntentForInteractiveBrokerRequestAsync(brokerPayload, _activity)
                    .ConfigureAwait(false);

                if (brokerIntent != null)
                {
                    try
                    {
                        _logger.Info(
                            "Calling activity pid:" + AndroidNative.OS.Process.MyPid()
                            + " tid:" + AndroidNative.OS.Process.MyTid() + "uid:"
                            + AndroidNative.OS.Process.MyUid());

                        _activity.StartActivityForResult(brokerIntent, 1001);
                    }
                    catch (ActivityNotFoundException e)
                    {
                        _logger.ErrorPiiWithPrefix(e, "Unable to get android activity during interactive broker request");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorPiiWithPrefix(ex, "Broker invocation failed.");
                throw;
            }

            await s_readyForResponse.WaitAsync().ConfigureAwait(false);
        }

        internal static void SetBrokerResult(Intent data, int resultCode)
        {
            try
            {
                if (data == null)
                {
                    return;
                }

                if (resultCode != (int)BrokerResponseCode.ResponseReceived)
                {
                    s_androidBrokerTokenResponse = new MsalTokenResponse
                    {
                        Error = MsalError.BrokerResponseReturnedError,
                        ErrorDescription = data.GetStringExtra(BrokerConstants.BrokerResultV2),
                    };
                }
                else
                {
                    s_androidBrokerTokenResponse = CreateMsalTokenResponseFromResult(data.GetStringExtra(BrokerConstants.BrokerResultV2));
                }
            }
            finally
            {
                s_readyForResponse.Release();
            }
        }

        private static MsalTokenResponse CreateMsalTokenResponseFromResult(string brokerResult)
        {
            if (string.IsNullOrEmpty(brokerResult))
            {
                return null;
            }
            Dictionary<string, string> response = new Dictionary<string, string>();
            dynamic authResult = JObject.Parse(brokerResult);

            response.Add(BrokerResponseConst.Authority, authResult[BrokerResponseConst.Authority].ToString());
            response.Add(BrokerResponseConst.AccessToken, authResult[BrokerResponseConst.AccessToken].ToString());
            response.Add(BrokerResponseConst.IdToken, authResult[BrokerResponseConst.IdToken].ToString());
            response.Add(BrokerResponseConst.CorrelationId, s_correlationId);
            response.Add(BrokerResponseConst.Scope, authResult[BrokerResponseConst.AndroidScopes].ToString());
            response.Add(BrokerResponseConst.ExpiresOn, authResult[BrokerResponseConst.ExpiresOn].ToString());
            response.Add(BrokerResponseConst.ClientInfo, authResult[BrokerResponseConst.ClientInfo].ToString());

            return MsalTokenResponse.CreateFromBrokerResponse(response);
        }
    }
}

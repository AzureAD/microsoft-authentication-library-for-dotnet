// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Accounts;
using Android.App;
using Android.Content;
using AndroidNative = Android;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using System.Globalization;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Json;
using Android.OS;

namespace Microsoft.Identity.Client.Platforms.Android
{
    [AndroidNative.Runtime.Preserve(AllMembers = true)]
    internal class AndroidBroker : IBroker
    {
        private static SemaphoreSlim readyForResponse = null;
        private static MsalTokenResponse resultEx = null;
        private readonly AndroidBrokerHelper _brokerHelper;
        private readonly ICoreLogger _logger;
        private Activity _activity;
        //TODO: Temp variable to hold data not requrned from the broker due to a bug. Will be removed.
        private static Dictionary<string, string> _brokerPayload;

        public AndroidBroker(ICoreLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _brokerHelper = new AndroidBrokerHelper(Application.Context, logger);
        }

        public bool CanInvokeBroker(CoreUIParent uiParent)
        {
            bool canInvoke = _brokerHelper.CanSwitchToBroker();
            _logger.Verbose("Can invoke broker? " + canInvoke);
            _activity = uiParent.Activity;
            return canInvoke;
        }

        public async Task<MsalTokenResponse> AcquireTokenUsingBrokerAsync(Dictionary<string, string> brokerPayload)
        {
            resultEx = null;
            readyForResponse = new SemaphoreSlim(0);
            _brokerPayload = brokerPayload;
            //Need to disable warning for non awaited async call.
#pragma warning disable 4014
            try
            {
                Task.Run(() => AcquireTokenInternalAsync(brokerPayload));
            }
            catch (MsalException Ex)
            {
                _logger.Error("Broker Operation Failed to complete.");
                throw Ex;
            }
#pragma warning restore 4014
            await readyForResponse.WaitAsync().ConfigureAwait(false);
            return resultEx;
        }

        private async Task AcquireTokenInternalAsync(IDictionary<string, string> brokerPayload)
        {
            _brokerHelper.SayHelloToBroker(_activity);

            Context mContext = Application.Context;

            if (_brokerHelper.VerifyUser(AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.LoginHint),
                AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Username)))
            {
                brokerPayload[BrokerParameter.BrokerAccountName] = AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.LoginHint);
                _logger.InfoPii(
                    "It switched to broker for context: " + mContext.PackageName + " login hint: " + 
                    AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.BrokerAccountName),
                    "It switched to broker for context");

                // Don't send silent background request if account information is not provided
                bool hasAccountNameOrUserId = !string.IsNullOrEmpty(AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.BrokerAccountName)) ||
                    !string.IsNullOrEmpty(AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Username));
                if (string.IsNullOrEmpty(AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Claims)) && hasAccountNameOrUserId)
                {
                    _logger.Verbose("User is specified for silent token request");
                    Bundle SilentResult = _brokerHelper.GetAuthTokenSilently(brokerPayload, _activity);
                }
                else
                {
                    _logger.Verbose("User is not specified for silent token request");
                }

                if (resultEx != null && !string.IsNullOrEmpty(resultEx.AccessToken))
                {
                    _logger.Verbose("Token is returned from silent call");
                    readyForResponse.Release();
                    return;
                }
                else
                {
                    _logger.Verbose("Token is not returned from silent backgroud call");
                }

                _logger.Verbose("Starting Authentication Activity");

                if (resultEx == null)
                {
                    _logger.Verbose("Initial request to broker");
                    // Log the initial request but not force a prompt
                }

                // onActivityResult will receive the response for this activity.
                // Lauching this activity will switch to the broker app.
                Intent brokerIntent = _brokerHelper.GetIntentForInteractiveBrokerRequest(brokerPayload, _activity);
                if (brokerIntent != null)
                {
                    try
                    {
                        _logger.Verbose(
                            "Calling activity pid:" + AndroidNative.OS.Process.MyPid()
                            + " tid:" + AndroidNative.OS.Process.MyTid() + "uid:"
                            + AndroidNative.OS.Process.MyUid());

                        _activity.StartActivityForResult(brokerIntent, 1001);
                    }
                    catch (ActivityNotFoundException e)
                    {
                        _logger.ErrorPii(e);
                    }
                }
        }
            else
            {
                throw new MsalException(MsalError.NoBrokerAccountFound, "Please add the selected account to the broker");
            }

            await readyForResponse.WaitAsync().ConfigureAwait(false);
        }

        internal static void SetBrokerResult(Intent data, int resultCode)
        {
            if (data == null)
            {
                readyForResponse.Release();
                return;
            }

            if (resultCode != BrokerResponseCode.ResponseReceived)
            {
                resultEx = new MsalTokenResponse
                {
                    Error = data.GetStringExtra(BrokerConstants.ResponseErrorCode),
                    ErrorDescription = data.GetStringExtra(BrokerConstants.ResponseErrorMessage),
                };
            }
            else
            {
                resultEx = CreateMsalTokenResponseFromResult(data.GetStringExtra(BrokerConstants.BrokerResultV2));
            }

            readyForResponse.Release();
        }

        private static MsalTokenResponse CreateMsalTokenResponseFromResult(string brokerResult)
        {
            Dictionary<string, string> response = new Dictionary<string, string>();
            dynamic authResult = JObject.Parse(brokerResult);

            response.Add(BrokerResponseConst.Authority, authResult[BrokerResponseConst.Authority].ToString());
            response.Add(BrokerResponseConst.AccessToken, authResult[BrokerResponseConst.AccessToken].ToString());
            response.Add(BrokerResponseConst.IdToken, authResult[BrokerResponseConst.IdToken].ToString());
            response.Add(BrokerResponseConst.CorrelationId, AndroidBrokerHelper.GetValueFromBrokerPayload(_brokerPayload, BrokerParameter.CorrelationId));
            response.Add(BrokerResponseConst.Scope, authResult[BrokerResponseConst.AndroidScopes].ToString());
            response.Add(BrokerResponseConst.ExpiresOn, authResult[BrokerResponseConst.ExpiresOn].ToString());
            response.Add(BrokerResponseConst.ClientInfo, authResult[BrokerResponseConst.ClientInfo].ToString());

            return MsalTokenResponse.CreateFromBrokerResponse(response);
        }
    }

    internal static class BrokerResponseCode
    {
        public const int UserCancelled = 2001;
        public const int BrowserCodeError = 2002;
        public const int ResponseReceived = 2004;
    }
}

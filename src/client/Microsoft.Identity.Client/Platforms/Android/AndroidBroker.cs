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
        private static MsalTokenResponse _androidBrokerTokenResponse = null;
        //Since the correlation ID is not returned from the broker response, it must be stored at the beginning of the authentication call and reinjected into the response at the end.
        private static string _correlationId;
        private readonly AndroidBrokerHelper _brokerHelper;
        private readonly ICoreLogger _logger;
        private Activity _activity;

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
            _androidBrokerTokenResponse = null;
            _correlationId = AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.CorrelationId);

            try
            {
                await Task.Run(() => AcquireTokenInternalAsync(brokerPayload)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error("Broker Operation Failed to complete.");
                if (ex is MsalException)
                    throw;
                else
                    throw new MsalException(MsalError.AndroidBrokerOperationFailed, ex.Message, ex);
            }

            return _androidBrokerTokenResponse;
        }

        private void AcquireTokenInternalAsync(IDictionary<string, string> brokerPayload)
        {
            _brokerHelper.InitiateBrokerHandshake(_activity);

            Context mContext = Application.Context;

            brokerPayload[BrokerParameter.BrokerAccountName] = AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.LoginHint);

            // Don't send silent background request if account information is not provided
            if (!string.IsNullOrEmpty(AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.BrokerAccountName)) ||
                !string.IsNullOrEmpty(AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Username)))
            {
                _logger.Verbose("User is specified for silent token request. Starting silent broker request");
                string silentResult = _brokerHelper.GetBrokerAuthTokenSilently(brokerPayload, _activity);
                _androidBrokerTokenResponse = CreateMsalTokenResponseFromResult(silentResult);
                return;
            }
            else
            {
                _logger.Verbose("User is not specified for silent token request");
            }

            _logger.Verbose("Starting Android Broker interactive authentication");

            // onActivityResult will receive the response for this activity.
            // Lauching this activity will switch to the broker app.
            Intent brokerIntent = _brokerHelper.GetIntentForInteractiveBrokerRequest(brokerPayload, _activity);
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
                }
            }
        }

        internal static void SetBrokerResult(Intent data, int resultCode)
        {
            if (data == null)
            {
                return;
            }

            if (resultCode != ((int)BrokerResponseCode.ResponseReceived))
            {
                _androidBrokerTokenResponse = new MsalTokenResponse
                {
                    Error = MsalError.BrokerResponseReturnedError,
                    ErrorDescription = data.GetStringExtra(BrokerConstants.BrokerResultV2),
                };
            }
            else
            {
                _androidBrokerTokenResponse = CreateMsalTokenResponseFromResult(data.GetStringExtra(BrokerConstants.BrokerResultV2));
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
            response.Add(BrokerResponseConst.CorrelationId, _correlationId);
            response.Add(BrokerResponseConst.Scope, authResult[BrokerResponseConst.AndroidScopes].ToString());
            response.Add(BrokerResponseConst.ExpiresOn, authResult[BrokerResponseConst.ExpiresOn].ToString());
            response.Add(BrokerResponseConst.ClientInfo, authResult[BrokerResponseConst.ClientInfo].ToString());

            return MsalTokenResponse.CreateFromBrokerResponse(response);
        }
    }
}

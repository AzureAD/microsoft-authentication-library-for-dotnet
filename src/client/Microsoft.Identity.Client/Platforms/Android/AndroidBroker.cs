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

namespace Microsoft.Identity.Client.Platforms.Android
{
    [AndroidNative.Runtime.Preserve(AllMembers = true)]
    internal class AndroidBroker : IBroker
    {
        private static SemaphoreSlim readyForResponse = null;
        private static MsalTokenResponse resultEx = null;

        private readonly AndroidBrokerHelper _brokerProxy;
        private readonly ICoreLogger _logger;

        private Activity _activity;

        public AndroidBroker(ICoreLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _brokerProxy = new AndroidBrokerHelper(Application.Context, logger);
        }

        public bool CanInvokeBroker(CoreUIParent uiParent)
        {
            bool canInvoke = _brokerProxy.CanSwitchToBroker();
            _logger.Verbose("Can invoke broker? " + canInvoke);
            _activity = uiParent.Activity;
            return canInvoke;
        }

        public async Task<MsalTokenResponse> AcquireTokenUsingBrokerAsync(Dictionary<string, string> brokerPayload)
        {
            resultEx = null;
            readyForResponse = new SemaphoreSlim(0);

            try
            {
                await Task.Run(() => AcquireTokenInternalAsync(brokerPayload)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorPii(ex);
                throw;
            }

            await readyForResponse.WaitAsync().ConfigureAwait(false);
            return resultEx;
        }

        private async Task AcquireTokenInternalAsync(IDictionary<string, string> brokerPayload)
        {
            _brokerProxy.SayHelloToBroker(_activity);

            Context mContext = Application.Context;

            if (_brokerProxy.VerifyUser(GetValueFromBrokerPayload(brokerPayload, BrokerParameter.LoginHint),
                GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Username)))
            {
                brokerPayload[BrokerParameter.BrokerAccountName] = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.LoginHint);
                _logger.InfoPii(
                    "It switched to broker for context: " + mContext.PackageName + " login hint: " + GetValueFromBrokerPayload(brokerPayload, BrokerParameter.BrokerAccountName),
                    "It switched to broker for context");

                // Don't send silent background request if account information is not provided
                bool hasAccountNameOrUserId = !string.IsNullOrEmpty(GetValueFromBrokerPayload(brokerPayload, BrokerParameter.BrokerAccountName)) || !string.IsNullOrEmpty(GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Username));
                if (string.IsNullOrEmpty(GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Claims)) && hasAccountNameOrUserId)
                {
                    _logger.Verbose("User is specified for silent token request");
                    //resultEx = _brokerProxy.GetAuthTokenSilently(brokerPayload, _activity);
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
                Intent brokerIntent = _brokerProxy.GetIntentForBrokerActivity(brokerPayload, _activity);
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
                throw new MsalException(MsalError.NoBrokerAccountFound, "PLease add the selected account to the broker");
            }

            await readyForResponse.WaitAsync().ConfigureAwait(false);
        }

        public string GetValueFromBrokerPayload(IDictionary<string, string> brokerPayload, string key)
        {
            string value;
            if (brokerPayload.TryGetValue(key, out value))
            {
                return value;
            }

            return string.Empty;
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
                try
                {
                    Dictionary<string, string> response = new Dictionary<string, string>();
                    string results = data.GetStringExtra(BrokerConstants.BrokerResultV2);
                    //var /*authResult*/ = JsonHelper.DeserializeFromJson<Dictionary<string, string>>(results);
                    //TODO: fix deserialization here
                    Dictionary<string, string> authResult = JsonConvert.DeserializeObject<Dictionary<string, string>>(results);

                    response.Add(BrokerConstants.AccountAuthority, authResult["authority"]);
                    response.Add(BrokerConstants.AccountAccessToken, authResult["access_token"]);
                    response.Add(BrokerConstants.AccountIdToken, authResult["id_token"]);
                    response.Add(BrokerConstants.AccountExpireDate, authResult["expires_on"]);

                    resultEx = MsalTokenResponse.CreateFromBrokerResponse(response);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            readyForResponse.Release();
        }
    }

    internal static class BrokerResponseCode
    {
        public const int UserCancelled = 2001;
        public const int BrowserCodeError = 2002;
        public const int ResponseReceived = 2004;
    }
}

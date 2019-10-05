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

namespace Microsoft.Identity.Client.Platforms.Android
{
    [AndroidNative.Runtime.Preserve(AllMembers = true)]
    internal class AndroidBroker : IBroker
    {
        private static SemaphoreSlim readyForResponse = null;
        private static MsalTokenResponse resultEx = null;

        private readonly AndroidBrokerProxy _brokerProxy;
        private readonly ICoreLogger _logger;

        private Activity _activity;

        public AndroidBroker(ICoreLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _brokerProxy = new AndroidBrokerProxy(Application.Context, logger);
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
                await Task.Run(() => AcquireTokenInternal(brokerPayload)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorPii(ex);
                throw;
            }
            await readyForResponse.WaitAsync().ConfigureAwait(false);
            return resultEx;
        }

        private void AcquireTokenInternal(IDictionary<string, string> brokerPayload)
        {
            if (brokerPayload.ContainsKey(BrokerParameter.BrokerInstallUrl))
            {
                _logger.Info("Android Broker - broker payload contains install url");

                string url = brokerPayload[BrokerParameter.BrokerInstallUrl];
                Uri uri = new Uri(url);
                string query = uri.Query;
                if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Substring(1);
                }

                Dictionary<string, string> keyPair = CoreHelpers.ParseKeyValueList(query, '&', true, false, null);

                var appLink = keyPair["app_link"];
                _logger.Info("Android Broker - Starting ActionView activity to " + appLink);
                _activity.StartActivity(new Intent(Intent.ActionView, AndroidNative.Net.Uri.Parse(appLink)));

                //throw new AdalException(AdalErrorAndroidEx.BrokerApplicationRequired, AdalErrorMessageAndroidEx.BrokerApplicationRequired);
            }

            Context mContext = Application.Context;

            // BROKER flow intercepts here
            // cache and refresh call happens through the authenticator service
            if (_brokerProxy.VerifyUser(brokerPayload[BrokerParameter.LoginHint],
                brokerPayload[BrokerParameter.Username]))
            {

                brokerPayload[BrokerParameter.BrokerAccountName] = brokerPayload[BrokerParameter.LoginHint];
                _logger.InfoPii(
                    "It switched to broker for context: " + mContext.PackageName + " login hint: " + brokerPayload[BrokerParameter.BrokerAccountName],
                    "It switched to broker for context");

                // Don't send background request, if prompt flag is always or
                // refresh_session
                bool hasAccountNameOrUserId = !string.IsNullOrEmpty(brokerPayload[BrokerParameter.BrokerAccountName]) || !string.IsNullOrEmpty(brokerPayload[BrokerParameter.Username]);
                if (string.IsNullOrEmpty(brokerPayload[BrokerParameter.Claims]) && hasAccountNameOrUserId)
                {
                    _logger.Verbose("User is specified for background token request");
                    resultEx = _brokerProxy.GetAuthTokenInBackground(brokerPayload, _activity);
                }
                else
                {
                    _logger.Verbose("User is not specified for background token request");
                }

                if (resultEx != null && !string.IsNullOrEmpty(resultEx.AccessToken))
                {
                    _logger.Verbose("Token is returned from background call");
                    readyForResponse.Release();
                    return;
                }

                // Launch broker activity
                // if cache and refresh request is not handled.
                // Initial request to authenticator needs to launch activity to
                // record calling uid for the account. This happens for Prompt auto
                // or always behavior.
                _logger.Verbose("Token is not returned from backgroud call");

                // Only happens with callback since silent call does not show UI
                _logger.Verbose("Launch activity for Authenticator");

                _logger.Verbose("Starting Authentication Activity");

                if (resultEx == null)
                {
                    _logger.Verbose("Initial request to authenticator");
                    // Log the initial request but not force a prompt
                }

                if (brokerPayload.ContainsKey(BrokerParameter.SilentBrokerFlow))
                {
                    _logger.Error("Can't invoke the broker in interactive mode because this is a silent flow");
                    throw new MsalException(MsalError.FailedToAcquireTokenSilentlyFromBroker);
                }

                // onActivityResult will receive the response
                // Activity needs to launch to record calling app for this
                // account
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
                //throw new AdalException(AdalErrorAndroidEx.NoBrokerAccountFound, "Add requested account as a Workplace account via Settings->Accounts or set UseBroker=true.");
            }
        }

        internal static void SetBrokerResult(Intent data, int resultCode)
        {
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
                var tokenResponse = new MsalTokenResponse
                {
                    Authority = data.GetStringExtra(BrokerConstants.AccountAuthority),
                    AccessToken = data.GetStringExtra(BrokerConstants.AccountAccessToken),
                    IdTokenString = data.GetStringExtra(BrokerConstants.AccountIdToken),
                    TokenType = "Bearer",
                    ExpiresOn = data.GetLongExtra(BrokerConstants.AccountExpireDate, 0)
                };

                resultEx = tokenResponse.GetResult(AndroidBrokerProxy.ConvertFromTimeT(tokenResponse.ExpiresOn),
                    AndroidBrokerProxy.ConvertFromTimeT(tokenResponse.ExpiresOn));
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

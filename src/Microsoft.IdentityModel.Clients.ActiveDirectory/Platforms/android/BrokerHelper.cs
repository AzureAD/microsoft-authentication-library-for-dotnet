//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Accounts;
using Android.App;
using Android.Content;
using Java.IO;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform
{
    [Android.Runtime.Preserve(AllMembers = true)]
    internal class BrokerHelper
    {
        private static SemaphoreSlim readyForResponse = null;
        private static AuthenticationResultEx resultEx = null;

        private readonly BrokerProxy mBrokerProxy = new BrokerProxy(Application.Context);

        public CallState CallState { get; set; }

        public IPlatformParameters PlatformParameters { get; set; }

        private bool WillUseBroker()
        {
            PlatformParameters pp = PlatformParameters as PlatformParameters;
            if (pp != null)
            {
                return pp.UseBroker;
            }

            return false;
        }

        public bool CanInvokeBroker
        {
            get
            {
                mBrokerProxy.CallState = CallState;
                return WillUseBroker() && mBrokerProxy.CanSwitchToBroker();
            }
        }


        public async Task<AuthenticationResultEx> AcquireTokenUsingBroker(IDictionary<string, string> brokerPayload)
        {
            mBrokerProxy.CallState = CallState;

            resultEx = null;
            readyForResponse = new SemaphoreSlim(0);
            try
            {
                await Task.Run(() => AcquireToken(brokerPayload)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CallState.Logger.Error(null, ex);
                CallState.Logger.ErrorPii(null, ex);
                throw;
            }
            await readyForResponse.WaitAsync().ConfigureAwait(false);
            return resultEx;
        }

        public void AcquireToken(IDictionary<string, string> brokerPayload)
        {

            if (brokerPayload.ContainsKey("broker_install_url"))
            {
                string url = brokerPayload["broker_install_url"];
                Uri uri = new Uri(url);
                string query = uri.Query;
                if (query.StartsWith("?"))
                {
                    query = query.Substring(1);
                }

                Dictionary<string, string> keyPair = EncodingHelper.ParseKeyValueList(query, '&', true, false, null);

                PlatformParameters pp = PlatformParameters as PlatformParameters;
                pp.CallerActivity.StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse(keyPair["app_link"])));

                throw new AdalException(AdalErrorAndroidEx.BrokerApplicationRequired, AdalErrorMessageAndroidEx.BrokerApplicationRequired);
            }

            Context mContext = Application.Context;
            AuthenticationRequest request = new AuthenticationRequest(brokerPayload);
            PlatformParameters platformParams = PlatformParameters as PlatformParameters;

            // BROKER flow intercepts here
            // cache and refresh call happens through the authenticator service
            if (mBrokerProxy.VerifyUser(request.LoginHint,
                request.UserId))
            {
                var msg = "It switched to broker for context: " + mContext.PackageName;
                CallState.Logger.Verbose(null, msg);
                CallState.Logger.VerbosePii(null, msg);

                request.BrokerAccountName = request.LoginHint;

                // Don't send background request, if prompt flag is always or
                // refresh_session
                bool hasAccountNameOrUserId = !string.IsNullOrEmpty(request.BrokerAccountName) || !string.IsNullOrEmpty(request.UserId);
                if (string.IsNullOrEmpty(request.Claims) && hasAccountNameOrUserId)
                {
                    msg = "User is specified for background token request";
                    CallState.Logger.Verbose(null, msg);
                    CallState.Logger.VerbosePii(null, msg);

                    resultEx = mBrokerProxy.GetAuthTokenInBackground(request, platformParams.CallerActivity);
                }
                else
                {
                    msg = "User is not specified for background token request";
                    CallState.Logger.Verbose(null, msg);
                    CallState.Logger.VerbosePii(null, msg);
                }

                if (resultEx != null && resultEx.Result != null && !string.IsNullOrEmpty(resultEx.Result.AccessToken))
                {
                    msg = "Token is returned from background call";
                    CallState.Logger.Verbose(null, msg);
                    CallState.Logger.VerbosePii(null, msg);

                    readyForResponse.Release();
                    return;
                }

                // Launch broker activity
                // if cache and refresh request is not handled.
                // Initial request to authenticator needs to launch activity to
                // record calling uid for the account. This happens for Prompt auto
                // or always behavior.
                msg = "Token is not returned from backgroud call";
                CallState.Logger.Verbose(null, msg);
                CallState.Logger.VerbosePii(null, msg);

                // Only happens with callback since silent call does not show UI
                msg = "Launch activity for Authenticator";
                CallState.Logger.Verbose(null, msg);
                CallState.Logger.VerbosePii(null, msg);

                msg = "Starting Authentication Activity";
                CallState.Logger.Verbose(null, msg);
                CallState.Logger.VerbosePii(null, msg);

                if (resultEx == null)
                {
                    msg = "Initial request to authenticator";
                    CallState.Logger.Verbose(null, msg);
                    CallState.Logger.VerbosePii(null, msg);
                    // Log the initial request but not force a prompt
                }

                if (brokerPayload.ContainsKey("silent_broker_flow"))
                {
                    throw new AdalSilentTokenAcquisitionException();
                }

                // onActivityResult will receive the response
                // Activity needs to launch to record calling app for this
                // account
                Intent brokerIntent = mBrokerProxy.GetIntentForBrokerActivity(request, platformParams.CallerActivity);
                if (brokerIntent != null)
                {
                    try
                    {
                        msg = "Calling activity pid:" + Android.OS.Process.MyPid()
                              + " tid:" + Android.OS.Process.MyTid() + "uid:"
                              + Android.OS.Process.MyUid();
                        CallState.Logger.Verbose(null, msg);
                        CallState.Logger.VerbosePii(null, msg);

                        platformParams.CallerActivity.StartActivityForResult(brokerIntent, 1001);
                    }
                    catch (ActivityNotFoundException e)
                    {
                        CallState.Logger.Error(null, e);
                        CallState.Logger.ErrorPii(null, e);
                    }
                }
            }
            else
            {
                throw new AdalException(AdalErrorAndroidEx.NoBrokerAccountFound, "Add requested account as a Workplace account via Settings->Accounts or set UseBroker=true.");
            }
        }

        internal static void SetBrokerResult(Intent data, int resultCode)
        {
            if (resultCode != BrokerResponseCode.ResponseReceived)
            {
                resultEx = new AuthenticationResultEx
                {
                    Exception =
                        new AdalException(data.GetStringExtra(BrokerConstants.ResponseErrorCode),
                            data.GetStringExtra(BrokerConstants.ResponseErrorMessage))
                };
            }
            else
            {
                var tokenResponse = new TokenResponse
                {
                    Authority = data.GetStringExtra(BrokerConstants.AccountAuthority),
                    AccessToken = data.GetStringExtra(BrokerConstants.AccountAccessToken),
                    IdTokenString = data.GetStringExtra(BrokerConstants.AccountIdToken),
                    TokenType = "Bearer",
                    ExpiresOn = data.GetLongExtra(BrokerConstants.AccountExpireDate, 0)
                };

                resultEx = tokenResponse.GetResult(BrokerProxy.ConvertFromTimeT(tokenResponse.ExpiresOn),
                    BrokerProxy.ConvertFromTimeT(tokenResponse.ExpiresOn));
            }

            readyForResponse.Release();
        }
    }

    internal class CallBackHandler : Java.Lang.Object, IAccountManagerCallback
    {
        public void Run(IAccountManagerFuture future)
        {
        }
    }
}

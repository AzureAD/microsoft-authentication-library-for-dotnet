//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Accounts;
using Android.App;
using Android.Content;
using Java.IO;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class BrokerHelper : IBrokerHelper
    {
        private static SemaphoreSlim readyForResponse = null;
        private static AuthenticationResultEx resultEx = null;

        private BrokerProxy mBrokerProxy = new BrokerProxy(Application.Context);

        public bool SkipBroker { get; set; }

        public IPlatformParameters PlatformParameters { get; set; }

        public bool CanInvokeBroker { get { return !SkipBroker && mBrokerProxy.CanSwitchToBroker(); } }


        public async Task<AuthenticationResultEx> AcquireTokenUsingBroker(IDictionary<string, string> brokerPayload)
        {
            resultEx = null;
            readyForResponse = new SemaphoreSlim(0);
            try
            {
                await Task.Run(() => AcquireToken(brokerPayload));
            }
            catch (Exception exc)
            {
                PlatformPlugin.Logger.Error(null, exc);
                throw exc;
            }
            await readyForResponse.WaitAsync();
            return resultEx;
        }
        public void AcquireToken(IDictionary<string, string> brokerPayload)
        {
            Context mContext = Application.Context;
            AuthenticationRequest request = new AuthenticationRequest(brokerPayload);
            PlatformParameters platformParams = PlatformParameters as PlatformParameters;

            // BROKER flow intercepts here
            // cache and refresh call happens through the authenticator service
            if (mBrokerProxy.VerifyUser(request.LoginHint,
                request.UserId))
            {
                PlatformPlugin.Logger.Verbose(null, "It switched to broker for context: " + mContext.PackageName);
                request.BrokerAccountName = request.LoginHint;

                // Don't send background request, if prompt flag is always or
                // refresh_session
                if (!string.IsNullOrEmpty(request.BrokerAccountName) || !string.IsNullOrEmpty(request.UserId))
                {
                    PlatformPlugin.Logger.Verbose(null, "User is specified for background token request");
                    resultEx = mBrokerProxy.GetAuthTokenInBackground(request, platformParams.CallerActivity);
                }
                else
                {
                    PlatformPlugin.Logger.Verbose(null, "User is not specified for background token request");
                }

                if (resultEx != null && !string.IsNullOrEmpty(resultEx.Result.AccessToken))
                {
                    PlatformPlugin.Logger.Verbose(null, "Token is returned from background call ");
                    readyForResponse.Release();
                    return;
                }

                // Launch broker activity
                // if cache and refresh request is not handled.
                // Initial request to authenticator needs to launch activity to
                // record calling uid for the account. This happens for Prompt auto
                // or always behavior.
                PlatformPlugin.Logger.Verbose(null, "Token is not returned from backgroud call");

                // Only happens with callback since silent call does not show UI
                PlatformPlugin.Logger.Verbose(null, "Launch activity for Authenticator");
                /*mAuthorizationCallback = callbackHandle.callback;
                request.setRequestId(callbackHandle.callback.hashCode());*/
                PlatformPlugin.Logger.Verbose(null, "Starting Authentication Activity");
                /*putWaitingRequest(callbackHandle.callback.hashCode(),
                    new AuthenticationRequestState(callbackHandle.callback.hashCode(), request,
                        callbackHandle.callback));*/
                if (resultEx == null)
                {
                    PlatformPlugin.Logger.Verbose(null, "Initial request to authenticator");
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
                        PlatformPlugin.Logger.Verbose(null, "Calling activity pid:" + Android.OS.Process.MyPid()
                                                            + " tid:" + Android.OS.Process.MyTid() + "uid:"
                                                            + Android.OS.Process.MyUid());
                        platformParams.CallerActivity.StartActivityForResult(brokerIntent, 1001);
                    }
                    catch (ActivityNotFoundException e)
                    {
                        PlatformPlugin.Logger.Error(null, e);
                    }
                }
            }
            else
            {
                throw new AdalException(AdalErrorEx.NoBrokerAccountFound, "Add requested account as a Workplace account via Settings->Accounts or set SkipBroker=false.");
            }
        }
        
        internal static void SetBrokerResult(Intent data, int resultCode)
        {
            if (resultCode != 2004)
            {
                resultEx = new AuthenticationResultEx
                {
                    Exception = new AdalException(AdalError.AuthenticationCanceled, AdalErrorMessage.AuthenticationCanceled)
                };
            }
            else
            {
                string accessToken = data.GetStringExtra("account.access.token");
                DateTimeOffset expiresOn = BrokerProxy.ConvertFromTimeT(data.GetLongExtra("account.expiredate", 0));
                UserInfo userInfo = BrokerProxy.GetUserInfoFromBrokerResult(data.Extras);
                resultEx = new AuthenticationResultEx
                {
                    Result = new AuthenticationResult("Bearer", accessToken, expiresOn)
                    {
                        UserInfo = userInfo
                    }
                };
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
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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [Android.Runtime.Preserve(AllMembers = true)]
    class BrokerHelper : IBrokerHelper
    {
        private static SemaphoreSlim readyForResponse = null;
        private static AuthenticationResultEx resultEx = null;

        private BrokerProxy mBrokerProxy = new BrokerProxy(Application.Context);

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

        public bool CanInvokeBroker { get { return WillUseBroker() && mBrokerProxy.CanSwitchToBroker(); } }


        public async Task<AuthenticationResultEx> AcquireTokenUsingBroker(IDictionary<string, string> brokerPayload)
        {
            resultEx = null;
            readyForResponse = new SemaphoreSlim(0);
            try
            {
                await Task.Run(() => AcquireToken(brokerPayload)).ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                PlatformPlugin.Logger.Error(null, exc);
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

                if (resultEx != null && resultEx.Result != null && !string.IsNullOrEmpty(resultEx.Result.AccessToken))
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
                PlatformPlugin.Logger.Verbose(null, "Starting Authentication Activity");
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
                string accessToken = data.GetStringExtra(BrokerConstants.AccountAccessToken);
                DateTimeOffset expiresOn = BrokerProxy.ConvertFromTimeT(data.GetLongExtra(BrokerConstants.AccountExpireDate, 0));
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

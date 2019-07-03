// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Utils;
using UIKit;
using Foundation;
using System;
using CoreFoundation;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    /// <summary>
    /// Handles requests which invoke the broker. This is only for mobile (iOS and Android) scenarios.
    /// </summary>
    internal class iOSBroker : NSObject, IBroker
    {
        private static SemaphoreSlim s_brokerResponseReady = null;
        private static NSUrl s_brokerResponse = null;

        private readonly IServiceBundle _serviceBundle;

        public iOSBroker(IServiceBundle serviceBundle)
        {
            _serviceBundle = serviceBundle;
        }

        public bool CanInvokeBroker(CoreUIParent uiParent)
        {
            if (uiParent?.CallerViewController == null)
            {
                _serviceBundle.DefaultLogger.Error(iOSBrokerConstants.CallerViewControllerIsNullCannotInvokeBroker);
                throw new MsalClientException(MsalError.UIViewControllerRequiredForiOSBroker, MsalErrorMessage.UIViewControllerIsRequiredToInvokeiOSBroker);
            }

            _serviceBundle.DefaultLogger.Info(iOSBrokerConstants.CanInvokeBroker + _serviceBundle.Config.IsBrokerEnabled);

            var result = false;

            uiParent.CallerViewController.InvokeOnMainThread(() =>
            {
                result = UIApplication.SharedApplication.CanOpenUrl(new NSUrl(BrokerParameter.BrokerV2));
            });

            if (!result)
            {
                _serviceBundle.DefaultLogger.Info(result + iOSBrokerConstants.CanInvokeBrokerReturnsFalseMessage);
            }

            return result;
        }

        public async Task<MsalTokenResponse> AcquireTokenUsingBrokerAsync(Dictionary<string, string> brokerPayload)
        {
            AddIosSpecificParametersToPayload(brokerPayload);

            await InvokeIosBrokerAsync(brokerPayload).ConfigureAwait(false);

            return ProcessBrokerResponse();
        }

        private void AddIosSpecificParametersToPayload(Dictionary<string, string> brokerPayload)
        {
            string encodedBrokerKey = Base64UrlHelpers.Encode(BrokerKeyHelper.GetRawBrokerKey(_serviceBundle.DefaultLogger));
            brokerPayload[iOSBrokerConstants.BrokerKey] = encodedBrokerKey;
            brokerPayload[iOSBrokerConstants.MsgProtocolVer] = BrokerParameter.MsgProtocolVersion3;

            if (brokerPayload.ContainsKey(iOSBrokerConstants.Claims))
            {
                brokerPayload.Add(iOSBrokerConstants.SkipCache, BrokerParameter.SkipCache);
                string claims = Base64UrlHelpers.Encode(brokerPayload[BrokerParameter.Claims]);
                brokerPayload[BrokerParameter.Claims] = claims;
            }
        }

        private async Task InvokeIosBrokerAsync(Dictionary<string, string> brokerPayload)
        {
            s_brokerResponseReady = new SemaphoreSlim(0);

            if (brokerPayload.ContainsKey(BrokerParameter.BrokerInstallUrl))
            {
                _serviceBundle.DefaultLogger.Info(iOSBrokerConstants.BrokerPayloadContainsInstallUrl);

                string url = brokerPayload[BrokerParameter.BrokerInstallUrl];
                Uri uri = new Uri(url);
                string query = uri.Query;

                if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Substring(1);
                }

                _serviceBundle.DefaultLogger.Info(iOSBrokerConstants.InvokeIosBrokerAppLink);

                Dictionary<string, string> keyPair = CoreHelpers.ParseKeyValueList(query, '&', true, false, null);

                _serviceBundle.DefaultLogger.Info(iOSBrokerConstants.StartingActionViewActivity + iOSBrokerConstants.AppLink);

                DispatchQueue.MainQueue.DispatchAsync(() => UIApplication.SharedApplication.OpenUrl(new NSUrl(keyPair[iOSBrokerConstants.AppLink])));

                throw new MsalClientException(MsalErrorIOSEx.BrokerApplicationRequired, MsalErrorMessageIOSEx.BrokerApplicationRequired);
            }

            else
            {
                _serviceBundle.DefaultLogger.Info(iOSBrokerConstants.InvokeTheIosBroker);

                NSUrl url = new NSUrl(iOSBrokerConstants.InvokeBroker + brokerPayload.ToQueryParameter());

                _serviceBundle.DefaultLogger.VerbosePii(iOSBrokerConstants.BrokerPayloadPii + brokerPayload.ToQueryParameter(),

                iOSBrokerConstants.BrokerPayloadNoPii + brokerPayload.Count);

                DispatchQueue.MainQueue.DispatchAsync(() => UIApplication.SharedApplication.OpenUrl(url));
            }

            await s_brokerResponseReady.WaitAsync().ConfigureAwait(false);
        }

        private MsalTokenResponse ProcessBrokerResponse()
        {
            string[] keyValuePairs = s_brokerResponse.Query.Split('&');
            Dictionary<string, string> responseDictionary = new Dictionary<string, string>();

            foreach (string pair in keyValuePairs)
            {
                string[] keyValue = pair.Split('=');
                responseDictionary[keyValue[0]] = CoreHelpers.UrlDecode(keyValue[1]);

                if (responseDictionary[keyValue[0]].Equals("(null)", StringComparison.OrdinalIgnoreCase)
                    && keyValue[0].Equals(iOSBrokerConstants.Code, StringComparison.OrdinalIgnoreCase))
                {
                    responseDictionary[iOSBrokerConstants.Error] = iOSBrokerConstants.BrokerError;

                    _serviceBundle.DefaultLogger.VerbosePii(iOSBrokerConstants.BrokerResponseValuesPii + keyValue.ToString(),

                    iOSBrokerConstants.BrokerResponseContainsError);
                }
            }

            _serviceBundle.DefaultLogger.Verbose(iOSBrokerConstants.ProcessBrokerResponse + responseDictionary.Count);

            return ResultFromBrokerResponse(responseDictionary);
        }

        private MsalTokenResponse ResultFromBrokerResponse(Dictionary<string, string> responseDictionary)
        {
            MsalTokenResponse brokerTokenResponse;

            if (responseDictionary.ContainsKey(iOSBrokerConstants.Error) || responseDictionary.ContainsKey(iOSBrokerConstants.ErrorDescription))
            {
                return MsalTokenResponse.CreateFromBrokerResponse(responseDictionary);
            }

            string expectedHash = responseDictionary[iOSBrokerConstants.ExpectedHash];
            string encryptedResponse = responseDictionary[iOSBrokerConstants.EncryptedResponsed];
            string decryptedResponse = BrokerKeyHelper.DecryptBrokerResponse(encryptedResponse, _serviceBundle.DefaultLogger);
            string responseActualHash = _serviceBundle.PlatformProxy.CryptographyManager.CreateSha256Hash(decryptedResponse);
            byte[] rawHash = Convert.FromBase64String(responseActualHash);
            string hash = BitConverter.ToString(rawHash);

            if (expectedHash.Equals(hash.Replace("-", ""), StringComparison.OrdinalIgnoreCase))
            {
                responseDictionary = CoreHelpers.ParseKeyValueList(decryptedResponse, '&', false, null);
                brokerTokenResponse = MsalTokenResponse.CreateFromBrokerResponse(responseDictionary);
            }
            else
            {
                brokerTokenResponse = new MsalTokenResponse
                {
                    Error = MsalError.BrokerResponseHashMismatch,
                    ErrorDescription = MsalErrorMessage.BrokerResponseHashMismatch
                };
            }

            return brokerTokenResponse;
        }

        public static void SetBrokerResponse(NSUrl responseUrl)
        {
            s_brokerResponse = responseUrl;
            s_brokerResponseReady.Release();
        }
    }
}

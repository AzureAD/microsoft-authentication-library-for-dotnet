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
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using System.Globalization;
using Security;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    /// <summary>
    /// Handles requests which invoke the broker. This is only for mobile (iOS and Android) scenarios.
    /// </summary>
    internal class iOSBroker : NSObject, IBroker
    {
        private static SemaphoreSlim s_brokerResponseReady = null;
        private static NSUrl s_brokerResponse = null;

        private readonly ICoreLogger _logger;
        private readonly ICryptographyManager _cryptoManager;
        private string _brokerRequestNonce;
        private bool _brokerV3Installed = false;

        public iOSBroker(ICoreLogger logger, ICryptographyManager cryptoManager)
        {
            _logger = logger;
            _cryptoManager = cryptoManager;
        }

        public bool CanInvokeBroker(CoreUIParent uiParent)
        {
            if (uiParent?.CallerViewController == null)
            {
                _logger.Error(iOSBrokerConstants.CallerViewControllerIsNullCannotInvokeBroker);
                throw new MsalClientException(MsalError.UIViewControllerRequiredForiOSBroker, MsalErrorMessage.UIViewControllerIsRequiredToInvokeiOSBroker);
            }

            bool canStartBroker = false;

            uiParent.CallerViewController.InvokeOnMainThread(() =>
            {
                if (IsBrokerInstalled(BrokerParameter.UriSchemeBrokerV3))
                {
                    _logger.Info(iOSBrokerConstants.iOSBrokerv3Installed);
                    _brokerV3Installed = true;
                    canStartBroker = true;
                }
            });

            if (!canStartBroker)
            {
                uiParent.CallerViewController.InvokeOnMainThread(() =>
                {
                    if (IsBrokerInstalled(BrokerParameter.UriSchemeBrokerV2))
                    {
                        _logger.Info(iOSBrokerConstants.iOSBrokerv2Installed);
                        canStartBroker = true;
                    }
                });
            }

            if (!canStartBroker)
            {
                _logger.Info(iOSBrokerConstants.CanInvokeBrokerReturnsFalseMessage);
            }

            return canStartBroker;
        }

        public async Task<MsalTokenResponse> AcquireTokenUsingBrokerAsync(Dictionary<string, string> brokerPayload)
        {
            AddIosSpecificParametersToPayload(brokerPayload);

            await InvokeIosBrokerAsync(brokerPayload).ConfigureAwait(false);

            return ProcessBrokerResponse();
        }

        private void AddIosSpecificParametersToPayload(Dictionary<string, string> brokerPayload)
        {
            string encodedBrokerKey = Base64UrlHelpers.Encode(BrokerKeyHelper.GetRawBrokerKey(_logger));
            brokerPayload[iOSBrokerConstants.BrokerKey] = encodedBrokerKey;
            brokerPayload[iOSBrokerConstants.MsgProtocolVer] = BrokerParameter.MsgProtocolVersion3;

            if (_brokerV3Installed)
            {
                _brokerRequestNonce = Guid.NewGuid().ToString();
                brokerPayload[iOSBrokerConstants.BrokerNonce] = _brokerRequestNonce;

                string applicationToken = TryReadBrokerApplicationTokenFromKeychain(brokerPayload);

                if (!string.IsNullOrEmpty(applicationToken))
                {
                    brokerPayload[iOSBrokerConstants.ApplicationToken] = applicationToken;
                }
            }

            if (brokerPayload.ContainsKey(iOSBrokerConstants.Claims))
            {
                brokerPayload[iOSBrokerConstants.SkipCache] = BrokerParameter.SkipCache;
                string claims = Base64UrlHelpers.Encode(brokerPayload[BrokerParameter.Claims]);
                brokerPayload[BrokerParameter.Claims] = claims;
            }
        }

        private async Task InvokeIosBrokerAsync(Dictionary<string, string> brokerPayload)
        {
            s_brokerResponseReady = new SemaphoreSlim(0);

            if (brokerPayload.ContainsKey(BrokerParameter.BrokerInstallUrl))
            {
                _logger.Info(iOSBrokerConstants.BrokerPayloadContainsInstallUrl);

                string url = brokerPayload[BrokerParameter.BrokerInstallUrl];
                Uri uri = new Uri(url);
                string query = uri.Query;

                if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Substring(1);
                }

                _logger.Info(iOSBrokerConstants.InvokeIosBrokerAppLink);

                Dictionary<string, string> keyPair = CoreHelpers.ParseKeyValueList(query, '&', true, false, null);

                _logger.Info(iOSBrokerConstants.StartingActionViewActivity + iOSBrokerConstants.AppLink);

                DispatchQueue.MainQueue.DispatchAsync(() => UIApplication.SharedApplication.OpenUrl(new NSUrl(keyPair[iOSBrokerConstants.AppLink])));

                throw new MsalClientException(MsalErrorIOSEx.BrokerApplicationRequired, MsalErrorMessageIOSEx.BrokerApplicationRequired);
            }

            else
            {
                _logger.Info(iOSBrokerConstants.InvokeTheIosBroker);

                NSUrl url = new NSUrl(iOSBrokerConstants.InvokeBroker + brokerPayload.ToQueryParameter());

                _logger.VerbosePii(iOSBrokerConstants.BrokerPayloadPii + brokerPayload.ToQueryParameter(),

                iOSBrokerConstants.BrokerPayloadNoPii + brokerPayload.Count);

                DispatchQueue.MainQueue.DispatchAsync(() => UIApplication.SharedApplication.OpenUrl(url));
            }

            await s_brokerResponseReady.WaitAsync().ConfigureAwait(false);
        }

        private MsalTokenResponse ProcessBrokerResponse()
        {
            string[] keyValuePairs = s_brokerResponse.Query.Split('&');
            Dictionary<string, string> responseDictionary = new Dictionary<string, string>(StringComparer.InvariantCulture);

            foreach (string pair in keyValuePairs)
            {
                string[] keyValue = pair.Split('=');
                responseDictionary[keyValue[0]] = CoreHelpers.UrlDecode(keyValue[1]);

                if (responseDictionary[keyValue[0]].Equals("(null)", StringComparison.OrdinalIgnoreCase)
                    && keyValue[0].Equals(iOSBrokerConstants.Code, StringComparison.OrdinalIgnoreCase))
                {
                    responseDictionary[iOSBrokerConstants.Error] = iOSBrokerConstants.BrokerError;

                    _logger.VerbosePii(iOSBrokerConstants.BrokerResponseValuesPii + keyValue.ToString(),
                    iOSBrokerConstants.BrokerResponseContainsError);
                }
            }

            _logger.Verbose(iOSBrokerConstants.ProcessBrokerResponse + responseDictionary.Count);

            return ResultFromBrokerResponse(responseDictionary);
        }

        private MsalTokenResponse ResultFromBrokerResponse(Dictionary<string, string> responseDictionary)
        {
            MsalTokenResponse brokerTokenResponse;

            string expectedHash = responseDictionary[iOSBrokerConstants.ExpectedHash];
            string encryptedResponse = responseDictionary[iOSBrokerConstants.EncryptedResponsed];
            string decryptedResponse = BrokerKeyHelper.DecryptBrokerResponse(encryptedResponse, _logger);
            string responseActualHash = _cryptoManager.CreateSha256Hash(decryptedResponse);
            byte[] rawHash = Convert.FromBase64String(responseActualHash);
            string hash = BitConverter.ToString(rawHash);

            if (expectedHash.Equals(hash.Replace("-", ""), StringComparison.OrdinalIgnoreCase))
            {
                responseDictionary = CoreHelpers.ParseKeyValueList(decryptedResponse, '&', false, null);

                if (!ValidateBrokerResponseNonceWithRequestNonce(responseDictionary))
                {
                    return new MsalTokenResponse
                    {
                        Error = MsalError.BrokerNonceMismatch,
                        ErrorDescription = MsalErrorMessage.BrokerNonceMismatch
                    };
                }

                if (responseDictionary.ContainsKey(iOSBrokerConstants.ApplicationToken))
                {
                    TryWriteBrokerApplicationTokenToKeychain(
                        responseDictionary[BrokerResponseConst.ClientId], 
                        responseDictionary[iOSBrokerConstants.ApplicationToken]);
                }

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

        private bool IsBrokerInstalled(string brokerUriScheme)
        {
            return UIApplication.SharedApplication.CanOpenUrl(new NSUrl(brokerUriScheme));
        }

        private bool ValidateBrokerResponseNonceWithRequestNonce(Dictionary<string, string> brokerResponseDictionary)
        {
            if (_brokerV3Installed)
            {
                string brokerResponseNonce = brokerResponseDictionary[BrokerResponseConst.iOSBrokerNonce];

                bool ok = string.Equals(
                    brokerResponseNonce,
                    _brokerRequestNonce,
                    StringComparison.InvariantCultureIgnoreCase);

                if (!ok)
                {
                    _logger.Error(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "Nonce check failed! Broker response nonce is:  {0}, \nBroker request nonce is: {1}",
                            brokerResponseNonce,
                            _brokerRequestNonce));
                }
                return ok;
            }
            return true;
        }

        private void TryWriteBrokerApplicationTokenToKeychain(string clientId, string applicationToken)
        {
            iOSTokenCacheAccessor iOSTokenCacheAccessor = new iOSTokenCacheAccessor();

            try
            {
                SecStatusCode secStatusCode = iOSTokenCacheAccessor.SaveBrokerApplicationToken(clientId, applicationToken);

                _logger.Info(string.Format(
                    CultureInfo.CurrentCulture,
                    iOSBrokerConstants.AttemptToSaveBrokerApplicationToken + "SecStatusCode: {0}",
                    secStatusCode));
            }
            catch(Exception ex)
            {
                throw new MsalClientException(
                    MsalError.WritingApplicationTokenToKeychainFailed, 
                    MsalErrorMessage.WritingApplicationTokenToKeychainFailed + ex.Message);
            }
        }

        private string TryReadBrokerApplicationTokenFromKeychain(Dictionary<string, string> brokerPayload)
        {
            iOSTokenCacheAccessor iOSTokenCacheAccessor = new iOSTokenCacheAccessor();

            try
            {
                SecStatusCode secStatusCode = iOSTokenCacheAccessor.TryGetBrokerApplicationToken(brokerPayload[BrokerParameter.ClientId], out string appToken);

                _logger.Info(string.Format(
                    CultureInfo.CurrentCulture,
                    iOSBrokerConstants.SecStatusCodeFromTryGetBrokerApplicationToken + "SecStatusCode: {0}",
                    secStatusCode));

                return appToken;
            }
            catch(Exception ex)
            {
                throw new MsalClientException(
                    MsalError.ReadingApplicationTokenFromKeychainFailed, 
                    MsalErrorMessage.ReadingApplicationTokenFromKeychainFailed + ex.Message);
            }
        }

        public static void SetBrokerResponse(NSUrl responseUrl)
        {
            s_brokerResponse = responseUrl;
            s_brokerResponseReady.Release();
        }
    }
}

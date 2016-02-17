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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using Foundation;
using Microsoft.Identity.Client.Interfaces;
using Microsoft.Identity.Client.Internal;
using UIKit;

namespace Microsoft.Identity.Client
{
    internal class BrokerHelper : IBrokerHelper
    {
        private static SemaphoreSlim brokerResponseReady = null;
        private static NSUrl brokerResponse = null;
        
        public IPlatformParameters PlatformParameters { get; set; }

        //TODO - enable broker flows when authenticator apps support it
        public bool CanInvokeBroker { get
        {
            PlatformParameters pp = PlatformParameters as PlatformParameters;
            return false && !pp.SkipBroker && UIApplication.SharedApplication.CanOpenUrl(new NSUrl("msauth://"));
        } }

        public async Task<AuthenticationResultEx> AcquireTokenUsingBroker(IDictionary<string, string> brokerPayload)
        {
            if (brokerPayload.ContainsKey("silent_broker_flow"))
            {
                throw new MsalSilentTokenAcquisitionException();
            }

            brokerResponse = null;
            brokerResponseReady = new SemaphoreSlim(0);
            
            //call broker
            string base64EncodedString = Base64UrlEncoder.Encode(BrokerKeyHelper.GetRawBrokerKey());
            brokerPayload["broker_key"] = base64EncodedString;
            brokerPayload["max_protocol_ver"] = "2";

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

                DispatchQueue.MainQueue.DispatchAsync(() => UIApplication.SharedApplication.OpenUrl(new NSUrl(keyPair["app_link"])));

                    throw new MsalException(AdalErrorIOSEx.BrokerApplicationRequired, AdalErrorMessageIOSEx.BrokerApplicationRequired);
            }
            else
            {
                NSUrl url = new NSUrl("msauth://broker?" + brokerPayload.ToQueryParameter());
                DispatchQueue.MainQueue.DispatchAsync(() => UIApplication.SharedApplication.OpenUrl(url));
            }

            await brokerResponseReady.WaitAsync().ConfigureAwait(false);
            return ProcessBrokerResponse();
        }

        private AuthenticationResultEx ProcessBrokerResponse()
        {
            string[] keyValuePairs = brokerResponse.Query.Split('&');

            IDictionary<string, string> responseDictionary = new Dictionary<string, string>();
            foreach (string pair in keyValuePairs)
            {
                string[] keyValue = pair.Split('=');
                responseDictionary[keyValue[0]] = EncodingHelper.UrlDecode(keyValue[1]);
                if (responseDictionary[keyValue[0]].Equals("(null)") && keyValue[0].Equals("code"))
                {
                    responseDictionary["error"] = "broker_error";
                }
            }

            return ResultFromBrokerResponse(responseDictionary);
        }

        private AuthenticationResultEx ResultFromBrokerResponse(IDictionary<string, string> responseDictionary)
        {
            TokenResponse response = new TokenResponse();

            if (responseDictionary.ContainsKey("error") || responseDictionary.ContainsKey("error_description"))
            {
                response = TokenResponse.CreateFromBrokerResponse(responseDictionary);
            }
            else
            {
                string expectedHash = responseDictionary["hash"];
                string encryptedResponse = responseDictionary["response"];
                string decryptedResponse = BrokerKeyHelper.DecryptBrokerResponse(encryptedResponse, responseDictionary.ContainsKey("msg_protocol_ver"));
                string responseActualHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(decryptedResponse);
                byte[] rawHash = Convert.FromBase64String(responseActualHash);
                string hash  = BitConverter.ToString(rawHash);
                if (expectedHash.Equals(hash.Replace("-","")))
                {
                    responseDictionary = EncodingHelper.ParseKeyValueList(decryptedResponse, '&', false, null);
                    response = TokenResponse.CreateFromBrokerResponse(responseDictionary);
                }
                else
                {
                    response = new TokenResponse
                    {
                        Error = MsalError.BrokerReponseHashMismatch,
                        ErrorDescription = MsalErrorMessage.BrokerReponseHashMismatch
                    };
                }
            }

            return response.GetResult();
        }
        
        public static void SetBrokerResponse(NSUrl brokerResponse)
        {
            BrokerHelper.brokerResponse = brokerResponse;
            brokerResponseReady.Release();
        }
    }
}
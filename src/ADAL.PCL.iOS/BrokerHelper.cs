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
using Foundation;
using UIKit;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class BrokerHelper : IBrokerHelper
    {
        private static SemaphoreSlim brokerResponseReady = null;
        private static NSUrl brokerResponse = null;
        
        public bool CanUseBroker { get { return UIApplication.SharedApplication.CanOpenUrl(new NSUrl("msauth://")); } }

        public async Task<AuthenticationResultEx> AcquireTokenUsingBroker(IDictionary<string, string> brokerPayload)
        {
            brokerResponse = null;

            /*- (void)callBrokerForAuthority:(NSString*)authority
                      resource:(NSString*)resource
                      clientId:(NSString*)clientId
                   redirectUri:(NSURL*)redirectUri
                promptBehavior:(ADPromptBehavior)promptBehavior
                        userId:(ADUserIdentifier*)userId
          extraQueryParameters:(NSString*)queryParams
                 correlationId:(NSString*)correlationId
               completionBlock:(ADAuthenticationCallback)completionBlock*/

            brokerResponseReady = new SemaphoreSlim(0);
            
            //call broker
            brokerPayload["broker_key"] = EncodingHelper.UrlEncode(BrokerKeyHelper.GetBrokerKey());
            await brokerResponseReady.WaitAsync();
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
            }

            return ResultFromBrokerResponse(responseDictionary);
        }

        private AuthenticationResultEx ResultFromBrokerResponse(IDictionary<string, string> responseDictionary)
        {
            TokenResponse response = new TokenResponse();

            if (responseDictionary.ContainsKey("error"))
            {
                response = TokenResponse.CreateFromBrokerResponse(responseDictionary);
            }
            else
            {
                string expectedHash = responseDictionary["hash"];
                string encryptedResponse = responseDictionary["response"];
                string decryptedResponse = BrokerKeyHelper.DecryptBrokerResponse(encryptedResponse);
                string responseActualHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(decryptedResponse);

                if (expectedHash.Equals(responseActualHash))
                {
                    response = TokenResponse.CreateFromBrokerResponse(responseDictionary);
                }
                else
                {
                    response = new TokenResponse
                    {
                        Error = AdalError.BrokerReponseHashMismatch,
                        ErrorDescription = AdalErrorMessage.BrokerReponseHashMismatch
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
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using Foundation;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Flows;
using UIKit;
using Microsoft.Identity.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform
{
    internal class BrokerHelper
    {
        private static SemaphoreSlim brokerResponseReady = null;
        private static NSUrl brokerResponse = null;

        public RequestContext RequestContext { get; set; }
        public IPlatformParameters PlatformParameters { get; set; }

        public bool CanInvokeBroker
        {
            get
            {
                PlatformParameters pp = PlatformParameters as PlatformParameters;
				if (pp == null)
				{
					return false;
				}

                var res = false;

                if (pp.UseBroker)
                {
                    pp.CallerViewController.InvokeOnMainThread(() =>
                    {
                        res = UIApplication.SharedApplication.CanOpenUrl(new NSUrl("msauth://"));
                    });
                }

                return res;
            }
        }

        public async Task<AuthenticationResultEx> AcquireTokenUsingBroker(IDictionary<string, string> brokerPayload)
        {
            if (brokerPayload.ContainsKey(BrokerParameter.SilentBrokerFlow))
            {
                throw new AdalSilentTokenAcquisitionException();
            }

            brokerResponse = null;
            brokerResponseReady = new SemaphoreSlim(0);
            
            //call broker
            string base64EncodedString = Base64UrlEncoder.Encode(BrokerKeyHelper.GetRawBrokerKey());
            brokerPayload["broker_key"] = base64EncodedString;
            brokerPayload["max_protocol_ver"] = "2";

            if (brokerPayload.ContainsKey("claims"))
            {
                brokerPayload.Add("skip_cache", "YES");
                string claims = EncodingHelper.UrlEncode(brokerPayload[BrokerParameter.Claims]);
                brokerPayload[BrokerParameter.Claims] = claims;
            }

            if (brokerPayload.ContainsKey(BrokerParameter.BrokerInstallUrl))
            {
                    string url = brokerPayload[BrokerParameter.BrokerInstallUrl];
                    Uri uri = new Uri(url);
                    string query = uri.Query;
                    if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Substring(1);
                    }

                    Dictionary<string, string> keyPair = EncodingHelper.ParseKeyValueList(query, '&', true, false, null);

                DispatchQueue.MainQueue.DispatchAsync(() => UIApplication.SharedApplication.OpenUrl(new NSUrl(keyPair["app_link"])));

                    throw new AdalException(AdalErrorIOSEx.BrokerApplicationRequired, AdalErrorMessageIOSEx.BrokerApplicationRequired);
            }

            else
            {
                NSUrl url = new NSUrl("msauth://broker?" + brokerPayload.ToQueryParameter());
                DispatchQueue.MainQueue.DispatchAsync(() => UIApplication.SharedApplication.OpenUrl(url));
            }

            await brokerResponseReady.WaitAsync().ConfigureAwait(false);
            PlatformParameters = null;
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
            TokenResponse response;

            if (responseDictionary.ContainsKey("error") || responseDictionary.ContainsKey("error_description"))
            {
                response = TokenResponse.CreateFromBrokerResponse(responseDictionary);
            }
            else
            {
                string expectedHash = responseDictionary["hash"];
                string encryptedResponse = responseDictionary["response"];
                string decryptedResponse = BrokerKeyHelper.DecryptBrokerResponse(encryptedResponse);
                string responseActualHash = CryptographyHelper.CreateSha256Hash(decryptedResponse);
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
                        Error = AdalError.BrokerReponseHashMismatch,
                        ErrorDescription = AdalErrorMessage.BrokerReponseHashMismatch
                    };
                }
            }

            var dateTimeOffset = new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            dateTimeOffset = dateTimeOffset.AddSeconds(response.ExpiresOn);
            return response.GetResult(dateTimeOffset, dateTimeOffset);
        }
        
        public static void SetBrokerResponse(NSUrl brokerResponse)
        {
            BrokerHelper.brokerResponse = brokerResponse;
            brokerResponseReady.Release();
        }
    }
}

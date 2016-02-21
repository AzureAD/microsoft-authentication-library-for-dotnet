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
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Handlers
{
    class AcquireTokenByDeviceCodeHandler : AcquireTokenHandlerBase
    {
        private DeviceCodeResult deviceCodeResult = null;

        public AcquireTokenByDeviceCodeHandler(Authenticator authenticator, TokenCache tokenCache, DeviceCodeResult deviceCodeResult)
            : base(authenticator, tokenCache, deviceCodeResult.Scope, new ClientKey(deviceCodeResult.ClientId), null)
        {
            if (deviceCodeResult == null)
            {
                throw new ArgumentNullException("deviceCodeResult");
            }
            
            this.LoadFromCache = false; //no cache lookup for token
            this.StoreToCache = (tokenCache != null);
            this.SupportADFS = false;
            this.deviceCodeResult = deviceCodeResult;
        }

        protected override async Task<AuthenticationResultEx> SendTokenRequestAsync()
        {
            TimeSpan timeRemaining = deviceCodeResult.ExpiresOn - DateTimeOffset.UtcNow;
            AuthenticationResultEx resultEx = null;
            while (timeRemaining.TotalSeconds > 0)
            {
                try
                {
                    resultEx = await base.SendTokenRequestAsync().ConfigureAwait(false);
                    break;
                }
                catch (MsalServiceException exc)
                {
                    if (!exc.ErrorCode.Equals(MsalErrorEx.DeviceCodeAuthorizationPendingError))
                    {
                        throw;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(deviceCodeResult.Interval)).ConfigureAwait(false);
                timeRemaining = deviceCodeResult.ExpiresOn - DateTimeOffset.UtcNow;
            }

            return resultEx;
        }

        protected override void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters)
        {
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.DeviceCode;
            requestParameters[OAuthParameter.Code] = this.deviceCodeResult.DeviceCode;
        }
    }
}

//----------------------------------------------------------------------
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
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AcquireTokenByDeviceCodeHandler : AcquireTokenHandlerBase
    {
        private DeviceCodeResult deviceCodeResult = null;

        public AcquireTokenByDeviceCodeHandler(RequestData requestData, DeviceCodeResult deviceCodeResult)
            : base(requestData)
        {
            this.LoadFromCache = false; //no cache lookup for token
            this.StoreToCache = (requestData.TokenCache != null);
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
                catch (AdalServiceException exc)
                {
                    if (!exc.ErrorCode.Equals(AdalErrorEx.DeviceCodeAuthorizationPendingError))
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

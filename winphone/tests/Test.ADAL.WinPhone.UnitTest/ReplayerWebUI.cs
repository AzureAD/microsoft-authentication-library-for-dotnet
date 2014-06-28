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
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Test.ADAL.WinPhone.UnitTest
{
    class ReplayerWebUI : ReplayerBase, IWebUI
    {
        private const string Delimiter = ":::";

        public object OwnerWindow { get; set; }

        public ReplayerWebUI(bool useCorporateNetwork)
        {
        }

        public void Authenticate(Uri requestUri, Uri callbackUri, CallState callState, IDictionary<string, object> headersMap)
        {
            string key = requestUri.AbsoluteUri + callbackUri.AbsoluteUri;

            if (IOMap.ContainsKey(key))
            {
                string value = IOMap[key];
                if (value[0] == 'P')
                {
                    //return OAuth2Response.ParseAuthorizeResponse(value.Substring(1), callState);
                }
                
                if (value[0] == 'A')
                {
                    string []segments = value.Substring(1).Split(new [] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
                    //return new AuthorizationResult(error: segments[0], errorDescription: segments[1]);
                }
            }
        }

    }
}

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
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.WinRT.Unit
{
    class ReplayerWebUI : ReplayerBase, IWebUI
    {
        private const string Delimiter = ":::";

        public ReplayerWebUI(IPlatformParameters parameters)
        {
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            string key = authorizationUri.AbsoluteUri + redirectUri.OriginalString;

            if (IOMap.ContainsKey(key))
            {
                string value = IOMap[key];
                if (value[0] == 'P')
                {
                    value = value.Substring(1);
                    string[] valueSegments = value.Split(new string[] { "::" }, StringSplitOptions.None);
                    return new AuthorizationResult((AuthorizationStatus)Enum.Parse(typeof(AuthorizationStatus), valueSegments[0]), valueSegments[1]);
                }
                
                if (value[0] == 'A')
                {
                    string []segments = value.Substring(1).Split(new [] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
                    return new AuthorizationResult(AuthorizationStatus.Success, string.Format("https://dummy?error={0}&error_description={1}", segments[0], segments[1]));
                }
            }

            return null;
        }
    }
}

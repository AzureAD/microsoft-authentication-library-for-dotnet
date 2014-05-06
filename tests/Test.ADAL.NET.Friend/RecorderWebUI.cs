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
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Test.ADAL.NET.Friend
{
    public class RecorderWebUI : RecorderBase, IWebUI
    {
        private const string Delimiter = ":::";
        private readonly IWebUI internalWebUI;

        static RecorderWebUI()
        {
            Initialize();
        }

        public RecorderWebUI(PromptBehavior promptBehavior, object ownerWindow)
        {
            this.internalWebUI = (new WebUIFactory()).Create(promptBehavior, ownerWindow);
        }

        public object OwnerWindow { get; set; }

        public string Authenticate(Uri requestUri, Uri callbackUri)
        {
            string key = requestUri.AbsoluteUri + callbackUri.AbsoluteUri;
            string value = null;

            if (IOMap.ContainsKey(key))
            {
                value = IOMap[key];
                if (value[0] == 'P')
                {
                    return value.Substring(1);
                }
                
                if (value[0] == 'A')
                {
                    string []segments = value.Substring(1).Split(new [] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
                    throw new AdalServiceException(errorCode: segments[0], message: segments[1])
                          {
                              StatusCode = int.Parse(segments[2])
                          };
                }
            }

            try
            {
                string result = this.internalWebUI.Authenticate(requestUri, callbackUri);
                value = 'P' + result;
                return result;
            }
            catch (AdalException ex)
            {
                AdalServiceException serviceException = ex as AdalServiceException;
                if (serviceException != null && serviceException.StatusCode == 503)
                {
                    value = null;
                }
                else
                {
                    value = 'A' + string.Format("{0}{1}{2}{3}{4}", ex.ErrorCode, Delimiter, ex.Message, Delimiter, 
                        (serviceException != null) ? serviceException.StatusCode : 0);
                }
                
                throw;
            }
            finally
            {
                if (value != null)
                {
                    IOMap.Add(key, value);
                }
            }
        }
    }
}

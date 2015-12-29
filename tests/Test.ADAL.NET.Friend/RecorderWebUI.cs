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

namespace Test.ADAL.NET.Friend
{
    internal class RecorderWebUI : RecorderBase, IWebUI
    {
        private const string Delimiter = ":::";
        private readonly IWebUI internalWebUI;

        static RecorderWebUI()
        {
            Initialize();
        }

        public RecorderWebUI(IPlatformParameters parameters)
        {
            this.internalWebUI = (new WebUIFactory()).CreateAuthenticationDialog(parameters);
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri requestUri, Uri callbackUri, CallState callState)
        {
            string key = requestUri.AbsoluteUri + callbackUri.OriginalString;
            string value = null;

            if (IOMap.ContainsKey(key))
            {
                value = IOMap[key];
                if (value[0] == 'P')
                {
                    value = value.Substring(1);
                    string[] valueSegments = value.Split(new [] {"::"}, StringSplitOptions.None);
                    return new AuthorizationResult((AuthorizationStatus)Enum.Parse(typeof(AuthorizationStatus), valueSegments[0]), valueSegments[1]);
                }               
                
                if (value[0] == 'A')
                {
                    string []segments = value.Substring(1).Split(new [] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
                    throw new MsalServiceException(errorCode: segments[0], message: segments[1])
                          {
                              StatusCode = int.Parse(segments[2])
                          };
                }
            }

            try
            {
                AuthorizationResult result = await this.internalWebUI.AcquireAuthorizationAsync(requestUri, callbackUri, callState);
                const string DummyUri = "https://temp_uri";
                switch (result.Status)
                {
                    case AuthorizationStatus.Success: value = string.Format("{0}?code={1}", DummyUri, result.Code); break;
                    case AuthorizationStatus.UserCancel: value = string.Empty; break;
                    case AuthorizationStatus.ProtocolError: value = string.Format("{0}?error={1}&error_description={2}", DummyUri, result.Error, result.ErrorDescription); break;
                    default: value = string.Empty; break;
                }

                value = "P" + result.Status + "::" + value;

                return result;
            }
            catch (MsalException ex)
            {
                MsalServiceException serviceException = ex as MsalServiceException;
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

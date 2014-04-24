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
using System.IO;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Test.ADAL.NET.Friend
{
    public class RecorderWebUI : IWebUI
    {
        private const string Delimiter = ":::";
        private const string DictionaryFilename = @"recorded_webui.dat";
        private readonly string dictionaryFilePath;
        private readonly IWebUI internalWebUI;

        private readonly Dictionary<string, string> iOMap;
        public RecorderWebUI(PromptBehavior promptBehavior)
        {
            this.dictionaryFilePath = RecorderSettings.Path + DictionaryFilename;
            this.internalWebUI = WebAuthenticationDialogFactory.Create(promptBehavior);
            this.iOMap = (RecorderSettings.Mode == RecorderMode.Replay && File.Exists(this.dictionaryFilePath)) ? 
                SerializationHelper.DeserializeDictionary(this.dictionaryFilePath) : new Dictionary<string, string>();
        }

        public object OwnerWindow { get; set; }

        public void WriteToFile()
        {
            SerializationHelper.SerializeDictionary(this.iOMap, this.dictionaryFilePath);            
        }

        public string Authenticate(Uri requestUri, Uri callbackUri)
        {
            string key = requestUri.AbsoluteUri + callbackUri.AbsoluteUri;
            string value = null;

            if (this.iOMap.ContainsKey(key))
            {
                value = this.iOMap[key];
                if (value[0] == 'P')
                {
                    return value.Substring(1);
                }
                
                if (value[0] == 'A')
                {
                    string []segments = value.Substring(1).Split(new [] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
                    throw new ActiveDirectoryAuthenticationException(errorCode: segments[0], message: segments[1])
                          {
                              InnerStatusCode = int.Parse(segments[2])
                          };
                }
            }

            try
            {
                string result = this.internalWebUI.Authenticate(requestUri, callbackUri);
                value = 'P' + result;
                return result;
            }
            catch (ActiveDirectoryAuthenticationException ex)
            {
                if (ex.InnerStatusCode == 503)
                {
                    value = null;
                }
                else
                { 
                    value = 'A' + string.Format("{0}{1}{2}{3}{4}", ex.ErrorCode, Delimiter, ex.Message, Delimiter, ex.InnerStatusCode);
                }
                
                throw;
            }
            finally
            {
                if (value != null)
                {
                    this.iOMap.Add(key, value);
                }
            }
        }
    }
}

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

using System.ComponentModel;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    public class WebBrowserNavigateErrorEventArgs : CancelEventArgs
    {
        // Fields
        private readonly string targetFrameName;
        private readonly string url;
        private readonly int statusCode;
        private readonly object webBrowserActiveXInstance;

        // Methods
        public WebBrowserNavigateErrorEventArgs(string url, string targetFrameName, int statusCode, object webBrowserActiveXInstance)
        {
            this.url = url;
            this.targetFrameName = targetFrameName;
            this.statusCode = statusCode;
            this.webBrowserActiveXInstance = webBrowserActiveXInstance;
        }

        // Properties
        public string TargetFrameName
        {
            get
            {
                return this.targetFrameName;
            }
        }

        // url as a string, as in case of error it could be invalid url
        public string Url
        {
            get
            {
                return this.url;
            }
        }

        // ADAL.Native has code for interpretation of this code to string
        // we don't do it here, as we need to come consideration should we do it or not.
        public int StatusCode
        {
            get
            {
                return this.statusCode;
            }
        }

        public object WebBrowserActiveXInstance
        {
            get
            {
                return this.webBrowserActiveXInstance;
            }
        }
    }
}
